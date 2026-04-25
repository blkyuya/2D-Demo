using System;

// 完整游戏存档：主格式为 JSON（JsonUtility + persistentDataPath），营火列表用数组以便 Unity 能序列化。
// 旧版二进制仍可读入并迁移为 JSON（见 SaveManager / BinarySaveIO）。
[Serializable]
public class SaveData
{
    // 存档格式版本，后续加字段可据此做分支兼容
    public int saveFormatVersion = 1;

    public float playerPosX;
    public float playerPosY;
    public float playerPosZ;

    public int playerHealth;
    public int playerHunger;

    public float currentTimeNormalized;

    public InventorySaveData inventory;

    // 必须用数组：JsonUtility 不序列化 List<>
    public CampfireSaveData[] campfires = Array.Empty<CampfireSaveData>();

    public int currentWoodCount;
    public bool woodQuestCompleted;
    public bool campfireQuestCompleted;
    public bool surviveNightQuestCompleted;
    public bool allQuestsCompleted;

    // 装备栏 6 格：-1 表示空，否则为 ItemType 整型（枚举顺序与存档绑定，勿改序）
    public int[] equippedItemTypeOrMinusOne = new int[6] { -1, -1, -1, -1, -1, -1 };
}
