// 昼夜阶段枚举，供 DayNightCycle 广播事件时传递，各系统（敌人、黑暗伤害、UI 等）订阅后判断行为
public enum DayPhase
{
    Day,
    Dusk,
    Night
}
