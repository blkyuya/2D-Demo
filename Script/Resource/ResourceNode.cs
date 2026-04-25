using System.Collections;
using UnityEngine;

// 可采集资源节点（树木、石头等），实现 IInteractable + IHarvestable 两个接口。
// 采集需要「击打次数」，装备了对应工具时每次 Swing 可消耗更多段，实现「有斧砍一刀」的效果。
// 可恢复资源采空后启动协程等待重生，而非销毁，节省 Instantiate 开销。
public class ResourceNode : MonoBehaviour, IInteractable, IHarvestable
{
    [Header("基础信息")]
    public string resourceName = "树木";

    [Header("采集配置")]
    public int maxHitCount = 3;
    public bool destroyWhenDepleted = true;

    [Header("可恢复资源配置")]
    public bool canRegrow = false;
    public float regrowTime = 10f;

    [Header("外观配置")]
    public Sprite normalSprite;
    public Sprite depletedSprite;

    [Header("掉落配置")]
    public GameObject dropPrefab;
    public ItemType dropItemType = ItemType.Wood;
    public int minDropCount = 1;
    public int maxDropCount = 1;

    [Header("掉落范围")]
    public Vector2 dropOffsetMin = new Vector2(-0.25f, -0.2f);
    public Vector2 dropOffsetMax = new Vector2(0.25f, 0.2f);

    [Header("工具需求（false = 空手即可采集）")]
    public bool requiresTool = false;
    public ItemType requiredToolType = ItemType.WoodenAxe;

    private int _currentHitCount;
    private bool _isDepleted = false;
    private SpriteRenderer _spriteRenderer;

    // 缓存引用，避免 CanHarvest 每帧 FindObjectOfType
    private PlayerInventory _playerInventory;
    private PlayerEquipment _playerEquipment;

    private void Awake()
    {
        _currentHitCount = maxHitCount;
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // 与玩家共用 Y 轴排序层，才能用 Order 做正确的前后遮挡
        ApplyWorldYAlignedSortingLayer(gameObject);
        EnsureSpriteStableYSort2D(gameObject);

        if (_spriteRenderer != null && normalSprite != null)
            _spriteRenderer.sprite = normalSprite;
    }

    private void Start()
    {
        _playerInventory = FindObjectOfType<PlayerInventory>();
    }

    // 每次按 E 触发：按工具情况计算本次消耗段数，达到 0 时触发采集完成
    public void Interact()
    {
        if (_isDepleted)
            return;

        if (requiresTool && !HasRequiredTool())
            return;

        int swing = GetHarvestDamageThisSwing();
        _currentHitCount -= swing;

        if (_currentHitCount <= 0)
            Harvest();
    }

    // 计算本次 Swing 消耗的击打段数：
    // 装备了对应工具时读 ItemData.harvestHitsPerSwing，否则每次 1 段
    private int GetHarvestDamageThisSwing()
    {
        if (_playerInventory == null)
            _playerInventory = FindObjectOfType<PlayerInventory>();
        if (_playerEquipment == null)
            _playerEquipment = FindObjectOfType<PlayerEquipment>();

        if (requiresTool)
        {
            if (ItemDatabase.Instance == null)
                return 1;

            bool inBag    = _playerInventory != null && _playerInventory.HasEnoughItem(requiredToolType, 1);
            bool equipped = _playerEquipment != null && _playerEquipment.IsItemEquipped(requiredToolType);

            if (!inBag && !equipped)
                return 1;

            ItemData toolData = ItemDatabase.Instance.GetItemData(requiredToolType);
            if (toolData == null)
                return 1;

            // 只有装备在装备栏才享受多段加成；仅在背包每次 1 段
            if (!equipped)
                return 1;

            return Mathf.Max(Mathf.Max(1, toolData.harvestHitsPerSwing), maxHitCount);
        }

        // 没有设置工具需求时，装备木斧也享受多段（兼容旧预制体）
        if (ItemDatabase.Instance != null && _playerEquipment != null
            && _playerEquipment.IsItemEquipped(ItemType.WoodenAxe))
        {
            ItemData axeData = ItemDatabase.Instance.GetItemData(ItemType.WoodenAxe);
            if (axeData != null && maxHitCount > 1)
                return Mathf.Max(Mathf.Max(1, axeData.harvestHitsPerSwing), maxHitCount);
        }

        return 1;
    }

    // 根据状态返回提示文本：已耗尽 / 缺工具 / 正常采集
    public string GetInteractionText()
    {
        if (_isDepleted)
            return resourceName + " 已采空";

        if (requiresTool && !HasRequiredTool())
            return $"需要【{GetToolDisplayName()}】才能采集 {resourceName}";

        return "按 E 采集 " + resourceName;
    }

    // 采集完成：生成掉落物，决定销毁还是进入耗尽状态（可重生时走协程）
    private void Harvest()
    {
        SpawnDrop();

        if (destroyWhenDepleted)
        {
            Destroy(gameObject);
            return;
        }

        _isDepleted = true;
        ChangeToDepletedSprite();

        if (canRegrow)
            StartCoroutine(RegrowCoroutine());
    }

    // 优先从对象池取掉落物，找不到时降级用 Instantiate
    private void SpawnDrop()
    {
        if (dropPrefab == null)
            return;

        int dropCount = Random.Range(minDropCount, maxDropCount + 1);

        for (int i = 0; i < dropCount; i++)
        {
            float offsetX = Random.Range(dropOffsetMin.x, dropOffsetMax.x);
            float offsetY = Random.Range(dropOffsetMin.y, dropOffsetMax.y);
            Vector3 spawnPosition = transform.position + new Vector3(offsetX, offsetY, 0f);

            GameObject dropObj = null;

            if (PoolManager.Instance != null)
                dropObj = PoolManager.Instance.GetObject("DroppedItem", spawnPosition, Quaternion.identity);

            if (dropObj == null)
                dropObj = Instantiate(dropPrefab, spawnPosition, Quaternion.identity);

            if (dropObj == null)
                continue;

            // 掉落物在 Entity 层，能在草地和角色上方被看见
            ApplyEntitySortingLayerForDrops(dropObj);

            DroppedItem droppedItem = dropObj.GetComponent<DroppedItem>();
            if (droppedItem != null)
            {
                Sprite icon = null;
                if (ItemDatabase.Instance != null)
                {
                    ItemData itemData = ItemDatabase.Instance.GetItemData(dropItemType);
                    if (itemData != null)
                        icon = itemData.icon;
                }

                droppedItem.Initialize(dropItemType, 1, icon);
            }
        }
    }

    // 切换到耗尽外观
    private void ChangeToDepletedSprite()
    {
        if (_spriteRenderer != null && depletedSprite != null)
            _spriteRenderer.sprite = depletedSprite;
    }

    // 切换回正常外观
    private void ChangeToNormalSprite()
    {
        if (_spriteRenderer != null && normalSprite != null)
            _spriteRenderer.sprite = normalSprite;
    }

    // 等待指定时间后资源重生
    private IEnumerator RegrowCoroutine()
    {
        yield return new WaitForSeconds(regrowTime);

        _isDepleted = false;
        _currentHitCount = maxHitCount;
        ChangeToNormalSprite();
    }

    // IHarvestable 实现：已耗尽或缺工具时返回 false
    public bool CanHarvest()
    {
        if (_isDepleted) return false;
        if (requiresTool && !HasRequiredTool()) return false;
        return true;
    }

    // 检查玩家背包或装备栏是否有所需工具
    private bool HasRequiredTool()
    {
        if (_playerInventory == null)
            _playerInventory = FindObjectOfType<PlayerInventory>();
        if (_playerEquipment == null)
            _playerEquipment = FindObjectOfType<PlayerEquipment>();

        if (_playerInventory != null && _playerInventory.HasEnoughItem(requiredToolType, 1))
            return true;
        return _playerEquipment != null && _playerEquipment.IsItemEquipped(requiredToolType);
    }

    // 获取工具的显示名称（优先 ItemDatabase，兜底枚举名）
    private string GetToolDisplayName()
    {
        if (ItemDatabase.Instance != null)
        {
            ItemData data = ItemDatabase.Instance.GetItemData(requiredToolType);
            if (data != null && !string.IsNullOrEmpty(data.itemName))
                return data.itemName;
        }
        return requiredToolType.ToString();
    }

    private const string WorldYAlignedSortingLayerName = "Objects";

    private static void ApplyEntitySortingLayerForDrops(GameObject obj)
    {
        if (obj == null) return;
        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in renderers)
            sr.sortingLayerName = "Entity";
    }

    // 与玩家共用 Y 轴排序层，确保树木和角色能正确比较前后关系
    private static void ApplyWorldYAlignedSortingLayer(GameObject obj)
    {
        if (obj == null) return;
        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in renderers)
            sr.sortingLayerName = WorldYAlignedSortingLayerName;
    }

    // 确保资源节点有 SpriteStableYSort2D 组件，没有就自动添加
    private static void EnsureSpriteStableYSort2D(GameObject obj)
    {
        if (obj == null) return;
        if (obj.GetComponent<SpriteStableYSort2D>() != null) return;
        obj.AddComponent<SpriteStableYSort2D>();
    }
}
