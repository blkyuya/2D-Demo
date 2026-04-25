using UnityEngine;

// 世界中的可拾取掉落物：按 E 拾取后加入背包，然后通过对象池归还（找不到池时销毁）。
public class DroppedItem : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemType itemType;
    [SerializeField] private int amount = 1;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // 每次激活（含对象池复用）都设为 Entity 层，确保在地表上方可见
        ApplyEntitySortingLayer(gameObject);

        RefreshVisual();
    }

    // 由资源节点或敌人死亡时调用：设置物品类型、数量和图标
    public void Initialize(ItemType type, int count, Sprite sprite)
    {
        itemType = type;
        amount = count;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }
            else
            {
                // 传入 null 时回退到 ItemDatabase 中的 icon
                ItemData data = ItemDatabase.Instance != null ? ItemDatabase.Instance.GetItemData(itemType) : null;
                if (data != null)
                    spriteRenderer.sprite = data.icon;
            }
        }
    }

    public string GetInteractionText()
    {
        return $"按 E 拾取 {GetItemDisplayName()} x{amount}";
    }

    // 拾取：加入背包，广播事件，归还到对象池或销毁
    public void Interact()
    {
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory == null)
            return;

        playerInventory.AddItem(itemType, amount);
        GameEvents.RaiseItemPickedUp(itemType, amount);

        PooledObject pooledObject = GetComponent<PooledObject>();
        if (pooledObject != null)
            pooledObject.ReturnToPool();
        else
            Destroy(gameObject);
    }

    // Awake 时如果还没有 Sprite 则从 ItemDatabase 补图标
    private void RefreshVisual()
    {
        if (spriteRenderer == null)
            return;

        if (spriteRenderer.sprite != null)
            return;

        ItemData data = ItemDatabase.Instance != null ? ItemDatabase.Instance.GetItemData(itemType) : null;
        if (data != null)
            spriteRenderer.sprite = data.icon;
    }

    // 设置 Entity 排序层
    private static void ApplyEntitySortingLayer(GameObject obj)
    {
        if (obj == null) return;
        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in renderers)
            sr.sortingLayerName = "Entity";
    }

    // 优先从 ItemDatabase 取本地化名称，兜底用硬编码中文名
    public string GetItemDisplayName()
    {
        if (ItemDatabase.Instance != null)
        {
            ItemData data = ItemDatabase.Instance.GetItemData(itemType);
            if (data != null && !string.IsNullOrEmpty(data.itemName))
                return data.itemName;
        }

        return GetItemName();
    }

    // 硬编码兜底名称（ItemDatabase 未初始化时使用）
    private string GetItemName()
    {
        switch (itemType)
        {
            case ItemType.Wood:       return "木头";
            case ItemType.Stone:      return "石头";
            case ItemType.Berry:      return "浆果";
            case ItemType.InsectWing: return "虫翼";
            case ItemType.BugShell:   return "虫壳";
            case ItemType.VenomSac:   return "毒囊";
            default:                  return "物品";
        }
    }
}
