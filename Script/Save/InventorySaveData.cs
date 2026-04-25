using System;

// 背包存档数据（目前只持久化三种基础资源，可按需扩展）
[Serializable]
public class InventorySaveData
{
    public int woodCount;
    public int stoneCount;
    public int berryCount;
}
