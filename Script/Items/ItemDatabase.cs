using UnityEngine;

// 物品数据库：Inspector 中拖入各 ItemData SO，运行时通过 ItemType 枚举查询。
// 为什么不用字典：物品数量固定且少，switch 的编译结果比字典哈希查找更高效。
public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance;

    [Header("基础资源")]
    [SerializeField] private ItemData woodData;
    [SerializeField] private ItemData stoneData;
    [SerializeField] private ItemData berryData;

    [Header("怪物掉落物")]
    [SerializeField] private ItemData insectWingData;
    [SerializeField] private ItemData bugShellData;
    [SerializeField] private ItemData venomSacData;

    [Header("合成材料")]
    [SerializeField] private ItemData hardWoodData;
    [SerializeField] private ItemData stoneChipData;
    [SerializeField] private ItemData threadData;
    [SerializeField] private ItemData leatherData;
    [SerializeField] private ItemData herbData;

    [Header("工具与消耗品")]
    [SerializeField] private ItemData woodenAxeData;
    [SerializeField] private ItemData torchData;
    [SerializeField] private ItemData antiToxinData;
    [SerializeField] private ItemData simpleBagData;

    [Header("高级建筑")]
    [SerializeField] private ItemData advancedCampfireData;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // 根据 ItemType 枚举返回对应的 ItemData（找不到返回 null）
    public ItemData GetItemData(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Wood:             return woodData;
            case ItemType.Stone:            return stoneData;
            case ItemType.Berry:            return berryData;
            case ItemType.InsectWing:       return insectWingData;
            case ItemType.BugShell:         return bugShellData;
            case ItemType.VenomSac:         return venomSacData;

            case ItemType.HardWood:         return hardWoodData;
            case ItemType.StoneChip:        return stoneChipData;
            case ItemType.Thread:           return threadData;
            case ItemType.Leather:          return leatherData;
            case ItemType.Herb:             return herbData;

            case ItemType.WoodenAxe:        return woodenAxeData;
            case ItemType.Torch:            return torchData;
            case ItemType.AntiToxin:        return antiToxinData;
            case ItemType.SimpleBag:        return simpleBagData;

            case ItemType.AdvancedCampfire: return advancedCampfireData;

            default:                        return null;
        }
    }
}
