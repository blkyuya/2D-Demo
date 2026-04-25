using System;

// 场景中已放置建筑的存档数据（配方名 + 世界坐标 + 营火燃料）
[Serializable]
public class PlacedBuildingSaveData
{
    public string recipeName;
    public float posX;
    public float posY;
    public float posZ;

    // 只有营火类型有燃料，其他建筑此字段为 0
    public float campfireFuel;
}
