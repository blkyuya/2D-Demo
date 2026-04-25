using UnityEngine;

// 早期建造配方结构体（已被 BuildingRecipeSO 替代，此类仍保留以兼容旧场景引用）。
// 新建配方请使用 BuildingRecipeSO（ScriptableObject），不要在此结构体上扩展字段。
[System.Serializable]
public class BuildingRecipe
{
    public string     buildingName;
    public GameObject buildingPrefab;
    public Sprite     icon;

    [Header("消耗材料")]
    public int woodCost;
    public int stoneCost;

    [Header("解锁条件")]
    public bool requiresWorkbench;
}
