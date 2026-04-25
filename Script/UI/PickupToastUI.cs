using System.Collections;
using TMPro;
using UnityEngine;

// 拾取提示横幅：监听物品拾取事件，短暂显示"获得 xxx x1"后自动隐藏。
// 与飘字不同，此组件固定位置显示，适合作为持久性状态提示。
public class PickupToastUI : MonoBehaviour
{
    [Header("提示容器")]
    [SerializeField] private GameObject contentRoot;

    [Header("提示文本")]
    [SerializeField] private TMP_Text messageText;

    [Header("显示时长（秒）")]
    [SerializeField] private float displayDuration = 1.5f;

    private Coroutine _hideCoroutine;

    private void OnEnable()
    {
        GameEvents.OnItemPickedUp += HandleItemPickedUp;
    }

    private void OnDisable()
    {
        GameEvents.OnItemPickedUp -= HandleItemPickedUp;
    }

    private void Start()
    {
        HideImmediately();
    }

    // 收到拾取事件：更新文本，显示面板，重置隐藏计时器
    private void HandleItemPickedUp(ItemType itemType, int amount)
    {
        if (messageText == null) return;
        messageText.text = $"获得 {GetItemDisplayName(itemType)} x{amount}";
        if (contentRoot != null) contentRoot.SetActive(true);

        if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
        _hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    // 等待 displayDuration 秒后自动隐藏
    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        HideImmediately();
    }

    // 立刻清空并隐藏（初始化和手动关闭时使用）
    private void HideImmediately()
    {
        if (messageText != null) messageText.text = "";
        if (contentRoot != null) contentRoot.SetActive(false);
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
