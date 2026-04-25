using System.Collections.Generic;
using UnityEngine;

// 营火交互与范围检测。
// OnEnable 时向全局列表注册，夜晚伤害 / 任务等系统遍历本列表判断安全范围，
// 避免每帧 FindObjectsOfType 全场景扫描（Play 后持续卡顿的主要原因之一）。
public class Campfire : MonoBehaviour, IInteractable
{
    // 当前场景中所有已激活的营火（OnEnable 加入，OnDisable 移除）
    public static readonly List<Campfire> RegisteredInstances = new List<Campfire>(16);

    public int healAmount = 20;

    [Header("营火安全范围")]
    public float safeRadius = 3f;

    [Header("营火燃料配置")]
    public float maxFuel = 100f;
    public float currentFuel = 100f;
    public float fuelConsumePerSecond = 5f;
    public float addFuelAmount = 30f;

    private SpriteRenderer _spriteRenderer;
    private Color _originalColor;
    private bool _wasLitLastFrame = true;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_spriteRenderer != null)
            _originalColor = _spriteRenderer.color;
    }

    // 注册到全局列表
    private void OnEnable()
    {
        if (!RegisteredInstances.Contains(this))
            RegisteredInstances.Add(this);
    }

    // 从全局列表移除，防止其他系统拿到已销毁的引用
    private void OnDisable()
    {
        RegisteredInstances.Remove(this);
    }

    // 消耗燃料，熄灭时通知玩家，并更新外观颜色
    private void Update()
    {
        if (PauseMenu.IsPaused)
            return;

        if (currentFuel > 0f)
        {
            currentFuel -= fuelConsumePerSecond * Time.deltaTime;
            if (currentFuel < 0f)
                currentFuel = 0f;
        }

        UpdateVisualState();

        bool isLitNow = IsLit();

        // 刚从燃烧变为熄灭的那一帧才弹提示
        if (_wasLitLastFrame && !isLitNow)
        {
            if (NotificationUI.Instance != null)
                NotificationUI.Instance.ShowMessage("营火已熄灭");
        }

        _wasLitLastFrame = isLitNow;
    }

    // 与营火交互：有木头且燃料未满时先添燃料，否则回血
    public void Interact()
    {
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();
        PlayerStats playerStats = FindObjectOfType<PlayerStats>();

        if (playerInventory != null && currentFuel < maxFuel)
        {
            bool usedWood = playerInventory.UseItem(ItemType.Wood, 1);

            if (usedWood)
            {
                currentFuel = Mathf.Min(currentFuel + addFuelAmount, maxFuel);

                if (NotificationUI.Instance != null)
                    NotificationUI.Instance.ShowMessage("营火已补充燃料");

                return;
            }
        }

        if (playerStats == null)
            return;

        playerStats.AddHealth(healAmount);
    }

    // 根据当前燃料状态返回不同的交互提示文本
    public string GetInteractionText()
    {
        int fuelValue = Mathf.CeilToInt(currentFuel);

        if (!IsLit())
            return $"按 E 添加木头点燃营火\nFuel: {fuelValue}/{maxFuel}";

        if (currentFuel < maxFuel)
            return $"按 E 添加燃料 / 使用营火\nFuel: {fuelValue}/{maxFuel}";

        return $"按 E 使用营火\nFuel: {fuelValue}/{maxFuel}";
    }

    // 判断目标点是否在本营火的安全范围内（同时要求营火点燃中）
    public bool IsInSafeRange(Vector3 targetPosition)
    {
        if (!IsLit())
            return false;

        return Vector3.Distance(transform.position, targetPosition) <= safeRadius;
    }

    // 营火是否点燃（有燃料就算点燃）
    public bool IsLit()
    {
        return currentFuel > 0f;
    }

    // 读档时直接设置燃料（同时刷新视觉状态）
    public void SetFuel(float fuel)
    {
        currentFuel = Mathf.Clamp(fuel, 0f, maxFuel);
        UpdateVisualState();
        _wasLitLastFrame = IsLit();
    }

    // 燃烧时还原原色，熄灭时变灰
    private void UpdateVisualState()
    {
        if (_spriteRenderer == null)
            return;

        _spriteRenderer.color = IsLit() ? _originalColor : Color.gray;
    }

    // 编辑器中可视化安全范围
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsLit() ? Color.yellow : Color.gray;
        Gizmos.DrawWireSphere(transform.position, safeRadius);
    }
}
