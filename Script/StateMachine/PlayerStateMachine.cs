// 玩家有限状态机（FSM）。
// 只负责状态切换，不包含任何具体逻辑，具体行为全部由各状态类自己负责。
public class PlayerStateMachine
{
    public PlayerState CurrentState { get; private set; }

    // 初始化并直接进入起始状态
    public void Initialize(PlayerState startState)
    {
        CurrentState = startState;
        CurrentState.Enter();
    }

    // 切换到新状态：先 Exit 旧状态，再 Enter 新状态
    public void ChangeState(PlayerState newState)
    {
        if (CurrentState != null)
        {
            CurrentState.Exit();
        }

        CurrentState = newState;

        if (CurrentState != null)
        {
            CurrentState.Enter();
        }
    }
}
