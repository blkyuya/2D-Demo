using System.Collections;
using TMPro;
using UnityEngine;

// 营火操作提示 UI：短暂显示一条文本后自动隐藏，用于"加燃料成功"等即时反馈。
public class CampfireMessageUI : MonoBehaviour
{
    public static CampfireMessageUI Instance;

    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float showDuration = 1.5f;

    private Coroutine _messageCoroutine;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 初始状态隐藏文本节点
        if (messageText != null)
            messageText.gameObject.SetActive(false);
    }

    // 显示消息，如果上一条还未消失则直接替换（重置计时器）
    public void ShowMessage(string message)
    {
        if (messageText == null) return;
        if (_messageCoroutine != null) StopCoroutine(_messageCoroutine);
        _messageCoroutine = StartCoroutine(ShowMessageCoroutine(message));
    }

    // 展示文本，等待 showDuration 后隐藏
    private IEnumerator ShowMessageCoroutine(string message)
    {
        messageText.gameObject.SetActive(true);
        messageText.text = message;

        yield return new WaitForSecondsRealtime(showDuration);

        messageText.gameObject.SetActive(false);
        _messageCoroutine = null;
    }
}
