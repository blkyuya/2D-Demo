using UnityEngine;

// 物品数据 ScriptableObject：存储所有物品的基础属性。
// 所有物品都用同一个类型，通过 ItemCategory 和各字段区分行为差异，
// 避免为每类物品创建单独的 SO 类导致类型爆炸。
[CreateAssetMenu(fileName = "NewItemData", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("基础信息")]
    public string itemId;
    public string itemName;
    [TextArea] public string description;

    [Header("显示")]
    public Sprite icon;
    public GameObject worldPrefab;

    [Header("分类")]
    public ItemCategory category;

    [Header("堆叠设置")]
    public bool stackable = true;
    public int maxStack = 99;

    [Header("手持显示（角色装备时在手上叠加武器贴图）")]
    [Tooltip("只有此字段有值时才会在手上画；空则该工具装备后不在手上显示（避免火把等误显示）")]
    public Sprite handheldSprite;

    [Tooltip("相对 WeaponHandAnchor 的本地偏移（面向右时；朝左时脚本自动镜像 X）")]
    public Vector2 handheldLocalOffset = new Vector2(0.32f, 0.06f);

    [Tooltip("相对身体 SpriteRenderer 的排序增量，保证武器在身体前一层")]
    public int handheldSortingOrderOffset = 1;

    [Header("工具战斗 / 采集（仅 ItemCategory.Tool 时读取）")]
    [Tooltip("装备此工具时，近战伤害在基础值上额外增加的点数")]
    public int meleeDamageBonus = 0;

    [Tooltip("装备后用于需要该工具的资源节点时，每次 Swing 消耗的击打段数；≥3 可一斧砍倒 3 击树")]
    public int harvestHitsPerSwing = 1;

    // 返回手持显示 Sprite（只有 handheldSprite 有值时才显示）
    public Sprite GetHandheldDisplaySprite()
    {
        return handheldSprite;
    }
}
