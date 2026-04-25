using UnityEngine;

// 建筑配方 ScriptableObject：同时支持「放置到世界」和「合成到背包」两种模式。
// isInventoryRecipe = false → 走 BuildingSystem 世界放置流程；
// isInventoryRecipe = true  → 走 WorkbenchUI 合成流程，结果加入背包。
[CreateAssetMenu(fileName = "BuildingRecipe", menuName = "Game/Building Recipe")]
public class BuildingRecipeSO : ScriptableObject
{
    [Header("基础信息")]
    public string buildingName;
    public Sprite icon;

    [Header("世界放置（isInventoryRecipe = false 时使用）")]
    public GameObject buildingPrefab;

    [Header("合成消耗")]
    public BuildingCostData[] costs;

    [Header("工作台限制")]
    [Tooltip("true = 必须在工作台附近才能合成或放置")]
    public bool requiresWorkbench;

    [Header("合成到背包")]
    [Tooltip("true = 合成结果加入背包；false = 放置到世界（原有建造流程）")]
    public bool isInventoryRecipe;

    [Tooltip("合成结果的物品类型（isInventoryRecipe = true 时有效）")]
    public ItemType resultItemType;

    [Tooltip("每次合成获得的数量，默认 1")]
    public int resultAmount = 1;
}
