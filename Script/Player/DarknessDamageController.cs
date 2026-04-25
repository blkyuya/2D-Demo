using UnityEngine;

// 夜晚未靠近营火时持续扣血。
// 安全范围检测走 Campfire.RegisteredInstances 列表，O(营火数量) 而非全场景遍历，
// 避免每帧 FindObjectsOfType 造成 CPU 和 GC 尖峰。
public class DarknessDamageController : MonoBehaviour
{
    [Header("夜晚掉血间隔（秒）")]
    public float darknessDamageInterval = 2f;

    [Header("每次掉血数值")]
    public int darknessDamageAmount = 1;

    private PlayerStats _playerStats;
    private float _darknessTimer;

    private void Start()
    {
        _playerStats = GetComponent<PlayerStats>();
        _darknessTimer = darknessDamageInterval;
    }

    // 每帧判断：白天或在营火旁时重置计时器；夜晚且不安全时定时扣血
    private void Update()
    {
        if (PauseMenu.IsPaused)
            return;

        if (_playerStats == null || DayNightCycle.Instance == null)
            return;

        bool isNight = DayNightCycle.Instance.IsNight();
        bool isSafe  = IsInAnyCampfireSafeRange();

        if (!isNight || isSafe)
        {
            _darknessTimer = darknessDamageInterval;

            if (DayNightUI.Instance != null)
                DayNightUI.Instance.HideWarning();

            return;
        }

        if (DayNightUI.Instance != null)
            DayNightUI.Instance.ShowWarning("危险：请靠近营火！");

        _darknessTimer -= Time.deltaTime;

        if (_darknessTimer > 0f)
            return;

        _darknessTimer = darknessDamageInterval;
        _playerStats.AddHealth(-darknessDamageAmount);
    }

    // 遍历注册表中已点燃的营火，检查玩家是否在任意一个的安全范围内
    private bool IsInAnyCampfireSafeRange()
    {
        for (int i = 0; i < Campfire.RegisteredInstances.Count; i++)
        {
            Campfire campfire = Campfire.RegisteredInstances[i];
            if (campfire == null || !campfire.IsLit())
                continue;

            if (campfire.IsInSafeRange(transform.position))
                return true;
        }

        return false;
    }
}
