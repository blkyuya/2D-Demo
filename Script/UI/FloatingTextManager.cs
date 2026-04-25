using UnityEngine;

// 飘字管理器：监听物品拾取事件，从对象池取出飘字 UI 并播放动画。
// 飘字由 FloatingTextUI 自行动画和归池，此脚本只负责触发。
public class FloatingTextManager : MonoBehaviour
{
    [Header("飘字父节点（UI Canvas 内的 RectTransform）")]
    [SerializeField] private RectTransform floatingTextParent;

    [Header("生成位置（相对父节点的本地坐标）")]
    [SerializeField] private Vector2 spawnPosition = new Vector2(0f, 0f);

    [Header("拾取提示颜色")]
    [SerializeField] private Color itemPickupColor = Color.yellow;

    private void OnEnable()
    {
        GameEvents.OnItemPickedUp += HandleItemPickedUp;
    }

    private void OnDisable()
    {
        GameEvents.OnItemPickedUp -= HandleItemPickedUp;
    }

    // 收到拾取事件后拼接文本并触发飘字
    private void HandleItemPickedUp(ItemType itemType, int amount)
    {
        string itemName = GetItemDisplayName(itemType);
        SpawnFloatingText($"+{amount} {itemName}", itemPickupColor);
    }

    // 从对象池取飘字对象，设置父节点和位置后播放
    private void SpawnFloatingText(string message, Color color)
    {
        if (PoolManager.Instance == null) return;

        GameObject obj = PoolManager.Instance.GetObject("FloatingText", Vector3.zero, Quaternion.identity);
        if (obj == null) return;

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect != null && floatingTextParent != null)
        {
            rect.SetParent(floatingTextParent, false);
            rect.anchoredPosition = spawnPosition;
        }

        FloatingTextUI floatingText = obj.GetComponent<FloatingTextUI>();
        if (floatingText != null)
            floatingText.Play(message, color);
    }

    // 优先从 ItemDatabase 取本地化名称，兜底硬编码
    private string GetItemDisplayName(ItemType itemType)
    {
        if (ItemDatabase.Instance != null)
        {
            ItemData data = ItemDatabase.Instance.GetItemData(itemType);
            if (data != null && !string.IsNullOrEmpty(data.itemName))
                return data.itemName;
        }

        switch (itemType)
        {
            case ItemType.Wood:  return "木头";
            case ItemType.Stone: return "石头";
            case ItemType.Berry: return "浆果";
            default:             return "物品";
        }
    }
}
