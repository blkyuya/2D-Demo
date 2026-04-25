using System.Collections;
using TMPro;
using UnityEngine;

// 任务完成提示弹窗：显示指定时长后自动隐藏，多次触发时中断上次协程重新计时。
public class QuestCompleteUI : MonoBehaviour
{
    public static QuestCompleteUI Instance;

    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text   completeText;
    [SerializeField] private float showDuration = 2f;

    private Coroutine _showCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (root != null) root.SetActive(false);
    }

    // 显示"任务完成：xxx"样式的提示
    public void ShowComplete(string questName)
    {
        ShowMessage("任务完成：" + questName);
    }

    // 显示任意文本的临时提示（任务完成/教程提示等复用）
    public void ShowMessage(string message)
    {
        if (!gameObject.activeInHierarchy) return;
        if (_showCoroutine != null) StopCoroutine(_showCoroutine);
        _showCoroutine = StartCoroutine(ShowRoutine(message));
    }

    // 显示内容，等待 showDuration 后自动隐藏
    private IEnumerator ShowRoutine(string message)
    {
        if (root != null)         root.SetActive(true);
        if (completeText != null) completeText.text = message;

        yield return new WaitForSecondsRealtime(showDuration);

        if (root != null) root.SetActive(false);
        _showCoroutine = null;
    }
}
