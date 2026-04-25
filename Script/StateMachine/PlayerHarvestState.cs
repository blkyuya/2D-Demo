// 采集状态：进入时锁定移动并触发采集动画，动画结束后由 Animation Event 切回 Idle
public class PlayerHarvestState : PlayerState
{
    public PlayerHarvestState(PlayerController controller, PlayerStateMachine stateMachine)
        : base(controller, stateMachine)
    {
    }

    // 锁移动、触发动画，整个采集过程中玩家不能走动
    public override void Enter()
    {
        controller.SetCanMove(false);
        controller.SetHarvesting(true);
        controller.StopMovement();

        if (controller.Anim != null)
        {
            controller.Anim.SetTrigger("HarvestTrigger");
        }
    }

    // 动画结束后恢复移动权限（由 OnHarvestAnimationFinished 触发 ChangeState 到 Idle）
    public override void Exit()
    {
        controller.SetCanMove(true);
        controller.SetHarvesting(false);
    }
}
