using UnityEngine;

// 移动状态：在 FixedUpdate 里驱动刚体，同时每帧判断是否需要切换状态
public class PlayerMoveState : PlayerState
{
    public PlayerMoveState(PlayerController controller, PlayerStateMachine stateMachine)
        : base(controller, stateMachine)
    {
    }

    // 同 IdleState，死亡 > 采集 > 建造 > 停止（切回待机）
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

        if (controller.TryProcessAttackInput())
            return;

        if (controller.MoveInput == Vector2.zero)
        {
            stateMachine.ChangeState(controller.IdleState);
        }
    }

    // 物理步里施加速度，避免在 Update 改 velocity 与物理引擎冲突
    public override void FixedUpdate()
    {
        controller.Move();
    }

    // 离开移动状态时清零速度，防止惯性滑行
    public override void Exit()
    {
        controller.StopMovement();
    }
}
