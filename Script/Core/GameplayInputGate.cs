using UnityEngine;

// 统一判断"是否应屏蔽世界玩法输入（移动、朝向等）"，与背包/暂停/合成台等 UI 状态对齐。
// 各系统调用本类，而不是各自单独判断 PauseMenu / InventoryUI，便于统一维护屏蔽条件。
public static class GameplayInputGate
{
    // 任意阻断 UI 打开或时间已停止时，返回 true
    private static bool AnyBlockingUiOrPausedTime()
    {
        if (PauseMenu.IsPaused)
            return true;
        if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen)
            return true;
        if (WorkbenchUI.Instance != null && WorkbenchUI.Instance.IsOpen)
            return true;
        // 兜底：timeScale 为 0 时也视为不可操作世界（标志与 UI 不同步的边界情况）
        if (Time.timeScale <= 0f)
            return true;
        return false;
    }

    // 是否屏蔽移动和朝向（建造放置模式下允许移动，由 BuildingState 单独处理）
    public static bool ShouldBlockLocomotionAndFacing()
    {
        return AnyBlockingUiOrPausedTime();
    }

    // 是否屏蔽近战攻击（含建造预览期间）
    public static bool ShouldBlockCombatInput()
    {
        if (AnyBlockingUiOrPausedTime())
            return true;
        if (BuildingSystem.Instance != null && BuildingSystem.Instance.IsPlacing)
            return true;
        return false;
    }
}
