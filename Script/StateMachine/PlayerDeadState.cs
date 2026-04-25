// 死亡状态：进入后锁定所有移动，等待玩家按 R 复活（由 PlayerRespawn 监听）
public class PlayerDeadState : PlayerState
{
    public PlayerDeadState(PlayerController controller, PlayerStateMachine stateMachine)
        : base(controller, stateMachine)
    {
    }

    // 死亡时立刻停止一切位移
    public override void Enter()
    {
        controller.SetCanMove(false);
        controller.StopMovement();
    }
}
