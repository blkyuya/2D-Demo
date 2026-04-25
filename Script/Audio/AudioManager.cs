using System.Collections;
using UnityEngine;

// 音频管理器：统一管理 SFX 单次播放和 BGM 跨昼夜淡入淡出切换。
// 通过订阅 GameEvents（物品拾取/玩家死亡/建造放置/营火熄灭/昼夜切换）自动触发对应音效。
// BGM 切换使用淡出→切片段→淡入的协程，避免生硬跳变。
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("播放器")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;

    [Header("音效资源")]
    [SerializeField] private AudioClip itemPickupClip;
    [SerializeField] private AudioClip playerDeathClip;
    [SerializeField] private AudioClip buildingPlacedClip;
    [SerializeField] private AudioClip campfireExtinguishedClip;
    [SerializeField] private AudioClip dayPhaseChangedClip;

    [Header("背景音乐")]
    [SerializeField] private AudioClip dayBgmClip;
    [SerializeField] private AudioClip nightBgmClip;

    [Header("BGM 淡入淡出时长（秒）")]
    [SerializeField] private float bgmFadeDuration = 1.5f;

    [Header("音量基准（0~1，实际音量还乘以 SettingsManager）")]
    [Range(0f, 1f)] [SerializeField] private float itemPickupVolume           = 1f;
    [Range(0f, 1f)] [SerializeField] private float playerDeathVolume          = 1f;
    [Range(0f, 1f)] [SerializeField] private float buildingPlacedVolume       = 1f;
    [Range(0f, 1f)] [SerializeField] private float campfireExtinguishedVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float dayPhaseChangedVolume      = 1f;
    [Range(0f, 1f)] [SerializeField] private float bgmBaseVolume              = 1f;

    private Coroutine _bgmCoroutine;
    private AudioClip _currentBgmClip;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        SetupAudioSources();
    }

    private void Start()
    {
        // 延迟一帧确保 DayNightCycle.Awake 已完成，再从当前阶段开始播放 BGM
        StartCoroutine(RestartBgmAfterSceneReady());
    }

    private IEnumerator RestartBgmAfterSceneReady()
    {
        yield return null;
        RestartBgmForSceneEnter();
    }

    // 进入 SampleScene 时调用：停止上一段进度，根据当前昼夜阶段从头播 BGM
    public void RestartBgmForSceneEnter()
    {
        if (_bgmCoroutine != null) { StopCoroutine(_bgmCoroutine); _bgmCoroutine = null; }
        _currentBgmClip = null;

        if (bgmSource != null) { bgmSource.Stop(); bgmSource.time = 0f; }

        if (DayNightCycle.Instance != null)
            UpdateBgmByPhase(DayNightCycle.Instance.GetCurrentPhase(), true, true);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (bgmSource != null) bgmSource.Stop();
    }

    // 静态方法：退回主菜单/切场景前立刻停掉关卡 BGM，防止音乐泄漏到主菜单
    public static void StopGameBgmImmediately()
    {
        if (Instance != null) { Instance.StopBgmPlaybackCompletely(); return; }
        AudioManager found = FindObjectOfType<AudioManager>();
        if (found != null) found.StopBgmPlaybackCompletely();
    }

    // 完全停止 BGM（包含中断正在进行的淡入淡出协程）
    private void StopBgmPlaybackCompletely()
    {
        if (_bgmCoroutine != null) { StopCoroutine(_bgmCoroutine); _bgmCoroutine = null; }
        _currentBgmClip = null;
        if (bgmSource != null) { bgmSource.Stop(); bgmSource.time = 0f; bgmSource.clip = null; }
    }

    // 每帧同步 BGM 音量（在非淡入淡出阶段实时跟随 SettingsManager 设置）
    private void Update()
    {
        UpdateBgmVolume();
    }

    private void OnEnable()
    {
        GameEvents.OnItemPickedUp        += HandleItemPickedUp;
        GameEvents.OnPlayerDied          += HandlePlayerDied;
        GameEvents.OnBuildingPlaced      += HandleBuildingPlaced;
        GameEvents.OnCampfireExtinguished += HandleCampfireExtinguished;
        GameEvents.OnDayPhaseChanged     += HandleDayPhaseChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnItemPickedUp        -= HandleItemPickedUp;
        GameEvents.OnPlayerDied          -= HandlePlayerDied;
        GameEvents.OnBuildingPlaced      -= HandleBuildingPlaced;
        GameEvents.OnCampfireExtinguished -= HandleCampfireExtinguished;
        GameEvents.OnDayPhaseChanged     -= HandleDayPhaseChanged;
    }

    // 初始化 AudioSource：SFX 用本物体上的（或动态添加），BGM 用子物体
    private void SetupAudioSources()
    {
        if (sfxSource == null) sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop        = false;

        if (bgmSource == null)
        {
            GameObject bgmObj = new GameObject("BGMSource");
            bgmObj.transform.SetParent(transform);
            bgmSource = bgmObj.AddComponent<AudioSource>();
        }
        bgmSource.playOnAwake = false;
        bgmSource.loop        = true;
    }

    private void HandleItemPickedUp(ItemType itemType, int amount) => PlaySFX(itemPickupClip, itemPickupVolume);
    private void HandlePlayerDied()                                 => PlaySFX(playerDeathClip, playerDeathVolume);
    private void HandleBuildingPlaced(BuildingRecipeSO recipe)      => PlaySFX(buildingPlacedClip, buildingPlacedVolume);
    private void HandleCampfireExtinguished()                       => PlaySFX(campfireExtinguishedClip, campfireExtinguishedVolume);

    // 昼夜切换：播放阶段切换音效并切换 BGM（带淡入淡出）
    private void HandleDayPhaseChanged(DayPhase phase)
    {
        PlaySFX(dayPhaseChangedClip, dayPhaseChangedVolume);
        UpdateBgmByPhase(phase, false);
    }

    // 播放一次性 SFX，音量乘以 SettingsManager 的 SFX 系数
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (sfxSource == null || clip == null) return;
        float finalVolume = volume;
        if (SettingsManager.Instance != null) finalVolume *= SettingsManager.Instance.SfxVolume;
        sfxSource.PlayOneShot(clip, finalVolume);
    }

    // 根据昼夜阶段选择 BGM 片段，immediate=true 时直接切换，否则走淡入淡出协程
    private void UpdateBgmByPhase(DayPhase phase, bool immediate, bool forceRestartEvenIfSameClip = false)
    {
        AudioClip targetClip = phase == DayPhase.Day ? dayBgmClip : nightBgmClip;

        if (!forceRestartEvenIfSameClip && targetClip == _currentBgmClip) return;

        _currentBgmClip = targetClip;
        if (_bgmCoroutine != null) StopCoroutine(_bgmCoroutine);

        if (immediate)
            PlayBgmImmediate(targetClip);
        else
            _bgmCoroutine = StartCoroutine(FadeSwitchBgm(targetClip));
    }

    // 立即切换 BGM（进场景时不需要过渡效果）
    private void PlayBgmImmediate(AudioClip clip)
    {
        if (bgmSource == null) return;
        bgmSource.clip = clip;
        if (clip == null) { bgmSource.Stop(); return; }
        bgmSource.volume = GetCurrentBgmVolume();
        bgmSource.Play();
    }

    // 淡出当前 BGM → 换片段 → 淡入新 BGM
    private IEnumerator FadeSwitchBgm(AudioClip newClip)
    {
        if (bgmSource == null) yield break;

        float startVolume = bgmSource.volume;
        float timer = 0f;

        while (timer < bgmFadeDuration)
        {
            timer += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, timer / bgmFadeDuration);
            yield return null;
        }

        bgmSource.volume = 0f;
        bgmSource.Stop();
        bgmSource.clip = newClip;
        if (newClip != null) bgmSource.Play();

        float targetVolume = GetCurrentBgmVolume();
        timer = 0f;

        while (timer < bgmFadeDuration)
        {
            timer += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(0f, targetVolume, timer / bgmFadeDuration);
            yield return null;
        }

        bgmSource.volume = targetVolume;
        _bgmCoroutine = null;
    }

    // 非淡入淡出阶段实时同步 BGM 音量（响应用户实时调整设置）
    private void UpdateBgmVolume()
    {
        if (bgmSource == null) return;
        if (_bgmCoroutine == null)
            bgmSource.volume = GetCurrentBgmVolume();
    }

    // BGM 最终音量 = 基准音量 × SettingsManager.BgmVolume
    private float GetCurrentBgmVolume()
    {
        float volume = bgmBaseVolume;
        if (SettingsManager.Instance != null) volume *= SettingsManager.Instance.BgmVolume;
        return volume;
    }
}
