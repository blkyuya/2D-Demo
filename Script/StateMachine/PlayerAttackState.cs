using UnityEngine;

// 攻击状态：进入时锁移动并触发攻击动画，动画结束后通过 Animation Event 或帧超时切回 Idle。
// 同时提供两层兜底（normalizedTime >= 0.99 和 StuckTimeout），防止事件丢失时永远卡在攻击态。
public class PlayerAttackState : PlayerState
{
    // 标记 Animator 是否已进入过 Attack 节点（防止切入前误判为已离开）
    private bool _animatorSawAttackState;

    // 进入本状态的时间戳，用于动画未进入 Attack 节点时的超时兜底
    private float _enteredAt;

    private const string AttackAnimatorStateName = "Attack";

    // 片段接近播完但事件偶发未触发时，强制切回 Idle 的阈值
    private const float NormalizedTimeFallback = 0.99f;

    // 从未进入 Attack 动画节点时的最大等待时间，避免无限卡住
    private const float StuckTimeoutSeconds = 2f;

    public PlayerAttackState(PlayerController controller, PlayerStateMachine stateMachine)
        : base(controller, stateMachine)
    {
    }

    // 锁移动、触发攻击 Trigger，重置标志位
    public override void Enter()
    {
        _animatorSawAttackState = false;
        _enteredAt = Time.time;

        controller.SetCanMove(false);
        controller.StopMovement();

        if (controller.Anim != null)
            controller.Anim.SetTrigger("attackTrigger");
    }

    // 攻击结束后恢复移动权限
    public override void Exit()
    {
        controller.SetCanMove(true);
    }

    // 攻击过程中屏蔽移动输入，避免位移动画与攻击动画互相干扰
    public override void HandleInput()
    {
    }

    // 每帧轮询 Animator 状态，处理「动画结束」和「超时兜底」两种退出路径
    public override void Update()
    {
        Animator anim = controller.Anim;
        if (anim == null)
        {
            stateMachine.ChangeState(controller.IdleState);
            return;
        }

        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);

        if (info.IsName(AttackAnimatorStateName))
            _animatorSawAttackState = true;

        // 还没进入 Attack 节点时等待，超时则强制退出
        if (!_animatorSawAttackState)
        {
            if (Time.time - _enteredAt >= StuckTimeoutSeconds)
                stateMachine.ChangeState(controller.IdleState);
            return;
        }

        // 已经进入过 Attack，现在已离开（含过渡结束）→ 切回 Idle
        if (!info.IsName(AttackAnimatorStateName))
        {
            stateMachine.ChangeState(controller.IdleState);
            return;
        }

        // 仍在 Attack 节点但片段将结束、Animation Event 偶发未触发时的最后兜底
        if (info.normalizedTime >= NormalizedTimeFallback)
            stateMachine.ChangeState(controller.IdleState);
    }

    // 攻击期间每帧清零速度，防止脚本执行顺序导致本帧仍残留移动速度
    public override void FixedUpdate()
    {
        if (controller.Rb != null)
            controller.Rb.velocity = Vector2.zero;
    }
}
