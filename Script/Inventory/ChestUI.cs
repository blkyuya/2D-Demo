using UnityEngine;

// 箱子 UI 的桥接层：将 Chest.Interact() 的打开/关闭请求转发给 InventoryUI。
// Awake 时会销毁旧版 ChestUI 留下的视觉子节点，防止与新版 InventoryUI 面板重叠。
public class ChestUI : MonoBehaviour
{
    public static ChestUI Instance;

    private void Awake()
    {
        Instance = this;

        // 清理旧版遗留的视觉子节点（面板、格子、按钮等）
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
    }

    // 打开箱子：通知 InventoryUI 切换到箱子模式
    public void OpenChest(Chest chest)
    {
        InventoryUI.Instance?.OpenChestMode(chest);
    }

    // 关闭箱子：通知 InventoryUI 关闭所有面板
    public void CloseChest()
    {
        InventoryUI.Instance?.CloseChestMode();
    }

    public bool IsOpen => InventoryUI.Instance != null && InventoryUI.Instance.IsOpen;

    // 以下为兼容旧调用的转发接口
    public void OnSlotClicked(ItemSlotUI slot)      => InventoryUI.Instance?.OnSlotClicked(slot);
    public void OnSlotRightClicked(ItemSlotUI slot) => InventoryUI.Instance?.OnSlotRightClicked(slot);
    public void BeginDrag(ItemSlotUI slot)           => InventoryUI.Instance?.BeginDrag(slot);
    public void UpdateDrag(Vector2 pos)              => InventoryUI.Instance?.UpdateDrag(pos);
    public void EndDrag()                            => InventoryUI.Instance?.EndDrag();
    public void OnDroppedToArea(SlotOwnerType type)  => InventoryUI.Instance?.OnDroppedToArea(type);
    public void RefreshUI()                          => InventoryUI.Instance?.RefreshUI();
}
