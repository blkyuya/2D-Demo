using UnityEngine;
using UnityEngine.SceneManagement;

// ESC 在 LateUpdate 中处理：Unity 先执行全部 Update，再执行 LateUpdate，
// 确保同一帧内 InventoryUI/WorkbenchUI 先在各自的 Update 里关掉面板，
// 再轮到本脚本响应 ESC，不依赖脚本间未定义的 executionOrder。
public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused = false;

    [Header("面板")]
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("返回主菜单")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool _isPaused = false;

    // 跨场景跳转前调用：清除静态暂停态，防止再次进入游戏时仍视为暂停
    public static void ResetGamePauseState()
    {
        IsPaused       = false;
        Time.timeScale = 1f;
    }

    // 强制关闭所有暂停/设置面板并同步内部标志（用于场景加载完成后的修正）
    public void ForceApplyClosedState()
    {
        _isPaused      = false;
        IsPaused       = false;
        Time.timeScale = 1f;

        if (pauseUI      != null) pauseUI.SetActive(false);
        if (pausePanel   != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    private void Awake()  => ForceApplyClosedState();
    private void OnEnable() => ForceApplyClosedState();
    private void Start()  => ForceApplyClosedState();

    // 场景卸载时释放全局暂停，避免主菜单场景继承 timeScale=0
    private void OnDestroy()
    {
        IsPaused       = false;
        Time.timeScale = 1f;
    }

    private void LateUpdate()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        // 背包/箱子在 Update 中关闭，LateUpdate 时 IsOpen 已为 false，此处不重复处理
        if (InventoryUI.Instance  != null && InventoryUI.Instance.IsOpen)  return;
        if (WorkbenchUI.Instance  != null && WorkbenchUI.Instance.IsOpen)  return;

        // timeScale 已停但 _isPaused 未置位时（如箱子未恢复时间），先恢复再走暂停逻辑
        if (Time.timeScale <= 0f && !_isPaused)
            Time.timeScale = 1f;

        if (!_isPaused)
        {
            PauseGame();
        }
        else
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
                CloseSettings();
            else
                ResumeGame();
        }
    }

    // 暂停游戏：显示暂停面板，停止时间
    public void PauseGame()
    {
        _isPaused = true;
        IsPaused  = true;
        if (pauseUI      != null) pauseUI.SetActive(true);
        if (pausePanel   != null) pausePanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        Time.timeScale = 0f;
    }

    // 恢复游戏：隐藏所有暂停面板，恢复时间
    public void ResumeGame()
    {
        _isPaused = false;
        IsPaused  = false;
        if (pausePanel   != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pauseUI      != null) pauseUI.SetActive(false);
        Time.timeScale = 1f;
    }

    // 暂停时打开设置面板（隐藏主暂停按钮列表）
    public void OpenSettings()
    {
        if (pausePanel   != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    // 关闭设置面板，返回主暂停按钮列表
    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel   != null) pausePanel.SetActive(true);
    }

    // 返回主菜单：先停 BGM 再切场景，防止关卡音乐带入主菜单
    public void QuitGame()
    {
        ResetGamePauseState();
        _isPaused = false;
        AudioManager.StopGameBgmImmediately();
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
