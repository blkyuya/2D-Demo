using System;
using UnityEngine;

// 建筑配方中单条材料消耗（物品类型 + 数量）
[Serializable]
public class BuildingCostData
{
    public ItemType itemType;
    public int amount;
}
