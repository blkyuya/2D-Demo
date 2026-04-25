using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 全局通知弹出框：消息通过队列顺序展示，避免同时出现多条相互覆盖。
// WaitForSecondsRealtime 保证暂停时（timeScale=0）通知仍然正常消失。
public class NotificationUI : MonoBehaviour
{
    public static NotificationUI Instance;

    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float showDuration = 2f;

    private readonly Queue<string> _messageQueue = new Queue<string>();
    private Coroutine _queueCoroutine;
    private bool _isShowing;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (root != null) root.SetActive(false);
    }

    // 加入队列：如果当前没有通知在显示则立即启动协程
    public void ShowMessage(string message)
    {
        if (!gameObject.activeInHierarchy) return;
        if (string.IsNullOrWhiteSpace(message)) return;

        _messageQueue.Enqueue(message);
        if (!_isShowing)
            _queueCoroutine = StartCoroutine(ProcessQueue());
    }

    // 依次展示队列中所有消息，每条之间有短暂间隔防止闪烁
    private IEnumerator ProcessQueue()
    {
        _isShowing = true;
        while (_messageQueue.Count > 0)
        {
            string message = _messageQueue.Dequeue();
            if (root != null) root.SetActive(true);
            if (messageText != null) messageText.text = message;

            yield return new WaitForSecondsRealtime(showDuration);

            if (root != null) root.SetActive(false);
            yield return new WaitForSecondsRealtime(0.1f);
        }
        _isShowing = false;
        _queueCoroutine = null;
    }
}
