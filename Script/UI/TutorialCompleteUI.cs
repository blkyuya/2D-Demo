using TMPro;
using UnityEngine;

// 教程完成提示面板：显示标题和正文，由外部逻辑主动调用 ShowCompletePanel/ClosePanel 控制。
public class TutorialCompleteUI : MonoBehaviour
{
    public static TutorialCompleteUI Instance;

    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text   titleText;
    [SerializeField] private TMP_Text   bodyText;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Show(false);
    }

    // 显示带标题和正文的完成面板
    public void ShowCompletePanel(string title, string body)
    {
        if (titleText != null) titleText.text = title;
        if (bodyText  != null) bodyText.text  = body;
        Show(true);
    }

    // 关闭面板
    public void ClosePanel()
    {
        Show(false);
    }

    private void Show(bool show)
    {
        if (root != null) root.SetActive(show);
    }
}
