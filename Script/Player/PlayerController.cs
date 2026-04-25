using UnityEngine;

// 玩家角色控制器（Controller 层）：读取输入、驱动刚体移动、同步动画参数、管理 FSM 状态切换。
// 状态机拆分了 Idle / Move / Harvest / Attack / Building / Dead 六个状态，
// 每个状态只关心自己的逻辑，避免在 Update 里维护巨大的条件判断树。
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody2D _rb;
    private Animator _animator;
    private PlayerCombat _playerCombat;

    private Vector2 _moveInput;
    private Vector2 _lastMoveDirection = Vector2.down;

    private bool _canMove = true;
    private bool _isHarvesting = false;
    private bool _isDead = false;

    public PlayerStateMachine StateMachine { get; private set; }

    public PlayerIdleState IdleState { get; private set; }
    public PlayerMoveState MoveState { get; private set; }
    public PlayerHarvestState HarvestState { get; private set; }
    public PlayerDeadState DeadState { get; private set; }

    // 建造放置模式（按住预览移动时与 Move 共用位移逻辑，单独状态便于描述 FSM 扩展点）
    public PlayerBuildingState BuildingState { get; private set; }

    // 近战攻击：锁定移动直至动画结束（Animation Event 切回 Idle）
    public PlayerAttackState AttackState { get; private set; }

    public Rigidbody2D Rb => _rb;
    public Animator Anim => _animator;
    public Vector2 MoveInput => _moveInput;
    public Vector2 LastMoveDirection => _lastMoveDirection;
    public bool CanMove => _canMove;
    public bool IsHarvesting => _isHarvesting;
    public bool IsDead => _isDead;

    private float _lastAttackDirectionX = 1f;
    public float LastAttackDirectionX => _lastAttackDirectionX;

    // 初始化所有组件引用和状态机实例
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        // Animator 常挂在子物体 Visual 上，仅用 GetComponent 会得到 null
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>(true);

        StateMachine = new PlayerStateMachine();
        IdleState = new PlayerIdleState(this, StateMachine);
        MoveState = new PlayerMoveState(this, StateMachine);
        HarvestState = new PlayerHarvestState(this, StateMachine);
        DeadState = new PlayerDeadState(this, StateMachine);
        BuildingState = new PlayerBuildingState(this, StateMachine);
        AttackState = new PlayerAttackState(this, StateMachine);
        _playerCombat = GetComponent<PlayerCombat>();
    }

    // 由 Idle / Move 等状态的 HandleInput 调用，是否本帧已切入攻击态
    public bool TryProcessAttackInput()
    {
        return _playerCombat != null && _playerCombat.TryProcessAttackInput();
    }

    // 查询 BuildingSystem 是否处于放置预览模式
    public bool IsInBuildingPlacementMode()
    {
        return BuildingSystem.Instance != null && BuildingSystem.Instance.IsPlacing;
    }

    // 设置初始动画参数并启动 FSM
    private void Start()
    {
        if (_animator != null)
        {
            _animator.SetFloat("moveX", 0f);
            _animator.SetFloat("moveY", 0f);
            _animator.SetFloat("lastMoveX", 0f);
            _animator.SetFloat("lastMoveY", -1f);
            _animator.SetBool("isMoving", false);
            _animator.SetFloat("attackDirX", 1f);
        }

        StateMachine.Initialize(IdleState);
    }

    // UI 遮挡时直接停止移动，否则交给当前状态处理输入和逻辑更新
    private void Update()
    {
        if (GameplayInputGate.ShouldBlockLocomotionAndFacing())
        {
            StopMovement();
            return;
        }

        StateMachine.CurrentState?.HandleInput();
        StateMachine.CurrentState?.Update();
    }

    // 物理更新交给当前状态处理
    private void FixedUpdate()
    {
        StateMachine.CurrentState?.FixedUpdate();
    }

    // 读取原始轴输入，记录最近一次朝向（用于动画），并归一化输入向量防止斜向加速
    public void ReadMovementInput()
    {
        if (!_canMove)
        {
            _moveInput = Vector2.zero;
            return;
        }

        _moveInput.x = Input.GetAxisRaw("Horizontal");
        _moveInput.y = Input.GetAxisRaw("Vertical");

        Vector2 rawInput = _moveInput;

        if (rawInput != Vector2.zero)
        {
            // 取绝对值大的轴作为朝向，避免斜向走时朝向不稳定
            if (Mathf.Abs(rawInput.x) > Mathf.Abs(rawInput.y))
                _lastMoveDirection = new Vector2(Mathf.Sign(rawInput.x), 0f);
            else
                _lastMoveDirection = new Vector2(0f, Mathf.Sign(rawInput.y));
        }

        _moveInput = rawInput.normalized;
        UpdateAnimation(rawInput);
    }

    // 清零速度并更新动画 isMoving = false
    public void StopMovement()
    {
        _moveInput = Vector2.zero;

        if (_rb != null)
            _rb.velocity = Vector2.zero;

        if (_animator != null)
            _animator.SetBool("isMoving", false);
    }

    // 在 FixedUpdate 中调用：施加移动速度
    public void Move()
    {
        if (_rb == null)
            return;

        _rb.velocity = _moveInput * moveSpeed;
    }

    // 外部锁定 / 解锁移动权限（如采集、攻击时需要锁定）
    public void SetCanMove(bool canMove)
    {
        _canMove = canMove;

        if (!canMove)
            StopMovement();
    }

    // 标记是否处于采集动画中（由 PlayerInteraction 和 HarvestState 共同维护）
    public void SetHarvesting(bool harvesting)
    {
        _isHarvesting = harvesting;
    }

    // 标记死亡状态（死亡后 FSM 会切换到 DeadState）
    public void SetDead(bool dead)
    {
        _isDead = dead;
    }

    // 同步 Animator 参数：移动方向、是否在移动、最后朝向
    private void UpdateAnimation(Vector2 rawInput)
    {
        if (_animator == null)
            return;

        Vector2 animDirection = Vector2.zero;

        if (rawInput != Vector2.zero)
        {
            if (Mathf.Abs(rawInput.x) > Mathf.Abs(rawInput.y))
                animDirection = new Vector2(Mathf.Sign(rawInput.x), 0f);
            else
                animDirection = new Vector2(0f, Mathf.Sign(rawInput.y));
        }

        _animator.SetFloat("moveX", animDirection.x);
        _animator.SetFloat("moveY", animDirection.y);
        _animator.SetBool("isMoving", rawInput != Vector2.zero);
        _animator.SetFloat("lastMoveX", _lastMoveDirection.x);
        _animator.SetFloat("lastMoveY", _lastMoveDirection.y);
    }

    // 触发采集动画并切换到 HarvestState
    public void PlayHarvestAnimation()
    {
        if (_animator == null)
            return;

        StateMachine.ChangeState(HarvestState);
    }

    // 采集动画关键帧事件：执行实际采集逻辑
    public void OnHarvestAction()
    {
        PlayerInteraction interaction = GetComponent<PlayerInteraction>();
        if (interaction != null)
            interaction.ExecuteHarvestInteraction();
    }

    // 采集动画结束事件：恢复状态
    public void OnHarvestAnimationFinished()
    {
        PlayerInteraction interaction = GetComponent<PlayerInteraction>();
        if (interaction != null)
            interaction.FinishHarvestAnimation();

        if (_isDead)
        {
            StateMachine.ChangeState(DeadState);
            return;
        }

        StateMachine.ChangeState(IdleState);
    }

    // 记录攻击朝向（X 轴方向），并同步给 Animator 控制攻击动画方向
    public void SetAttackDirection(Vector2 attackDirection)
    {
        if (attackDirection.x < -0.01f)
            _lastAttackDirectionX = -1f;
        else if (attackDirection.x > 0.01f)
            _lastAttackDirectionX = 1f;

        if (_animator != null)
            _animator.SetFloat("attackDirX", _lastAttackDirectionX);
    }

    // 挂在攻击动画最后一帧的 Animation Event，解除硬直并回到 Idle
    public void OnAttackAnimationFinished()
    {
        if (_isDead)
        {
            StateMachine.ChangeState(DeadState);
            return;
        }

        if (StateMachine.CurrentState == AttackState)
            StateMachine.ChangeState(IdleState);
    }
}
