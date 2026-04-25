using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 工作台合成面板 UI：显示背包类配方列表，选中后展示材料消耗和制作按钮。
// 挂载位置：Canvas 下的 WorkbenchUIPanel 对象。
// _panelVisible 作为开关标志，不依赖 activeSelf，防止父级始终激活时误判为打开。
public class WorkbenchUI : MonoBehaviour
{
    public static WorkbenchUI Instance;

    [Header("面板根节点")]
    [SerializeField] private GameObject workbenchPanel;

    [Header("配方槽位（需同时挂 BuildingUISlot + Button 组件）")]
    [SerializeField] private BuildingUISlot[] recipeSlots;

    [Header("详情文字")]
    [SerializeField] private TMP_Text recipeNameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text workbenchTipText;
    [SerializeField] private TMP_Text warningText;

    [Header("按钮")]
    [SerializeField] private Button   craftButton;
    [SerializeField] private TMP_Text craftButtonLabel;
    [SerializeField] private Button   closeButton;

    [Header("可合成配方（仅拖入 isInventoryRecipe = true 的配方 SO）")]
    [SerializeField] private BuildingRecipeSO[] craftableRecipes;

    private int             _selectedIndex   = -1;
    private PlayerInventory _playerInventory;
    private bool            _isAtWorkbench;
    private bool            _panelVisible;

    public bool IsOpen => _panelVisible;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // 首帧 Update 之前关闭面板，防止误判导致 ESC 路由异常
        _panelVisible = false;
        if (workbenchPanel != null) workbenchPanel.SetActive(false);
    }

    private void Start()
    {
        _playerInventory = FindObjectOfType<PlayerInventory>();

        if (closeButton  != null) closeButton.onClick.AddListener(ClosePanel);
        if (craftButton  != null) craftButton.onClick.AddListener(TryCraftSelected);

        // 为每个配方槽位注册点击事件，必须捕获 i 否则所有 lambda 共享同一个值
        if (recipeSlots != null)
        {
            for (int i = 0; i < recipeSlots.Length; i++)
            {
                int capturedIdx = i;
                Button btn = recipeSlots[i] != null ? recipeSlots[i].GetComponent<Button>() : null;
                if (btn != null) btn.onClick.AddListener(() => SelectRecipe(capturedIdx));
            }
        }

        ClosePanel();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            ClosePanel();
    }

    // 打开合成面板：isAtWorkbench 决定是否解锁需要工作台的高级配方
    public void OpenPanel(bool isAtWorkbench = true)
    {
        _isAtWorkbench = isAtWorkbench;
        _selectedIndex = -1;
        if (workbenchPanel != null) workbenchPanel.SetActive(true);
        _panelVisible = true;
        RefreshRecipeSlots();
        ClearDetails();
    }

    // 关闭合成面板并清空选中状态
    public void ClosePanel()
    {
        _selectedIndex = -1;
        if (workbenchPanel != null) workbenchPanel.SetActive(false);
        _panelVisible = false;
        HideWarning();
    }

    // 刷新左侧配方槽位：根据工作台状态决定是否显示锁定样式
    private void RefreshRecipeSlots()
    {
        if (recipeSlots == null) return;
        foreach (var slot in recipeSlots)
        {
            if (slot != null) slot.SetData(null, false, false);
        }

        if (craftableRecipes == null) return;
        int maxCount = Mathf.Min(craftableRecipes.Length, recipeSlots.Length);
        for (int i = 0; i < maxCount; i++)
        {
            BuildingRecipeSO recipe = craftableRecipes[i];
            if (recipe == null) continue;
            bool unlocked = !recipe.requiresWorkbench || _isAtWorkbench;
            bool selected = (i == _selectedIndex);
            recipeSlots[i].SetData(recipe.icon, selected, visible: true, unlocked: unlocked);
        }
    }

    // 点击配方槽位时记录选中下标并刷新右侧详情
    private void SelectRecipe(int slotIndex)
    {
        if (craftableRecipes == null || slotIndex < 0 || slotIndex >= craftableRecipes.Length) return;
        if (craftableRecipes[slotIndex] == null) return;
        _selectedIndex = slotIndex;
        RefreshRecipeSlots();
        ShowRecipeDetails(craftableRecipes[_selectedIndex]);
    }

    // 显示选中配方的名称、材料消耗（不足时红色）和制作按钮状态
    private void ShowRecipeDetails(BuildingRecipeSO recipe)
    {
        if (recipe == null) return;

        if (recipeNameText != null) recipeNameText.text = recipe.buildingName;

        if (costText != null)
        {
            if (recipe.costs == null || recipe.costs.Length == 0)
            {
                costText.text = "无需材料";
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                foreach (BuildingCostData cost in recipe.costs)
                {
                    if (cost == null) continue;
                    int owned  = _playerInventory != null ? _playerInventory.GetItemCount(cost.itemType) : 0;
                    string name = GetItemDisplayName(cost.itemType);
                    bool enough = owned >= cost.amount;
                    if (enough)
                        sb.AppendLine($"{name}  {owned}/{cost.amount}");
                    else
                        sb.AppendLine($"<color=#FF4444>{name}  {owned}/{cost.amount}</color>");
                }
                costText.text = sb.ToString().TrimEnd();
            }
        }

        // 配方需要工作台但当前未在工作台旁时显示提示
        if (workbenchTipText != null)
        {
            bool showTip = recipe.requiresWorkbench && !_isAtWorkbench;
            workbenchTipText.gameObject.SetActive(showTip);
        }

        bool unlocked = !recipe.requiresWorkbench || _isAtWorkbench;
        bool canCraft  = unlocked && CanCraft(recipe);

        if (craftButton      != null) craftButton.interactable = canCraft;
        if (craftButtonLabel != null)
        {
            if (!unlocked)      craftButtonLabel.text = "需要工作台";
            else if (!canCraft) craftButtonLabel.text = "材料不足";
            else                craftButtonLabel.text = "制  作";
        }

        HideWarning();
    }

    // 清空右侧详情，显示初始引导文字
    private void ClearDetails()
    {
        if (recipeNameText   != null) recipeNameText.text  = "选择左侧配方";
        if (costText         != null) costText.text        = "";
        if (workbenchTipText != null) workbenchTipText.gameObject.SetActive(false);
        if (craftButton      != null) craftButton.interactable = false;
        if (craftButtonLabel != null) craftButtonLabel.text = "制  作";
        HideWarning();
    }

    // 制作按钮：校验 → 扣材料 → 背包加物品 → 刷新 UI
    private void TryCraftSelected()
    {
        if (_selectedIndex < 0 || craftableRecipes == null || _selectedIndex >= craftableRecipes.Length)
        {
            ShowWarning("请先选择一个配方");
            return;
        }

        BuildingRecipeSO recipe = craftableRecipes[_selectedIndex];
        if (recipe == null) return;

        if (recipe.requiresWorkbench && !_isAtWorkbench)
        {
            ShowWarning("需要在工作台附近才能制作");
            return;
        }

        if (_playerInventory == null)
        {
            _playerInventory = FindObjectOfType<PlayerInventory>();
            if (_playerInventory == null) { ShowWarning("未找到背包，无法制作"); return; }
        }

        if (!CanCraft(recipe)) { ShowWarning("材料不足"); return; }

        // 扣除消耗材料
        foreach (BuildingCostData cost in recipe.costs)
        {
            if (cost == null) continue;
            _playerInventory.RemoveItem(cost.itemType, cost.amount);
        }

        // 产出物品加入背包
        int outputAmount = recipe.resultAmount > 0 ? recipe.resultAmount : 1;
        _playerInventory.AddItem(recipe.resultItemType, outputAmount);

        // 广播拾取事件，触发 PickupToastUI、QuestManager 等订阅者
        GameEvents.RaiseItemPickedUp(recipe.resultItemType, outputAmount);

        NotificationUI.Instance?.ShowMessage($"制作完成：{recipe.buildingName}");

        // 重新刷新详情，更新材料数量和按钮状态
        ShowRecipeDetails(recipe);
    }

    // 校验玩家是否拥有足够的所有消耗材料
    private bool CanCraft(BuildingRecipeSO recipe)
    {
        if (recipe == null || _playerInventory == null) return false;
        if (recipe.costs == null || recipe.costs.Length == 0) return true;
        foreach (BuildingCostData cost in recipe.costs)
        {
            if (cost == null) continue;
            if (_playerInventory.GetItemCount(cost.itemType) < cost.amount) return false;
        }
        return true;
    }

    private void ShowWarning(string msg)
    {
        if (warningText == null) return;
        warningText.gameObject.SetActive(true);
        warningText.text = msg;
    }

    private void HideWarning()
    {
        if (warningText == null) return;
        warningText.gameObject.SetActive(false);
    }

    // 优先从 ItemDatabase 取本地化名称，找不到时返回枚举名
    private string GetItemDisplayName(ItemType itemType)
    {
        if (ItemDatabase.Instance != null)
        {
            ItemData data = ItemDatabase.Instance.GetItemData(itemType);
            if (data != null && !string.IsNullOrEmpty(data.itemName))
                return data.itemName;
        }
        return itemType.ToString();
    }
}
