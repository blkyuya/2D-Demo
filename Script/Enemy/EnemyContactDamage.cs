using UnityEngine;

// 接触伤害：依赖 Is Trigger 碰撞体，持续触发时按间隔扣玩家血。
// 同物体上的「非 Trigger」碰撞体负责物理阻挡，两者分工不冲突。
public class EnemyContactDamage : MonoBehaviour
{
    [Header("接触伤害")]
    [SerializeField] private int damage         = 5;
    [SerializeField] private float damageInterval = 1f;
    [SerializeField] private EnemyAnimationController animationController;

    private float _damageTimer = 0f;
    private DayNightCycle _dayNightCycle;

    private void Awake()
    {
        if (animationController == null)
        {
            animationController = GetComponent<EnemyAnimationController>();
            if (animationController == null)
                animationController = GetComponentInChildren<EnemyAnimationController>(true);
        }

        _dayNightCycle = FindObjectOfType<DayNightCycle>();
    }

    // 冷却倒计时
    private void Update()
    {
        if (_damageTimer > 0f)
            _damageTimer -= Time.deltaTime;
    }

    // 持续在触发区域内时按间隔伤害：先播攻击动画，再扣血
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!IsEnemyActive())
            return;

        if (_damageTimer > 0f)
            return;

        // 先在当前物体找，再向上找父级（玩家根物体可能不是碰撞体直接所在层级）
        PlayerStats playerStats = other.GetComponent<PlayerStats>();
        if (playerStats == null)
            playerStats = other.GetComponentInParent<PlayerStats>();

        if (playerStats == null)
            return;

        if (animationController != null)
            animationController.PlayAttack();

        playerStats.AddHealth(-damage);
        _damageTimer = damageInterval;
    }

    // 只在夜晚激活
    private bool IsEnemyActive()
    {
        if (_dayNightCycle == null)
            return true;

        return _dayNightCycle.IsNight();
    }
}
