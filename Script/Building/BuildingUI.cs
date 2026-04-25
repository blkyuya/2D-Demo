using UnityEngine;
using TMPro;
using System.Text;

// 建造模式 UI：显示建筑蓝图槽、消耗费用文本和警告提示
public class BuildingUI : MonoBehaviour
{
    public static BuildingUI Instance;

    public GameObject buildingPanel;

    [Header("蓝图槽位")]
    public BuildingUISlot[] slots;

    [Header("文字")]
    public TMP_Text costText;
    public TMP_Text warningText;
    public TMP_Text selectedBuildingText;

    private void Awake()
    {
        Instance = this;
    }

    // 隐藏 UI，等建造模式激活后再显示
    private void Start()
    {
        ShowBuildingUI(false);
    }

    // 刷新蓝图槽图标、选中高亮和费用文本
    public void UpdateBlueprintUI(BuildingRecipeSO[] recipes, int selectedIndex)
    {
        if (slots != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                bool hasRecipe = recipes != null && i < recipes.Length && recipes[i] != null;
                Sprite icon = hasRecipe ? recipes[i].icon : null;
                bool selected = hasRecipe && i == selectedIndex;

                if (slots[i] != null)
                    slots[i].SetData(icon, selected, hasRecipe);
            }
        }

        if (costText != null)
            costText.text = GetCostText(recipes, selectedIndex);

        if (selectedBuildingText != null)
        {
            bool valid = recipes != null && recipes.Length > 0
                         && selectedIndex >= 0 && selectedIndex < recipes.Length
                         && recipes[selectedIndex] != null;

            selectedBuildingText.text = valid ? recipes[selectedIndex].buildingName : "";
        }
    }

    // 拼接当前选中配方的消耗文本
    private string GetCostText(BuildingRecipeSO[] recipes, int selectedIndex)
    {
        if (recipes == null || recipes.Length == 0
            || selectedIndex < 0 || selectedIndex >= recipes.Length)
            return "";

        BuildingRecipeSO recipe = recipes[selectedIndex];
        if (recipe == null || recipe.costs == null || recipe.costs.Length == 0)
            return "";

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < recipe.costs.Length; i++)
        {
            BuildingCostData cost = recipe.costs[i];
            if (cost == null)
                continue;

            string itemName = GetItemDisplayName(cost.itemType);
            sb.Append($"{itemName} {cost.amount}");

            if (i < recipe.costs.Length - 1)
                sb.Append("   ");
        }

        return sb.ToString();
    }

    // 优先从 ItemDatabase 取本地化名称，找不到时用硬编码中文名兜底
    private string GetItemDisplayName(ItemType itemType)
    {
        if (ItemDatabase.Instance != null)
        {
            ItemData data = ItemDatabase.Instance.GetItemData(itemType);
            if (data != null && !string.IsNullOrEmpty(data.itemName))
                return data.itemName;
        }

        switch (itemType)
        {
            case ItemType.Wood:  return "木头";
            case ItemType.Stone: return "石头";
            case ItemType.Berry: return "浆果";
            default:             return "物品";
        }
    }

    // 显示或隐藏建造面板
    public void ShowBuildingUI(bool show)
    {
        if (buildingPanel != null)
            buildingPanel.SetActive(show);

        if (!show)
            HideWarning();
    }

    // 显示警告文本（材料不足 / 位置无效等）
    public void ShowWarning(string message)
    {
        if (warningText != null)
        {
            warningText.gameObject.SetActive(true);
            warningText.text = message;
        }
    }

    // 隐藏警告文本
    public void HideWarning()
    {
        if (warningText != null)
            warningText.gameObject.SetActive(false);
    }
}
