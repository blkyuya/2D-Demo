using System.Collections;
using UnityEngine;

// 作品集演示引导：场景启动后延迟展示操作提示（多条）。
// 不改动任务逻辑本身，只通过 NotificationUI 推送说明文字。
// 挂载：与 QuestManager 同物体或任意常驻 Manager。
public class VerticalSliceBootstrap : MonoBehaviour
{
    [Header("开局提示（作品集演示）")]
    [SerializeField] private bool showOpeningHints = true;

    [Tooltip("首条提示延迟（秒），等 UI/任务初始化完成后再弹")]
    [SerializeField] private float delayFirstHintSeconds = 1.2f;

    [Tooltip("多条提示之间的间隔（秒）")]
    [SerializeField] private float hintIntervalSeconds = 6f;

    [TextArea(2, 5)]
    [SerializeField] private string hintLine1 =
        "请跟随左上角任务：① 拾取木材×3（砍伐树木）② 建造营火 ③ 入夜时在营火旁存活。";

    [TextArea(2, 5)]
    [SerializeField] private string hintLine2 = "操作：WASD 移动；鼠标左键攻击/采集；Tab 背包；ESC 暂停。";

    private void Start()
    {
        if (!showOpeningHints) return;
        StartCoroutine(PlayOpeningHintsRoutine());
    }

    // 依次推送提示文本，间隔一段时间后推下一条
    private IEnumerator PlayOpeningHintsRoutine()
    {
        yield return new WaitForSeconds(delayFirstHintSeconds);

        if (NotificationUI.Instance != null && !string.IsNullOrWhiteSpace(hintLine1))
            NotificationUI.Instance.ShowMessage(hintLine1);

        yield return new WaitForSeconds(hintIntervalSeconds);

        if (NotificationUI.Instance != null && !string.IsNullOrWhiteSpace(hintLine2))
            NotificationUI.Instance.ShowMessage(hintLine2);
    }
}
