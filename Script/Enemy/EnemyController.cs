using UnityEngine;

// 敌人追踪逻辑：只在夜晚激活，检测范围内有玩家则追过去，到停止距离后原地等待并发动攻击。
public class EnemyController : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed    = 2f;
    [SerializeField] private float detectRange  = 6f;
    [SerializeField] private float stopDistance = 0.6f;

    [Header("动画")]
    [SerializeField] private EnemyAnimationController animationController;

    private Transform _player;
    private DayNightCycle _dayNightCycle;

    // 启动时查找玩家和昼夜系统，Inspector 未拖入动画控制器时自动查找
    private void Start()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
            _player = player.transform;

        if (animationController == null)
        {
            animationController = GetComponent<EnemyAnimationController>();
            if (animationController == null)
                animationController = GetComponentInChildren<EnemyAnimationController>(true);
        }

        _dayNightCycle = FindObjectOfType<DayNightCycle>();
    }

    // 每帧判断是否应该追玩家，攻击中不位移（避免与攻击动画抢状态）
    private void Update()
    {
        if (PauseMenu.IsPaused)
        {
            SetIdle();
            return;
        }

        if (!IsEnemyActive())
        {
            SetIdle();
            return;
        }

        if (_player == null)
        {
            SetIdle();
            return;
        }

        if (animationController != null && animationController.IsInAttackState())
        {
            SetIdle();
            return;
        }

        float distance = Vector2.Distance(transform.position, _player.position);

        // 超出探测范围或已到攻击距离时停步
        if (distance > detectRange || distance <= stopDistance)
        {
            SetIdle();
            return;
        }

        Vector2 direction = (_player.position - transform.position).normalized;
        transform.position += (Vector3)(direction * moveSpeed * Time.deltaTime);

        if (animationController != null)
            animationController.UpdateMovement(direction, true);
    }

    // 只在夜晚激活；没有昼夜系统时默认一直激活
    private bool IsEnemyActive()
    {
        if (_dayNightCycle == null)
            return true;

        return _dayNightCycle.IsNight();
    }

    // 停止移动并通知动画控制器切回待机
    private void SetIdle()
    {
        if (animationController != null)
            animationController.UpdateMovement(Vector2.zero, false);
    }
}
