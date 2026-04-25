using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 主菜单 UI 控制器：处理新游戏、继续游戏、退出三个入口。
// 继续按钮在无存档时置灰，通过 SceneManager.LoadScene 完成场景跳转。
public class MainMenuUI : MonoBehaviour
{
    [Header("场景名")]
    [SerializeField] private string gameSceneName = "SampleScene";

    [Header("按钮")]
    [SerializeField] private Button continueButton;

    private void Start()
    {
        // 根据磁盘存档是否存在决定继续按钮是否可交互
        RefreshContinueButton();
    }

    // 新游戏：清除"读档"标记，直接进入游戏场景
    public void OnClickNewGame()
    {
        SaveManager.ShouldLoadOnSceneStart = false;
        PauseMenu.ResetGamePauseState();
        StopMainMenuMusicBeforeEnteringGame();
        SceneManager.LoadScene(gameSceneName);
    }

    // 继续游戏：标记"读档"后进入游戏场景，SaveManager 在 Awake 里会执行加载
    public void OnClickContinue()
    {
        if (!SaveManager.HasSaveFileOnDisk())
            return;

        SaveManager.ShouldLoadOnSceneStart = true;
        PauseMenu.ResetGamePauseState();
        StopMainMenuMusicBeforeEnteringGame();
        SceneManager.LoadScene(gameSceneName);
    }

    // 切换场景前停止主菜单 BGM，防止与游戏内 BGM 叠放
    private static void StopMainMenuMusicBeforeEnteringGame()
    {
        MainMenuBgmController menuBgm = FindObjectOfType<MainMenuBgmController>();
        if (menuBgm != null)
            menuBgm.StopMenuBgmNow();
    }

    // 退出游戏（编辑器内无效，打包后生效）
    public void OnClickQuit()
    {
        Application.Quit();
    }

    // 有存档才启用继续按钮
    private void RefreshContinueButton()
    {
        if (continueButton == null) return;
        continueButton.interactable = SaveManager.HasSaveFileOnDisk();
    }
}
