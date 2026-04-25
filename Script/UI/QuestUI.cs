using TMPro;
using UnityEngine;

// 任务追踪 HUD：订阅 QuestManager.OnQuestUpdated，实时刷新三项任务进度文本。
public class QuestUI : MonoBehaviour
{
    [SerializeField] private TMP_Text questText;

    private void Start()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestUpdated += RefreshUI;

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestUpdated -= RefreshUI;
    }

    // 从 QuestManager 读取任务完成状态并刷新文本
    public void RefreshUI()
    {
        if (questText == null) return;
        if (QuestManager.Instance == null) return;

        QuestManager q = QuestManager.Instance;

        string woodMark     = q.WoodQuestCompleted      ? "[x]" : "[ ]";
        string campfireMark = q.CampfireQuestCompleted  ? "[x]" : "[ ]";
        string surviveMark  = q.SurviveNightQuestCompleted ? "[x]" : "[ ]";

        string campfireProgress = q.CampfireQuestCompleted     ? "1/1" : "0/1";
        string surviveProgress  = q.SurviveNightQuestCompleted ? "1/1" : "0/1";

        questText.text =
            "任务\n" +
            $"{woodMark} 拾取木材 {q.CurrentWoodCount}/{q.GetTargetWoodCount()}\n" +
            $"{campfireMark} 建造营火 {campfireProgress}\n" +
            $"{surviveMark} 入夜时在营火旁存活 {surviveProgress}";
    }
}
