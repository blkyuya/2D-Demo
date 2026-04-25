using UnityEngine;

// 敌人动画控制：封装 Animator 参数写入，使 EnemyController 和 EnemyContactDamage 不直接操作 Animator。
// 使用 StringToHash 缓存哈希值，避免每帧字符串比对的开销。
public class EnemyAnimationController : MonoBehaviour
{
    [Header("动画组件")]
    [SerializeField] private Animator animator;

    private static readonly int MoveXHash  = Animator.StringToHash("MoveX");
    private static readonly int SpeedHash  = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    // 记录最近一次水平朝向，攻击时仍需保持朝向正确
    private float _lastMoveX = 1f;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);
        }
    }

    // 是否处于攻击状态（含切入 Attack 的过渡），用于禁止位移覆盖
    public bool IsInAttackState()
    {
        if (animator == null)
            return false;

        const int layer = 0;
        // 切入 Attack 的过渡期间也视为攻击中
        if (animator.IsInTransition(layer))
        {
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(layer);
            if (next.shortNameHash == AttackHash || next.IsName("Attack"))
                return true;
        }

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layer);
        return current.shortNameHash == AttackHash || current.IsName("Attack");
    }

    // 同步移动方向和速度参数（攻击中不修改，防止打断攻击）
    public void UpdateMovement(Vector2 moveDirection, bool isMoving)
    {
        if (IsInAttackState())
            return;

        if (moveDirection.x > 0.01f)
            _lastMoveX = 1f;
        else if (moveDirection.x < -0.01f)
            _lastMoveX = -1f;

        if (animator == null)
            return;

        animator.SetFloat(MoveXHash, _lastMoveX);
        animator.SetFloat(SpeedHash, isMoving ? 1f : 0f);
    }

    // 触发攻击动画：先停走，保持朝向，再触发 Attack Trigger
    public void PlayAttack()
    {
        if (animator == null)
            return;

        animator.SetFloat(SpeedHash, 0f);
        animator.SetFloat(MoveXHash, _lastMoveX);
        animator.SetTrigger(AttackHash);
    }
}
