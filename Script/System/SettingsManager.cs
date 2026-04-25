using UnityEngine;

// 设置管理器：用 PlayerPrefs 持久化音效和 BGM 音量，跨场景保留。
// 场景切换后只要 DontDestroyOnLoad 或每次重建，SettingsUI 都从这里读取初始值。
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    private const string SfxVolumeKey = "SFX_VOLUME";
    private const string BgmVolumeKey = "BGM_VOLUME";

    public float SfxVolume { get; private set; } = 1f;
    public float BgmVolume { get; private set; } = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // 启动时立即从 PlayerPrefs 读取，保证其他组件在 Start 里能拿到正确值
        LoadSettings();
    }

    // 设置音效音量并立即持久化到 PlayerPrefs
    public void SetSfxVolume(float volume)
    {
        SfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
        PlayerPrefs.Save();
    }

    // 设置 BGM 音量并立即持久化到 PlayerPrefs
    public void SetBgmVolume(float volume)
    {
        BgmVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(BgmVolumeKey, BgmVolume);
        PlayerPrefs.Save();
    }

    // 从 PlayerPrefs 读取音量（找不到则使用 1.0 默认值）
    public void LoadSettings()
    {
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        BgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
    }
}
