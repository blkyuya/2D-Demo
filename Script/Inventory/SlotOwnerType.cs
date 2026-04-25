// 槽位归属类型，用于在拖拽/点击事件中区分数据来源（背包、箱子、装备槽等）
public enum SlotOwnerType
{
    PlayerInventory,    // 玩家背包物品槽
    ChestInventory,     // 箱子物品槽
    Equipment,          // 玩家装备槽
    Hotbar,             // 快捷栏槽
    WeaponHud,          // 底部当前武器展示槽（右键可卸下）
}
