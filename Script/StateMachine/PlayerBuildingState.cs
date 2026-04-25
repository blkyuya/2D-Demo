using UnityEngine;

// 建造放置状态：放置预览期间仍可自由移动，退出建造模式后回到 Idle 或 Move。
// 单独拎出一个状态，在面试时方便清晰地描述「建造期间的输入与逻辑边界」。
public class PlayerBuildingState : PlayerState
{
    public PlayerBuildingState(PlayerController controller, PlayerStateMachine stateMachine)
        : base(controller, stateMachine)
    {
    }

    // 死亡 > 采集 > 退出建造（根据是否在移动决定切 Idle 还是 Move）
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

        if (!controller.IsInBuildingPlacementMode())
        {
            if (controller.MoveInput == Vector2.zero)
                stateMachine.ChangeState(controller.IdleState);
            else
                stateMachine.ChangeState(controller.MoveState);
            return;
        }

        if (controller.MoveInput == Vector2.zero)
        {
            controller.StopMovement();
        }
    }

    // 建造期间有输入就正常移动
    public override void FixedUpdate()
    {
        if (controller.MoveInput != Vector2.zero)
            controller.Move();
    }

    // 离开建造态不强制停步，下一状态（Idle/Move）会自行处理
    public override void Exit()
    {
    }
}
