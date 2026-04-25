using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 通用物品格子 UI，支持左键选中、右键快速操作、拖拽转移、悬停提示。
// 所有交互逻辑统一委托给 InventoryUI.Instance（背包+箱子双模式共用），
// 格子本身只负责视觉状态和事件分发，不持有任何业务数据。
public class ItemSlotUI : MonoBehaviour,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("UI 引用")]
    [SerializeField] private Image     iconImage;
    [SerializeField] private TMP_Text  amountText;
    [SerializeField] private GameObject highlightObject;

    [Header("格子归属（在 Inspector 设置）")]
    [SerializeField] private SlotOwnerType ownerType;

    private ItemType? _itemType;
    private int       _amount;
    private bool      _isEmpty = true;

    // 高亮状态拆分：选中 和 悬停 独立管理，互不干扰
    private bool _isSelected = false;
    private bool _isHovered  = false;

    public ItemType?     ItemType    => _itemType;
    public int           Amount      => _amount;
    public bool          IsEmpty     => _isEmpty;
    public SlotOwnerType OwnerType   => ownerType;
    public Sprite        CurrentIcon => iconImage != null ? iconImage.sprite : null;

    // 运行时构建器专用：替代 Inspector 拖拽完成初始化
    public void Setup(SlotOwnerType owner, Image icon, TMP_Text amount, GameObject highlight)
    {
        ownerType       = owner;
        iconImage       = icon;
        amountText      = amount;
        highlightObject = highlight;
        SetEmpty();
    }

    // 清空格子并重置所有视觉状态
    public void SetEmpty()
    {
        _isEmpty   = true;
        _itemType  = null;
        _amount    = 0;
        _isSelected = false;
        _isHovered  = false;

        if (iconImage != null)
        {
            iconImage.enabled = false;
            iconImage.sprite  = null;
            iconImage.color   = Color.white;
        }
        if (amountText != null) amountText.text = "";
        if (highlightObject != null) highlightObject.SetActive(false);
    }

    // 填入物品数据并刷新图标和数量显示（≤1 时不显示数字）
    public void SetItem(ItemType itemType, Sprite icon, int amount)
    {
        _isEmpty  = false;
        _itemType = itemType;
        _amount   = amount;
        // 数据刷新时清除悬停高亮，避免背包内容切换后遗留残影
        _isHovered = false;

        if (iconImage != null)
        {
            iconImage.enabled = true;
            iconImage.sprite  = icon;
            iconImage.color   = Color.white;
        }
        if (amountText != null)
            amountText.text = amount > 1 ? amount.ToString() : "";

        RefreshHighlight();
    }

    // 由 InventoryUI 调用，设置点击选中高亮
    public void SetHighlight(bool show)
    {
        _isSelected = show;
        RefreshHighlight();
    }

    // 高亮只要选中或悬停其中一个为真，就显示
    private void RefreshHighlight()
    {
        bool visible = !_isEmpty && (_isSelected || _isHovered);
        if (highlightObject != null)
            highlightObject.SetActive(visible);
    }

    private void Update()
    {
        // 快捷栏 / 武器展示槽在被其他 UI 遮挡时，OnPointerExit 可能不触发，
        // 每帧检查鼠标是否仍在格子矩形内，防止高亮和 tooltip 持续残留
        if (!_isHovered) return;
        if (ownerType != SlotOwnerType.Hotbar && ownerType != SlotOwnerType.WeaponHud) return;

        RectTransform rt = transform as RectTransform;
        if (rt == null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = null;
        if (canvas != null &&
            (canvas.renderMode == RenderMode.ScreenSpaceCamera || canvas.renderMode == RenderMode.WorldSpace))
            cam = canvas.worldCamera;

        if (!RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, cam))
        {
            _isHovered = false;
            RefreshHighlight();
            InventoryUI.Instance?.HideTooltip();
        }
    }

    // 拖拽时降低透明度，表示"正在被拖走"
    public void SetDragVisual(bool dragging)
    {
        if (iconImage != null)
            iconImage.color = dragging ? new Color(1f, 1f, 1f, 0.4f) : Color.white;
    }

    // 左键选中，右键快速操作（如装备、物品交换）
    public void OnPointerClick(PointerEventData eventData)
    {
        if (InventoryUI.Instance == null) return;
        if (eventData.button == PointerEventData.InputButton.Left)
            InventoryUI.Instance.OnSlotClicked(this);
        else if (eventData.button == PointerEventData.InputButton.Right)
            InventoryUI.Instance.OnSlotRightClicked(this);
    }

    // 开始拖拽：清除悬停状态，通知 InventoryUI 创建幽灵图标
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (InventoryUI.Instance == null || IsEmpty || _itemType == null) return;
        _isHovered = false;
        RefreshHighlight();
        InventoryUI.Instance.HideTooltip();
        InventoryUI.Instance.BeginDrag(this);
    }

    // 拖拽中：更新幽灵图标跟随鼠标位置
    public void OnDrag(PointerEventData eventData)
    {
        InventoryUI.Instance?.UpdateDrag(eventData.position);
    }

    // 拖拽结束：通知 InventoryUI 判断落点并执行数据交换
    public void OnEndDrag(PointerEventData eventData)
    {
        InventoryUI.Instance?.EndDrag();
    }

    // 悬停进入：显示高亮和物品 tooltip
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsEmpty || _itemType == null) return;
        _isHovered = true;
        RefreshHighlight();
        InventoryUI.Instance?.ShowTooltip(this, eventData.position);
    }

    // 悬停离开：清除悬停高亮，但保留点击选中状态
    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        RefreshHighlight();
        InventoryUI.Instance?.HideTooltip();
    }
}
