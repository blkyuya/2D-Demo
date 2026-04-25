using UnityEngine;
using UnityEngine.EventSystems;

// 拖拽放置区域检测：挂在背包/箱子的背景面板上，捕获"拖到区域"事件。
// 不处理任何数据逻辑，统一委托给 InventoryUI 执行实际的物品转移。
public class InventoryDropArea : MonoBehaviour, IDropHandler
{
    [SerializeField] private SlotOwnerType ownerType;

    public SlotOwnerType OwnerType => ownerType;

    // 运行时构建器专用：在代码中设置归属类型（替代 Inspector 设置）
    public void SetOwnerType(SlotOwnerType type) => ownerType = type;

    // 有物品拖入此区域时触发，由 InventoryUI 判断数据如何转移
    public void OnDrop(PointerEventData eventData)
    {
        InventoryUI.Instance?.OnDroppedToArea(ownerType);
    }
}
