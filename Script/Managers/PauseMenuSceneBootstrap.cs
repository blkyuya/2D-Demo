using UnityEngine;
using UnityEngine.SceneManagement;

// 场景加载完成后的修正引导：确保 PauseMenu 和各 UI 面板都处于关闭状态。
// 用 RuntimeInitializeOnLoadMethod 避免依赖某个 MonoBehaviour 的 Start 执行顺序。
public static class PauseMenuSceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnAfterSceneLoad()
    {
        Scene s = SceneManager.GetActiveScene();
        if (s.name != "SampleScene") return;

        // 强制清除可能因脚本执行顺序导致的暂停态残留
        PauseMenu.ResetGamePauseState();

        PauseMenu menu = Object.FindObjectOfType<PauseMenu>();
        if (menu != null) menu.ForceApplyClosedState();

        // 再次进入关卡时强制同步背包/工作台关闭态，防止 IsOpen 与面板激活状态不一致
        InventoryUI inv = Object.FindObjectOfType<InventoryUI>(true);
        if (inv != null) inv.CloseAll();

        WorkbenchUI bench = Object.FindObjectOfType<WorkbenchUI>(true);
        if (bench != null) bench.ClosePanel();
    }
}
