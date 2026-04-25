using System.Text;
using TMPro;
using UnityEngine;

// 玩家生存属性 HUD（纯 View 层）：只负责显示，不持有任何游戏数据。
// 用 StringBuilder 复用缓冲区，避免每次刷新拼接字符串产生临时 GC 分配。
public class PlayerStatsUI : MonoBehaviour
{
    public static PlayerStatsUI Instance;

    public TMP_Text hungerText;
    public TMP_Text healthText;

    [Header("可选：拖入 PlayerStats，避免 OnEnable 时 FindObjectOfType")]
    [SerializeField] private PlayerStats playerStatsRef;

    private PlayerStats _playerStats;

    // 复用缓冲区，容量按"饥饿：100/100"这类文本预留
    private readonly StringBuilder _sb = new StringBuilder(48);

    private void Awake()
    {
        Instance = this;
    }

    // 启用时订阅事件，确保与 PlayerStats 生命周期解耦
    private void OnEnable()
    {
        _playerStats = playerStatsRef != null ? playerStatsRef : FindObjectOfType<PlayerStats>();
        if (_playerStats != null)
        {
            _playerStats.OnHungerChanged += UpdateHungerText;
            _playerStats.OnHealthChanged += UpdateHealthText;
        }
    }

    // 禁用时取消订阅，防止对象销毁后仍收到回调导致空引用
    private void OnDisable()
    {
        if (_playerStats != null)
        {
            _playerStats.OnHungerChanged -= UpdateHungerText;
            _playerStats.OnHealthChanged -= UpdateHealthText;
        }
    }

    // 首次显示时同步初始值，避免启动那一帧数值为空
    private void Start()
    {
        if (_playerStats != null)
        {
            UpdateHungerText(_playerStats.currentHunger, _playerStats.maxHunger);
            UpdateHealthText(_playerStats.currentHealth, _playerStats.maxHealth);
        }
    }

    // 饥饿变化回调：拼接显示文本并写入 TMP 组件
    private void UpdateHungerText(int currentHunger, int maxHunger)
    {
        if (hungerText == null) return;
        _sb.Clear();
        _sb.Append("饥饿：");
        _sb.Append(currentHunger);
        _sb.Append('/');
        _sb.Append(maxHunger);
        hungerText.text = _sb.ToString();
    }

    // 生命变化回调：拼接显示文本并写入 TMP 组件
    private void UpdateHealthText(int currentHealth, int maxHealth)
    {
        if (healthText == null) return;
        _sb.Clear();
        _sb.Append("生命：");
        _sb.Append(currentHealth);
        _sb.Append('/');
        _sb.Append(maxHealth);
        healthText.text = _sb.ToString();
    }
}
