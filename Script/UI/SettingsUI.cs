using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 设置界面：音效和 BGM 音量滑条，实时同步到 SettingsManager（PlayerPrefs 持久化）。
public class SettingsUI : MonoBehaviour
{
    [Header("音量滑条")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider bgmSlider;

    [Header("音量数值文本")]
    [SerializeField] private TMP_Text sfxValueText;
    [SerializeField] private TMP_Text bgmValueText;

    private void Start()
    {
        if (SettingsManager.Instance == null) return;

        // 从 SettingsManager 读取当前音量，初始化滑条并注册回调
        if (sfxSlider != null)
        {
            sfxSlider.value = SettingsManager.Instance.SfxVolume;
            sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
            UpdateSfxText(sfxSlider.value);
        }

        if (bgmSlider != null)
        {
            bgmSlider.value = SettingsManager.Instance.BgmVolume;
            bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);
            UpdateBgmText(bgmSlider.value);
        }
    }

    private void OnDestroy()
    {
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);
        if (bgmSlider != null) bgmSlider.onValueChanged.RemoveListener(OnBgmSliderChanged);
    }

    // 音效滑条变化：同步到 SettingsManager 并刷新数值文本
    private void OnSfxSliderChanged(float value)
    {
        SettingsManager.Instance?.SetSfxVolume(value);
        UpdateSfxText(value);
    }

    // BGM 滑条变化：同步到 SettingsManager 并刷新数值文本
    private void OnBgmSliderChanged(float value)
    {
        SettingsManager.Instance?.SetBgmVolume(value);
        UpdateBgmText(value);
    }

    // 将 0~1 的浮点值转换为百分比文本显示
    private void UpdateSfxText(float value)
    {
        if (sfxValueText != null)
            sfxValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private void UpdateBgmText(float value)
    {
        if (bgmValueText != null)
            bgmValueText.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}
