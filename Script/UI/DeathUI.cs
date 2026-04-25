using TMPro;
using UnityEngine;

// 死亡提示面板：玩家死亡时显示"你已死亡 / 按 R 复活"，复活后隐藏。
public class DeathUI : MonoBehaviour
{
    public static DeathUI Instance;

    public GameObject deathPanel;
    public TMP_Text   deathText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 初始状态隐藏，避免启动一帧闪现
        ShowDeathUI(false);
    }

    // 控制死亡面板的显示/隐藏并更新提示文本
    public void ShowDeathUI(bool show)
    {
        if (deathPanel != null)
            deathPanel.SetActive(show);

        if (show && deathText != null)
            deathText.text = "你已死亡\n按 R 复活";
    }
}
