using System;
using UnityEngine;

// 玩家生命和饥饿值的 Model：定时扣饥饿，饥饿归零后持续扣血，血量归零触发死亡。
// 数值变化时通过 C# event 和 EventBus 双通道广播，UI 只需订阅，不用直接引用本组件。
public class PlayerStats : MonoBehaviour
{
    [Header("饥饿")]
    public int maxHunger = 100;
    public int currentHunger = 50;

    [Header("生命")]
    public int maxHealth = 100;
    public int currentHealth = 100;

    [Header("饥饿下降配置")]
    public float hungerDecreaseInterval = 5f;
    public int hungerDecreaseAmount = 1;

    [Header("饥饿为 0 时掉血配置")]
    public float healthDecreaseIntervalWhenStarving = 3f;
    public int healthDecreaseAmountWhenStarving = 1;

    private float _hungerTimer;
    private float _healthTimer;

    public event Action<int, int> OnHungerChanged;
    public event Action<int, int> OnHealthChanged;
    public event Action OnPlayerDied;

    // 初始化计时器并触发一次 UI 刷新，确保开局显示正确
    private void Start()
    {
        _hungerTimer = hungerDecreaseInterval;
        _healthTimer = healthDecreaseIntervalWhenStarving;

        NotifyHungerChanged();
        NotifyHealthChanged();
    }

    // 暂停时跳过所有计时（timeScale=0 不影响 Update 的调用）
    private void Update()
    {
        if (PauseMenu.IsPaused)
            return;

        HandleHungerDecrease();
        HandleHealthDecreaseWhenStarving();
    }

    // 外部调用：增加饥饿（正值回饱，负值扣饱）
    public void AddHunger(int amount)
    {
        currentHunger = Mathf.Clamp(currentHunger + amount, 0, maxHunger);
        NotifyHungerChanged();
    }

    // 外部调用：增加生命（正值回血，负值扣血），扣到 0 时触发死亡流程
    public void AddHealth(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        NotifyHealthChanged();

        if (currentHealth == 0)
        {
            OnPlayerDied?.Invoke();
            PlayerRespawn playerRespawn = GetComponent<PlayerRespawn>();
            if (playerRespawn != null)
                playerRespawn.HandleDeath();
        }
    }

    // 定时扣饱食度
    private void HandleHungerDecrease()
    {
        _hungerTimer -= Time.deltaTime;

        if (_hungerTimer > 0f)
            return;

        _hungerTimer = hungerDecreaseInterval;
        DecreaseHunger(hungerDecreaseAmount);
    }

    // 扣饱食度并广播
    private void DecreaseHunger(int amount)
    {
        currentHunger = Mathf.Max(0, currentHunger - amount);
        NotifyHungerChanged();
    }

    // 饥饿归零后才开始扣血；有饱食度时重置计时器，确保补充食物后不会立即扣血
    private void HandleHealthDecreaseWhenStarving()
    {
        if (currentHunger > 0)
        {
            _healthTimer = healthDecreaseIntervalWhenStarving;
            return;
        }

        if (currentHealth <= 0)
            return;

        _healthTimer -= Time.deltaTime;

        if (_healthTimer > 0f)
            return;

        _healthTimer = healthDecreaseIntervalWhenStarving;
        DecreaseHealth(healthDecreaseAmountWhenStarving);
    }

    // 扣血并检查是否死亡
    private void DecreaseHealth(int amount)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);
        NotifyHealthChanged();

        if (currentHealth == 0)
        {
            OnPlayerDied?.Invoke();
            PlayerRespawn playerRespawn = GetComponent<PlayerRespawn>();
            if (playerRespawn != null)
                playerRespawn.HandleDeath();
        }
    }

    // 复活后恢复满血，饥饿恢复到中等值（不给满，保留生存压力）
    public void ResetStatsAfterRespawn()
    {
        currentHealth = maxHealth;
        currentHunger = Mathf.Min(50, maxHunger);

        NotifyHealthChanged();
        NotifyHungerChanged();
    }

    // 对外暴露查询接口（供存档等需要明确获取数值的地方调用）
    public int GetCurrentHealth() => currentHealth;
    public int GetCurrentHunger() => currentHunger;

    // 读档时直接写入数值并广播
    public void LoadFromSaveData(int health, int hunger)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        currentHunger = Mathf.Clamp(hunger, 0, maxHunger);

        NotifyHealthChanged();
        NotifyHungerChanged();
    }

    // 广播饥饿变化（C# event + EventBus 双通道）
    private void NotifyHungerChanged()
    {
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
        EventBus.Publish(new HungerChangedPayload(currentHunger, maxHunger));
    }

    // 广播生命变化（C# event + EventBus 双通道）
    private void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        EventBus.Publish(new HealthChangedPayload(currentHealth, maxHealth));
    }
}
