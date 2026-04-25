using TMPro;
using UnityEngine;

// 昼夜阶段 HUD：订阅 GameEvents.OnDayPhaseChanged，切换时间段文本和颜色。
// 危险警告文本由外部逻辑主动调用 ShowWarning/HideWarning 控制。
public class DayNightUI : MonoBehaviour
{
    public static DayNightUI Instance;

    [Header("时间阶段文本")]
    public TMP_Text timePhaseText;

    [Header("危险提示文本")]
    public TMP_Text warningText;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        GameEvents.OnDayPhaseChanged += HandleDayPhaseChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnDayPhaseChanged -= HandleDayPhaseChanged;
    }

    private void Start()
    {
        if (warningText != null)
            warningText.gameObject.SetActive(false);

        // 启动时同步当前阶段，避免等到下一次切换才显示正确文本
        if (DayNightCycle.Instance != null)
            HandleDayPhaseChanged(DayNightCycle.Instance.GetCurrentPhase());
    }

    // 根据昼夜阶段更新文本内容和颜色
    private void HandleDayPhaseChanged(DayPhase phase)
    {
        if (timePhaseText == null) return;
        switch (phase)
        {
            case DayPhase.Day:
                timePhaseText.text  = "白天";
                timePhaseText.color = Color.white;
                break;
            case DayPhase.Dusk:
                timePhaseText.text  = "黄昏";
                timePhaseText.color = new Color(1f, 0.75f, 0.3f);
                break;
            case DayPhase.Night:
                timePhaseText.text  = "夜晚";
                timePhaseText.color = new Color(0.6f, 0.8f, 1f);
                break;
        }
    }

    // 显示危险警告文本（如"夜晚危险，速回营火"）
    public void ShowWarning(string message)
    {
        if (warningText == null) return;
        warningText.gameObject.SetActive(true);
        warningText.text = message;
    }

    // 隐藏危险警告文本
    public void HideWarning()
    {
        if (warningText == null) return;
        warningText.gameObject.SetActive(false);
    }
}
