using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 背包与箱子统一 UI 管理器。
// Tab 键 → 独立背包面板；靠近箱子按 E → 箱子面板（打开时暂停游戏时间）。
// 快捷栏始终显示，底部武器展示槽实时同步装备状态。
// 拖拽、右键快速移动、分类过滤、翻页等操作统一由此脚本处理，
// ItemSlotUI 只负责视觉显示，不持有业务逻辑。
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance;

    [Header("面板根节点（从 Canvas 拖入）")]
    [SerializeField] private GameObject _standalonePanel;
    [SerializeField] private GameObject _chestModePanel;

    [Header("独立背包（InventoryPanel 内）")]
    [SerializeField] private ItemSlotUI[] _standaloneItemSlots;
    [SerializeField] private Button       _standaloneCloseBtn;

    [Header("箱子面板：关闭按钮与标题")]
    [SerializeField] private Button   _chestCloseBtn;
    [SerializeField] private TMP_Text chestTitleText;

    [Header("箱子面板：装备槽（6）")]
    [SerializeField] private ItemSlotUI[] equipmentSlots;

    [Header("箱子面板：分类标签")]
    [SerializeField] private Button tabAllBtn;
    [SerializeField] private Button tabEquipBtn;
    [SerializeField] private Button tabConsumableBtn;
    [SerializeField] private Button tabMaterialBtn;

    [Header("箱子面板：背包格（12）与翻页")]
    [SerializeField] private ItemSlotUI[] _chestItemSlots;
    [SerializeField] private Button       prevPageBtn;
    [SerializeField] private Button       nextPageBtn;
    [SerializeField] private TMP_Text     pageText;

    [Header("箱子面板：详情区")]
    [SerializeField] private TMP_Text detailNameText;
    [SerializeField] private TMP_Text detailDescText;
    [SerializeField] private TMP_Text detailStatsText;
    [SerializeField] private TMP_Text weightText;
    [SerializeField] private Button   discardButton;
    [SerializeField] private Button   destroyButton;
    [SerializeField] private Button   sortButton;

    [Header("箱子面板：箱内存储（16）")]
    [SerializeField] private ItemSlotUI[] chestSlots;

    [Header("快捷栏（HotbarPanel 内 6 格）")]
    [SerializeField] private ItemSlotUI[] hotbarSlots;

    [Header("当前武器展示槽（与 6 格快捷栏分开）")]
    [Tooltip("复制格子 UI，放到 HotbarPanel 右侧（可与 HotbarPanel 同父级）。ItemSlotUI 的 Owner 设为 WeaponHud，再拖到这里。")]
    [SerializeField] private ItemSlotUI weaponHudSlot;

    [Header("共用：拖拽幽灵图标（InventoryUI 根下）")]
    [SerializeField] private RectTransform dragIconRoot;
    [SerializeField] private Image         dragIconImage;
    [SerializeField] private TMP_Text      dragAmountText;

    [Header("共用：物品说明浮窗")]
    [SerializeField] private GameObject    tooltipPanel;
    [SerializeField] private TMP_Text      tooltipNameText;
    [SerializeField] private TMP_Text      tooltipDescText;
    [SerializeField] private RectTransform tooltipRect;

    [Header("掉落物预制体（世界拾取）")]
    [SerializeField] private GameObject droppedItemPrefab;
    [SerializeField] private float dropDistance  = 1.2f;
    [SerializeField] private int   maxCapacity   = 3600;

    // 当前激活的背包格子：独立模式 → _standaloneItemSlots；箱子模式 → _chestItemSlots
    private ItemSlotUI[] itemSlots;

    private bool   _isOpen;
    private CategoryFilter _filter = CategoryFilter.All;
    private int    _currentPage;
    private const int PageSize = 12;
    private readonly List<(ItemType type, int count)> _filteredItems
        = new List<(ItemType, int)>();
    private ItemSlotUI  _selectedSlot;
    private ItemType?[] _equipped = new ItemType?[6];
    private int         _hotbarSelectedIndex;

    private bool  _isChestMode;
    private Chest _currentChest;

    // tooltip 必须挂到根 Canvas 下，否则会被背包面板的子 Canvas 遮挡
    private Canvas _rootCanvas;

    private ItemSlotUI _dragSourceSlot;
    private bool       _isDragging;
    private bool       _dropHandled;

    [Header("可选：玩家引用（避免 FindObjectOfType）")]
    [SerializeField] private PlayerInventory _playerInventoryRef;
    [SerializeField] private PlayerStats     _playerStatsRef;
    [SerializeField] private PlayerEquipment _playerEquipmentRef;

    private PlayerInventory _playerInventory;
    private PlayerStats     _playerStats;
    private PlayerEquipment _playerEquipment;
    private float           _savedTimeScale = 1f;

    public bool IsOpen => _isOpen;

    private enum CategoryFilter { All, Equipment, Consumable, Material }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _isOpen = false;
        _isChestMode = false;
    }

    private void Start()
    {
        // 优先使用 Inspector 拖入的引用，未填时才走 FindObjectOfType（兼容旧场景）
        _playerInventory = _playerInventoryRef != null ? _playerInventoryRef : FindObjectOfType<PlayerInventory>();
        _playerStats     = _playerStatsRef     != null ? _playerStatsRef     : FindObjectOfType<PlayerStats>();
        _playerEquipment = _playerEquipmentRef != null ? _playerEquipmentRef : FindObjectOfType<PlayerEquipment>();

        if (_standalonePanel != null) _standalonePanel.SetActive(false);
        if (_chestModePanel  != null) _chestModePanel.SetActive(false);
        if (dragIconRoot     != null) dragIconRoot.gameObject.SetActive(false);
        if (tooltipPanel     != null) tooltipPanel.SetActive(false);

        Canvas nearestCanvas = GetComponentInParent<Canvas>();
        _rootCanvas = nearestCanvas != null ? nearestCanvas.rootCanvas : null;
        ConfigureTooltipForTopLayer();

        _standaloneCloseBtn?.onClick.AddListener(CloseAll);
        _chestCloseBtn?.onClick.AddListener(CloseAll);

        tabAllBtn?.onClick.AddListener(()        => SetFilter(CategoryFilter.All));
        tabEquipBtn?.onClick.AddListener(()      => SetFilter(CategoryFilter.Equipment));
        tabConsumableBtn?.onClick.AddListener(() => SetFilter(CategoryFilter.Consumable));
        tabMaterialBtn?.onClick.AddListener(()   => SetFilter(CategoryFilter.Material));

        prevPageBtn?.onClick.AddListener(GoPrevPage);
        nextPageBtn?.onClick.AddListener(GoNextPage);

        discardButton?.onClick.AddListener(DiscardSelectedItem);
        destroyButton?.onClick.AddListener(DestroySelectedItem);
        sortButton?.onClick.AddListener(SortInventory);

        itemSlots = _standaloneItemSlots;

        // Inspector 未拖入武器栏时，运行时自动查找 WeaponHud 类型的格子
        if (weaponHudSlot == null)
        {
            ItemSlotUI[] slots = GetComponentsInChildren<ItemSlotUI>(true);
            foreach (ItemSlotUI s in slots)
            {
                if (s != null && s.OwnerType == SlotOwnerType.WeaponHud)
                {
                    weaponHudSlot = s;
                    break;
                }
            }
        }

        SyncEquipmentAndWeaponHud();
    }

    private void OnEnable()
    {
        if (_playerInventory != null)
            _playerInventory.OnInventoryChanged += OnPlayerInventoryChanged;
    }

    private void OnDisable()
    {
        if (_playerInventory != null)
            _playerInventory.OnInventoryChanged -= OnPlayerInventoryChanged;
    }

    // 背包数量变化时刷新整个面板（包含装备栏同步）
    private void OnPlayerInventoryChanged()
    {
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            if (Time.timeScale == 0f)
                Time.timeScale = 1f;
        }
    }

    private void Update()
    {
        if (PauseMenu.IsPaused) return;

        // Tab 键切换独立背包面板，建造模式和箱子模式下禁止切换
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (BuildingSystem.Instance != null && BuildingSystem.Instance.IsPlacing) return;
            if (_isChestMode) return;
            TogglePanel();
        }

        if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
            CloseAll();

        if (_isOpen) HandleHotbarKeys();

        // tooltip 每帧跟随鼠标移动
        if (tooltipPanel != null && tooltipPanel.activeSelf && tooltipRect != null)
            RepositionTooltip(Input.mousePosition);
    }

    // 切换独立背包面板的显示/隐藏
    public void TogglePanel()
    {
        if (_isOpen && !_isChestMode) CloseAll();
        else if (!_isOpen)            OpenInventory();
    }

    // 打开独立背包模式（Tab 键触发）
    public void OpenInventory()
    {
        _isOpen      = true;
        _isChestMode = false;
        _currentChest = null;
        _filter      = CategoryFilter.All;
        _currentPage = 0;

        itemSlots = _standaloneItemSlots;

        if (_standalonePanel != null) _standalonePanel.SetActive(true);
        if (_chestModePanel  != null) _chestModePanel.SetActive(false);
        if (tooltipPanel     != null) tooltipPanel.SetActive(false);

        RefreshUI();
    }

    // 打开箱子模式（靠近箱子按 E 触发），同时暂停游戏时间
    public void OpenChestMode(Chest chest)
    {
        _currentChest = chest;
        _isChestMode  = true;
        _isOpen       = true;
        _filter       = CategoryFilter.All;
        _currentPage  = 0;

        itemSlots = _chestItemSlots;

        if (_standalonePanel != null) _standalonePanel.SetActive(false);
        if (_chestModePanel  != null) _chestModePanel.SetActive(true);
        if (chestTitleText   != null) chestTitleText.text = "箱子";
        if (tooltipPanel     != null) tooltipPanel.SetActive(false);

        PauseGame();
        RefreshUI();
    }

    // 兼容旧调用
    public void CloseChestMode() => CloseAll();

    // 关闭所有面板，箱子模式下恢复时间
    public void CloseAll()
    {
        bool wasChestMode = _isChestMode;
        _isOpen       = false;
        _isChestMode  = false;
        _currentChest = null;
        _selectedSlot = null;
        ClearAllHighlights();
        ClearDetailPanel();

        if (_standalonePanel != null) _standalonePanel.SetActive(false);
        if (_chestModePanel  != null) _chestModePanel.SetActive(false);
        if (tooltipPanel     != null) tooltipPanel.SetActive(false);

        if (wasChestMode) ResumeGame();

        SyncEquipmentAndWeaponHud();
    }

    public void ClosePanel() => CloseAll();

    // 打开箱子时暂停时间（保存原 timeScale 以便正确恢复）
    private void PauseGame()
    {
        if (Time.timeScale > 0f)
            _savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

    // 关闭箱子时恢复暂停前的时间速度
    private void ResumeGame()
    {
        Time.timeScale = _savedTimeScale > 0f ? _savedTimeScale : 1f;
    }

    // 全量刷新：重建过滤列表，刷新所有区域，更新详情面板
    public void RefreshUI()
    {
        SyncEquipmentAndWeaponHud();
        if (!_isOpen) return;

        BuildFilteredList();
        RefreshItemSlots();
        RefreshEquipmentSlots();
        RefreshHotbar();
        RefreshChestSlots();
        RefreshWeightDisplay();
        RefreshPageButtons();
        RefreshTabHighlights();

        if (_selectedSlot != null && !_selectedSlot.IsEmpty)
            ShowDetail(_selectedSlot.ItemType);
        else
            ClearDetailPanel();
    }

    // 无论背包是否打开都要同步装备数据和底部武器栏
    private void SyncEquipmentAndWeaponHud()
    {
        PushEquipmentToPlayer();
        RefreshWeaponHudSlot();
    }

    // 将 UI 装备格的内容推送到 PlayerEquipment
    private void PushEquipmentToPlayer()
    {
        if (_playerEquipment == null)
            _playerEquipment = _playerEquipmentRef != null ? _playerEquipmentRef : FindObjectOfType<PlayerEquipment>();
        if (_playerEquipment == null) return;
        _playerEquipment.ApplyEquippedSlots(_equipped);
    }

    // 刷新底部武器展示槽，显示第一个已装备的工具
    private void RefreshWeaponHudSlot()
    {
        if (weaponHudSlot == null) return;
        ItemType? tool = GetFirstEquippedToolForWeaponHud();
        if (tool.HasValue)
            weaponHudSlot.SetItem(tool.Value, GetItemIcon(tool.Value), 1);
        else
            weaponHudSlot.SetEmpty();
    }

    // 找到装备格中第一个工具类物品，用于底部武器展示
    private ItemType? GetFirstEquippedToolForWeaponHud()
    {
        for (int i = 0; i < _equipped.Length; i++)
        {
            if (!_equipped[i].HasValue) continue;
            ItemData data = GetItemData(_equipped[i].Value);
            if (data != null && data.category == ItemCategory.Tool)
                return _equipped[i].Value;
        }
        return null;
    }

    private int FindEquippedSlotIndex(ItemType type)
    {
        for (int i = 0; i < _equipped.Length; i++)
        {
            if (_equipped[i].HasValue && _equipped[i].Value == type)
                return i;
        }
        return -1;
    }

    // 右键武器展示槽时将工具归还背包并清空装备格
    private void TryUnequipWeaponHudToInventory()
    {
        if (_playerInventory == null) return;
        ItemType? tool = GetFirstEquippedToolForWeaponHud();
        if (!tool.HasValue) return;
        int idx = FindEquippedSlotIndex(tool.Value);
        if (idx < 0) return;
        _playerInventory.AddItem(tool.Value, 1);
        _equipped[idx] = null;
        RefreshUI();
    }

    // 供存档：序列化装备格，空槽记为 -1
    public int[] GetEquippedSlotsForSave()
    {
        int[] arr = new int[6];
        for (int i = 0; i < 6; i++)
        {
            if (i >= _equipped.Length) { arr[i] = -1; continue; }
            arr[i] = _equipped[i].HasValue ? (int)_equipped[i].Value : -1;
        }
        return arr;
    }

    // 读档恢复：将 int 数组反序列化回 ItemType? 数组，再同步到 PlayerEquipment
    public void LoadEquippedSlotsFromSave(int[] slotInts)
    {
        for (int i = 0; i < _equipped.Length; i++)
            _equipped[i] = null;

        if (slotInts != null && slotInts.Length == 6)
        {
            for (int i = 0; i < 6; i++)
            {
                int v = slotInts[i];
                if (v < 0) { _equipped[i] = null; continue; }
                if (!System.Enum.IsDefined(typeof(ItemType), v)) { _equipped[i] = null; continue; }
                _equipped[i] = (ItemType)v;
            }
        }

        RefreshUI();
    }

    // 切换分类过滤并重置页码
    private void SetFilter(CategoryFilter filter)
    {
        _filter       = filter;
        _currentPage  = 0;
        _selectedSlot = null;
        ClearAllHighlights();
        RefreshUI();
    }

    // 遍历背包所有物品，筛选符合当前分类的条目
    private void BuildFilteredList()
    {
        _filteredItems.Clear();
        if (_playerInventory == null) return;
        foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
        {
            int count = _playerInventory.GetItemCount(type);
            if (count <= 0) continue;
            if (!MatchesFilter(type)) continue;
            _filteredItems.Add((type, count));
        }
    }

    private bool MatchesFilter(ItemType type)
    {
        if (_filter == CategoryFilter.All) return true;
        ItemData data = GetItemData(type);
        if (data == null) return _filter == CategoryFilter.Material;
        switch (_filter)
        {
            case CategoryFilter.Equipment:
                return data.category == ItemCategory.Equipment || data.category == ItemCategory.Tool;
            case CategoryFilter.Consumable:
                return data.category == ItemCategory.Food;
            case CategoryFilter.Material:
                return data.category == ItemCategory.Material || data.category == ItemCategory.Misc;
            default: return true;
        }
    }

    private int TotalPages => Mathf.Max(1, Mathf.CeilToInt((float)_filteredItems.Count / PageSize));

    private void GoPrevPage() { if (_currentPage > 0) { _currentPage--; RefreshUI(); } }
    private void GoNextPage() { if (_currentPage < TotalPages - 1) { _currentPage++; RefreshUI(); } }

    private void RefreshPageButtons()
    {
        if (pageText    != null) pageText.text = $"{_currentPage + 1}/{TotalPages}";
        if (prevPageBtn != null) prevPageBtn.interactable = _currentPage > 0;
        if (nextPageBtn != null) nextPageBtn.interactable = _currentPage < TotalPages - 1;
    }

    // 刷新标签颜色，高亮当前选中的分类
    private void RefreshTabHighlights()
    {
        Color active   = new Color(0.72f, 0.55f, 0.25f);
        Color inactive = new Color(0.40f, 0.28f, 0.14f);
        SetTabColor(tabAllBtn,        _filter == CategoryFilter.All,        active, inactive);
        SetTabColor(tabEquipBtn,      _filter == CategoryFilter.Equipment,  active, inactive);
        SetTabColor(tabConsumableBtn, _filter == CategoryFilter.Consumable, active, inactive);
        SetTabColor(tabMaterialBtn,   _filter == CategoryFilter.Material,   active, inactive);
    }

    private static void SetTabColor(Button btn, bool isActive, Color active, Color inactive)
    {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>();
        if (img != null) img.color = isActive ? active : inactive;
    }

    // 根据当前页码刷新背包格子
    private void RefreshItemSlots()
    {
        if (itemSlots == null) return;
        int startIdx = _currentPage * PageSize;
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (itemSlots[i] == null) continue;
            int dataIdx = startIdx + i;
            if (dataIdx < _filteredItems.Count)
            {
                var (type, count) = _filteredItems[dataIdx];
                itemSlots[i].SetItem(type, GetItemIcon(type), count);
            }
            else
                itemSlots[i].SetEmpty();
        }
    }

    // 刷新装备格（6 格）
    private void RefreshEquipmentSlots()
    {
        if (equipmentSlots == null) return;
        for (int i = 0; i < equipmentSlots.Length; i++)
        {
            if (equipmentSlots[i] == null) continue;
            if (_equipped[i].HasValue)
                equipmentSlots[i].SetItem(_equipped[i].Value, GetItemIcon(_equipped[i].Value), 1);
            else
                equipmentSlots[i].SetEmpty();
        }
    }

    // 刷新快捷栏（按枚举顺序填入有数量的物品）
    private void RefreshHotbar()
    {
        if (hotbarSlots == null || _playerInventory == null) return;
        int idx = 0;
        foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
        {
            if (idx >= hotbarSlots.Length) break;
            if (hotbarSlots[idx] == null) { idx++; continue; }
            int count = _playerInventory.GetItemCount(type);
            if (count > 0) { hotbarSlots[idx].SetItem(type, GetItemIcon(type), count); idx++; }
        }
        for (int i = idx; i < hotbarSlots.Length; i++) hotbarSlots[i]?.SetEmpty();
        for (int i = 0; i < hotbarSlots.Length; i++)
            hotbarSlots[i]?.SetHighlight(i == _hotbarSelectedIndex);
    }

    // 刷新箱子格子（仅箱子模式下执行）
    private void RefreshChestSlots()
    {
        if (!_isChestMode || chestSlots == null || _currentChest == null) return;
        int idx = 0;
        foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
        {
            if (idx >= chestSlots.Length) break;
            int count = _currentChest.GetItemCount(type);
            if (count <= 0) continue;
            chestSlots[idx]?.SetItem(type, GetItemIcon(type), count);
            idx++;
        }
        for (int i = idx; i < chestSlots.Length; i++) chestSlots[i]?.SetEmpty();
    }

    // 刷新重量/容量显示
    private void RefreshWeightDisplay()
    {
        if (weightText == null || _playerInventory == null) return;
        int total = GetTotalItemCount();
        weightText.text = $"总重量 {total}/{maxCapacity}";
    }

    // 刷新详情区（名称、描述、属性）
    private void ShowDetail(ItemType? type)
    {
        if (type == null) { ClearDetailPanel(); return; }
        ItemData data = GetItemData(type.Value);
        if (detailNameText  != null) detailNameText.text  = data != null ? data.itemName : type.Value.ToString();
        if (detailDescText  != null) detailDescText.text  = data?.description ?? "";
        if (detailStatsText != null) detailStatsText.text = BuildStatsText(type.Value, data);
        discardButton?.gameObject.SetActive(true);
        destroyButton?.gameObject.SetActive(true);
    }

    private void ClearDetailPanel()
    {
        if (detailNameText  != null) detailNameText.text  = "选择物品查看详情";
        if (detailDescText  != null) detailDescText.text  = "";
        if (detailStatsText != null) detailStatsText.text = "";
        discardButton?.gameObject.SetActive(false);
        destroyButton?.gameObject.SetActive(false);
    }

    private string BuildStatsText(ItemType type, ItemData data)
    {
        if (data == null) return "";
        switch (data.category)
        {
            case ItemCategory.Tool:      return "挖掘速度 1\n砍伐速度 1";
            case ItemCategory.Equipment: return "防御力 +1";
            case ItemCategory.Food:      return "恢复饥饿 +10";
            default: return "可叠加：" + (data.stackable ? "是" : "否");
        }
    }

    // 数字键 1~6 切换快捷栏高亮
    private void HandleHotbarKeys()
    {
        for (int i = 0; i < 6; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                _hotbarSelectedIndex = i;
                RefreshHotbar();
                break;
            }
        }
    }

    // 左键点击格子：选中并显示详情
    public void OnSlotClicked(ItemSlotUI clickedSlot)
    {
        ClearAllHighlights();
        _selectedSlot = clickedSlot;
        if (_selectedSlot != null && !_selectedSlot.IsEmpty)
        {
            _selectedSlot.SetHighlight(true);
            ShowDetail(_selectedSlot.ItemType);
        }
        else
        {
            _selectedSlot = null;
            ClearDetailPanel();
        }
    }

    // 右键格子：箱子模式下在背包/箱子间转移；普通模式下使用/装备
    public void OnSlotRightClicked(ItemSlotUI clickedSlot)
    {
        if (clickedSlot == null || clickedSlot.IsEmpty || clickedSlot.ItemType == null) return;

        if (clickedSlot.OwnerType == SlotOwnerType.WeaponHud)
        {
            TryUnequipWeaponHudToInventory();
            return;
        }

        ItemType itemType = clickedSlot.ItemType.Value;
        int      amount   = clickedSlot.Amount;

        if (_isChestMode && _currentChest != null && _playerInventory != null)
        {
            if (clickedSlot.OwnerType == SlotOwnerType.PlayerInventory)
            {
                if (_playerInventory.RemoveItem(itemType, amount))
                    _currentChest.AddItem(itemType, amount);
            }
            else if (clickedSlot.OwnerType == SlotOwnerType.ChestInventory)
            {
                if (_currentChest.RemoveItem(itemType, amount))
                    _playerInventory.AddItem(itemType, amount);
            }
            RefreshUI();
            return;
        }
        TryUseOrEquipItem(clickedSlot);
    }

    // 右键使用食物（恢复饥饿）或右键装备工具/装备
    private void TryUseOrEquipItem(ItemSlotUI slot)
    {
        if (slot == null || slot.IsEmpty || slot.ItemType == null) return;
        if (_playerInventory == null) return;
        ItemType type = slot.ItemType.Value;
        ItemData data = GetItemData(type);

        if (data != null && data.category == ItemCategory.Food)
        {
            if (_playerInventory.UseItem(type, 1) && _playerStats != null)
                _playerStats.AddHunger(10);
            RefreshUI();
            return;
        }
        if (data != null && (data.category == ItemCategory.Equipment || data.category == ItemCategory.Tool))
        {
            for (int i = 0; i < _equipped.Length; i++)
            {
                if (!_equipped[i].HasValue)
                {
                    _equipped[i] = type;
                    _playerInventory.RemoveItem(type, 1);
                    RefreshUI();
                    return;
                }
            }
        }
    }

    // 丢弃选中物品：从背包/箱子移除，并在玩家附近生成掉落物
    private void DiscardSelectedItem()
    {
        if (_selectedSlot == null || _selectedSlot.IsEmpty || _selectedSlot.ItemType == null) return;
        if (_playerInventory == null) return;
        ItemType type   = _selectedSlot.ItemType.Value;
        int      amount = _selectedSlot.Amount;
        bool     removed = false;

        if (_selectedSlot.OwnerType == SlotOwnerType.PlayerInventory)
            removed = _playerInventory.RemoveItem(type, amount);
        else if (_selectedSlot.OwnerType == SlotOwnerType.ChestInventory && _currentChest != null)
            removed = _currentChest.RemoveItem(type, amount);

        if (removed) SpawnDroppedItemNearPlayer(type, amount);
        _selectedSlot = null;
        ClearDetailPanel();
        RefreshUI();
    }

    // 销毁选中物品：直接从背包/箱子移除，不生成世界掉落物
    private void DestroySelectedItem()
    {
        if (_selectedSlot == null || _selectedSlot.IsEmpty || _selectedSlot.ItemType == null) return;
        if (_playerInventory == null) return;
        ItemType type   = _selectedSlot.ItemType.Value;
        int      amount = _selectedSlot.Amount;

        if (_selectedSlot.OwnerType == SlotOwnerType.PlayerInventory)
            _playerInventory.RemoveItem(type, amount);
        else if (_selectedSlot.OwnerType == SlotOwnerType.ChestInventory && _currentChest != null)
            _currentChest.RemoveItem(type, amount);

        _selectedSlot = null;
        ClearDetailPanel();
        RefreshUI();
    }

    // 整理背包：重置分类和页码，触发全量刷新（将来可扩展为排序算法）
    private void SortInventory()
    {
        _currentPage  = 0;
        _filter       = CategoryFilter.All;
        _selectedSlot = null;
        ClearDetailPanel();
        RefreshUI();
    }

    // 开始拖拽：记录来源格子，显示跟随鼠标的幽灵图标
    public void BeginDrag(ItemSlotUI sourceSlot)
    {
        if (sourceSlot == null || sourceSlot.IsEmpty || sourceSlot.ItemType == null) return;
        if (sourceSlot.OwnerType == SlotOwnerType.WeaponHud) return;
        _dragSourceSlot = sourceSlot;
        _isDragging     = true;
        _dropHandled    = false;
        sourceSlot.SetDragVisual(true);
        if (dragIconRoot != null) { dragIconRoot.gameObject.SetActive(true); dragIconRoot.position = Input.mousePosition; }
        if (dragIconImage != null) { dragIconImage.enabled = true; dragIconImage.sprite = sourceSlot.CurrentIcon; }
        if (dragAmountText != null) dragAmountText.text = sourceSlot.Amount > 1 ? sourceSlot.Amount.ToString() : "";
    }

    // 拖拽中：幽灵图标跟随鼠标位置
    public void UpdateDrag(Vector2 screenPos)
    {
        if (!_isDragging || dragIconRoot == null) return;
        dragIconRoot.position = screenPos;
    }

    // 拖拽结束：延迟一帧执行，确保 OnDrop 已被目标区域接收
    public void EndDrag()
    {
        Invoke(nameof(FinishDrag), 0f);
    }

    // 实际处理拖拽结束：未落入任何 UI 区域时执行世界丢弃
    private void FinishDrag()
    {
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (!_dropHandled && !overUI) TryDropToWorld();
        _dragSourceSlot?.SetDragVisual(false);
        _isDragging     = false;
        _dragSourceSlot = null;
        _dropHandled    = false;
        if (dragIconRoot != null) dragIconRoot.gameObject.SetActive(false);
    }

    // 拖拽落入背包区域或箱子区域时执行物品转移
    public void OnDroppedToArea(SlotOwnerType targetOwnerType)
    {
        if (!_isDragging || _dragSourceSlot == null || _dragSourceSlot.ItemType == null) return;
        ItemType itemType = _dragSourceSlot.ItemType.Value;
        int      amount   = _dragSourceSlot.Amount;

        if (_dragSourceSlot.OwnerType == targetOwnerType) { _dropHandled = true; return; }

        if (_dragSourceSlot.OwnerType == SlotOwnerType.PlayerInventory &&
            targetOwnerType            == SlotOwnerType.ChestInventory &&
            _currentChest != null && _playerInventory != null)
        {
            if (_playerInventory.RemoveItem(itemType, amount))
                _currentChest.AddItem(itemType, amount);
        }
        else if (_dragSourceSlot.OwnerType == SlotOwnerType.ChestInventory &&
                 targetOwnerType            == SlotOwnerType.PlayerInventory &&
                 _currentChest != null && _playerInventory != null)
        {
            if (_currentChest.RemoveItem(itemType, amount))
                _playerInventory.AddItem(itemType, amount);
        }
        _dropHandled = true;
        RefreshUI();
    }

    // 拖拽到 UI 外部时在玩家附近生成掉落物
    private void TryDropToWorld()
    {
        if (_dragSourceSlot == null || _dragSourceSlot.IsEmpty || _dragSourceSlot.ItemType == null) return;
        ItemType itemType = _dragSourceSlot.ItemType.Value;
        int      amount   = _dragSourceSlot.Amount;
        bool     removed  = false;

        if (_dragSourceSlot.OwnerType == SlotOwnerType.PlayerInventory && _playerInventory != null)
            removed = _playerInventory.RemoveItem(itemType, amount);
        else if (_dragSourceSlot.OwnerType == SlotOwnerType.ChestInventory && _currentChest != null)
            removed = _currentChest.RemoveItem(itemType, amount);

        if (!removed) return;
        SpawnDroppedItemNearPlayer(itemType, amount);
        RefreshUI();
    }

    // 在玩家附近（朝鼠标方向）生成掉落物，优先走对象池
    private void SpawnDroppedItemNearPlayer(ItemType itemType, int amount)
    {
        Vector3    worldPos = GetDropPositionNearPlayer();
        GameObject dropObj  = null;

        if (PoolManager.Instance != null)
            dropObj = PoolManager.Instance.GetObject("DroppedItem", worldPos, Quaternion.identity);
        if (dropObj == null && droppedItemPrefab != null)
            dropObj = Instantiate(droppedItemPrefab, worldPos, Quaternion.identity);
        if (dropObj == null) return;

        DroppedItem di = dropObj.GetComponent<DroppedItem>();
        if (di != null) di.Initialize(itemType, amount, GetItemIcon(itemType));
    }

    private void ClearAllHighlights()
    {
        ClearHighlights(_standaloneItemSlots);
        ClearHighlights(_chestItemSlots);
        ClearHighlights(equipmentSlots);
        ClearHighlights(chestSlots);
        weaponHudSlot?.SetHighlight(false);
    }

    private static void ClearHighlights(ItemSlotUI[] slots)
    {
        if (slots == null) return;
        foreach (var s in slots) s?.SetHighlight(false);
    }

    // 显示物品 tooltip 并定位到鼠标旁边
    public void ShowTooltip(ItemSlotUI slot, Vector2 screenPos)
    {
        if (tooltipPanel == null || slot == null || slot.IsEmpty || slot.ItemType == null) return;
        ItemData data = GetItemData(slot.ItemType.Value);
        if (tooltipNameText != null)
            tooltipNameText.text = data != null ? data.itemName : slot.ItemType.Value.ToString();
        if (tooltipDescText != null)
            tooltipDescText.text = data?.description ?? "无描述";

        tooltipPanel.SetActive(true);
        ConfigureTooltipForTopLayer();
        if (tooltipRect != null) RepositionTooltip(screenPos);
    }

    // 将 tooltip 挂到根 Canvas 最末子级，并设置足够高的 sortingOrder，
    // 防止被背包面板的嵌套 Canvas 或 Mask 遮挡
    private void ConfigureTooltipForTopLayer()
    {
        if (tooltipPanel == null) return;

        if (_rootCanvas == null)
        {
            Canvas nearest = GetComponentInParent<Canvas>();
            _rootCanvas = nearest != null ? nearest.rootCanvas : null;
        }
        if (_rootCanvas == null) return;

        if (tooltipPanel.transform.parent != _rootCanvas.transform)
            tooltipPanel.transform.SetParent(_rootCanvas.transform, false);

        tooltipPanel.transform.SetAsLastSibling();

        Canvas tc = tooltipPanel.GetComponent<Canvas>();
        if (tc == null) tc = tooltipPanel.AddComponent<Canvas>();
        tc.overrideSorting = true;
        tc.sortingOrder = Mathf.Max(32700, _rootCanvas.sortingOrder + 1000);

        if (tooltipPanel.GetComponent<GraphicRaycaster>() == null)
        {
            var gr = tooltipPanel.AddComponent<GraphicRaycaster>();
            gr.blockingMask = 0;
        }
    }

    // tooltip 跟随鼠标，超出屏幕时自动翻转到对侧
    private void RepositionTooltip(Vector2 screenPos)
    {
        const float offsetX = 16f;
        const float offsetY = 12f;

        Canvas.ForceUpdateCanvases();
        float w = tooltipRect.rect.width;
        float h = tooltipRect.rect.height;
        float x = screenPos.x + offsetX;
        float y = screenPos.y + offsetY;

        if (x + w > Screen.width)  x = screenPos.x - w - offsetX;
        if (y + h > Screen.height) y = screenPos.y - h - offsetY;

        tooltipRect.position = new Vector2(x, y);
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    private Sprite   GetItemIcon(ItemType type) => GetItemData(type)?.icon;

    private ItemData GetItemData(ItemType type)
    {
        if (ItemDatabase.Instance == null) return null;
        return ItemDatabase.Instance.GetItemData(type);
    }

    private int GetTotalItemCount()
    {
        if (_playerInventory == null) return 0;
        int total = 0;
        foreach (ItemType t in System.Enum.GetValues(typeof(ItemType)))
            total += _playerInventory.GetItemCount(t);
        return total;
    }

    // 计算背包丢弃的世界坐标：玩家位置 + 朝鼠标方向偏移
    private Vector3 GetDropPositionNearPlayer()
    {
        if (_playerInventory == null) return Vector3.zero;
        Vector3 playerPos     = _playerInventory.transform.position;
        Vector3 mouseWorldPos = Camera.main != null
            ? Camera.main.ScreenToWorldPoint(Input.mousePosition)
            : playerPos + Vector3.down;
        mouseWorldPos.z = 0f;
        Vector3 dir = (mouseWorldPos - playerPos).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = Vector3.down;
        return new Vector3(playerPos.x + dir.x * dropDistance,
                           playerPos.y + dir.y * dropDistance, 0f);
    }
}
