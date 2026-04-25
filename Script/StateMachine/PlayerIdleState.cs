using UnityEngine;

// 待机状态：进入时停止移动，每帧检查是否需要切换到其他状态
public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(PlayerController controller, PlayerStateMachine stateMachine)
        : base(controller, stateMachine)
    {
    }

    // 进入待机时立刻停步，防止上一状态残留速度
    public override void Enter()
    {
        controller.StopMovement();
    }

    // 按优先级依次检查状态转移条件：死亡 > 采集 > 建造 > 移动
    public override void HandleInput()
    {
        controller.ReadMovementInput();

        if (controller.IsDead)
        {
            stateMachine.ChangeState(controller.DeadState);
            return;
        }

        if (controller.IsHarvesting)
        {
            stateMachine.ChangeState(controller.HarvestState);
            return;
        }

        if (controller.IsInBuildingPlacementMode())
        {
            stateMachine.ChangeState(controller.BuildingState);
            return;
        }

        // 常见写法：当前状态统一处理可切入攻击（门禁在 PlayerCombat 内）
        if (controller.TryProcessAttackInput())
            return;

        if (controller.MoveInput != Vector2.zero)
        {
            stateMachine.ChangeState(controller.MoveState);
        }
    }
}
