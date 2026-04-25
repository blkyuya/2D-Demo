// 物品类型枚举。
// 注意：该枚举值会被写入存档文件，绝对不能改变已有枚举的顺序或删除枚举项，
// 否则会导致老存档加载错误。新增物品只能追加到末尾。
public enum ItemType
{
    // 基础采集材料
    Wood = 0,
    Stone = 1,
    Berry = 2,

    // 怪物掉落材料
    InsectWing = 10,
    BugShell = 11,
    VenomSac = 12,

    // 中间合成材料
    HardWood = 20,
    StoneChip = 21,
    Thread = 22,
    Leather = 23,
    Herb = 24,

    // 工具与消耗品
    WoodenAxe = 30,
    Torch = 31,
    AntiToxin = 32,
    SimpleBag = 33,

    // 高级建筑物品
    AdvancedCampfire = 40,
}
