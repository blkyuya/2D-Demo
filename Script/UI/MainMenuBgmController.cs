using UnityEngine;

// 主菜单循环 BGM：进入菜单场景时自动从头播放，音量实时跟随 SettingsManager。
// 进入游戏关卡前调用 StopMenuBgmNow()，防止与游戏内 BGM 叠放。
[RequireComponent(typeof(AudioSource))]
public class MainMenuBgmController : MonoBehaviour
{
    [Header("主菜单音乐")]
    [SerializeField] private AudioClip menuBgmClip;

    private AudioSource _audio;

    private void Awake()
    {
        _audio             = GetComponent<AudioSource>();
        _audio.loop        = true;
        _audio.playOnAwake = false;
        if (menuBgmClip != null)
            _audio.clip = menuBgmClip;
    }

    private void Start()
    {
        // 每次进入主菜单场景都从头播放 BGM
        RestartFromBeginning();
    }

    // 从头播放菜单 BGM（场景重新加载或返回主菜单时调用）
    public void RestartFromBeginning()
    {
        if (_audio == null)
        {
            _audio             = GetComponent<AudioSource>();
            _audio.loop        = true;
            _audio.playOnAwake = false;
            if (menuBgmClip != null)
                _audio.clip = menuBgmClip;
        }

        _audio.Stop();
        _audio.time = 0f;
        if (menuBgmClip != null)
            _audio.clip = menuBgmClip;

        ApplyVolume();
        if (_audio.clip != null)
            _audio.Play();
    }

    // 每帧同步音量，确保在菜单内调整设置时立即生效
    private void Update()
    {
        ApplyVolume();
    }

    // 从 SettingsManager 读取 BGM 音量并应用到 AudioSource
    private void ApplyVolume()
    {
        float v = SettingsManager.Instance != null ? SettingsManager.Instance.BgmVolume : 1f;
        _audio.volume = v;
    }

    // 进入关卡前停止主菜单 BGM
    public void StopMenuBgmNow()
    {
        if (_audio == null)
        {
            _audio = GetComponent<AudioSource>();
            if (_audio == null) return;
        }

        _audio.Stop();
        _audio.time = 0f;
    }
}
