using UnityEngine;

// 标记一个建筑实体的来源配方和是否由玩家放置。
// 同时负责在 Awake 时把自身及子物体的 SpriteRenderer 设为 Entity 层，
// 确保读档恢复和运行时放置两种场景下排序层都正确。
public class PlacedBuilding : MonoBehaviour
{
    [SerializeField] private BuildingRecipeSO sourceRecipe;
    [SerializeField] private bool isPlayerPlaced;

    public BuildingRecipeSO SourceRecipe => sourceRecipe;
    public bool IsPlayerPlaced => isPlayerPlaced;

    // 激活时立刻修正排序层，兼容读档恢复（Instantiate 后 Awake 会再次执行）
    private void Awake()
    {
        ApplyEntitySortingLayer(gameObject);
    }

    // 运行时放置时由 BuildingSystem 调用，写入配方和放置来源标记
    public void Initialize(BuildingRecipeSO recipe, bool playerPlaced = true)
    {
        sourceRecipe = recipe;
        isPlayerPlaced = playerPlaced;
    }

    // 将对象及所有子物体的 SpriteRenderer 设为 Entity 排序层
    private static void ApplyEntitySortingLayer(GameObject obj)
    {
        if (obj == null) return;
        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in renderers)
            sr.sortingLayerName = "Entity";
    }
}
