using UnityEngine;
using UnityEngine.UI;

// 世界光照控制器：订阅昼夜阶段变化事件，通过 UI Image（全屏半透明叠加层）平滑过渡到目标 alpha，
// 模拟白天→黄昏→夜晚的亮度变化。
public class WorldLightController : MonoBehaviour
{
    [Header("夜幕遮罩（全屏 UI Image，颜色设为黑色/深蓝）")]
    public Image nightOverlay;

    [Range(0f, 1f)] public float dayAlpha   = 0f;
    [Range(0f, 1f)] public float duskAlpha  = 0.35f;
    [Range(0f, 1f)] public float nightAlpha = 0.65f;

    private float _targetAlpha;

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
        // 启动时同步初始阶段，避免等到第一次切换才生效
        if (DayNightCycle.Instance != null)
            HandleDayPhaseChanged(DayNightCycle.Instance.GetCurrentPhase());
    }

    // 每帧平滑插值到目标 alpha（速度系数 2，视觉上约 0.5 秒过渡完成）
    private void Update()
    {
        if (nightOverlay == null) return;
        Color c = nightOverlay.color;
        c.a = Mathf.Lerp(c.a, _targetAlpha, Time.deltaTime * 2f);
        nightOverlay.color = c;
    }

    // 收到阶段变化事件，更新目标 alpha 值
    private void HandleDayPhaseChanged(DayPhase phase)
    {
        switch (phase)
        {
            case DayPhase.Day:   _targetAlpha = dayAlpha;   break;
            case DayPhase.Dusk:  _targetAlpha = duskAlpha;  break;
            case DayPhase.Night: _targetAlpha = nightAlpha; break;
        }
    }
}
