// 玩家状态基类。每个具体状态继承本类，只需重写用到的方法，其余留空不会出错。
// 状态拆分的好处：每个状态只关心自己的逻辑，避免 Update 里写一大堆 if/switch。
public abstract class PlayerState
{
    protected PlayerController controller;
    protected PlayerStateMachine stateMachine;

    protected PlayerState(PlayerController controller, PlayerStateMachine stateMachine)
    {
        this.controller = controller;
        this.stateMachine = stateMachine;
    }

    // 进入状态时调用一次（初始化、触发动画等）
    public virtual void Enter() { }

    // 离开状态时调用一次（清理、恢复权限等）
    public virtual void Exit() { }

    // 每帧读取输入并判断状态转移条件
    public virtual void HandleInput() { }

    // 每帧逻辑更新（非物理）
    public virtual void Update() { }

    // 固定时间步物理更新
    public virtual void FixedUpdate() { }
}
