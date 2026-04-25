using UnityEngine;

// 玩家近战攻击逻辑。
// 鼠标点击 → 计算攻击方向 → 切换到 AttackState → 动画关键帧事件 OnAttackHit → 扇形范围检测伤害。
// 伤害通过 IDamageable 接口施加，不依赖具体的敌人类型。
public class PlayerCombat : MonoBehaviour
{
    [Header("攻击设置")]
    [SerializeField] private int attackDamage = 5;
    [Tooltip("扇形半径：从玩家位置起算的最大攻击距离")]
    [SerializeField] private float attackRange = 1f;
    [Tooltip("扇形半角（度），总张角为两倍；例如 60 表示左右各 60°")]
    [SerializeField] private float attackSectorHalfAngle = 60f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("攻击冷却")]
    [SerializeField] private float attackCooldown = 0.3f;

    private float _attackTimer = 0f;
    private Camera _mainCamera;
    private PlayerController _playerController;
    private Animator _animator;
    private PlayerEquipment _playerEquipment;

    // 本次攻击的朝向，由 TryAttack 写入，OnAttackHit 读取
    private Vector2 _pendingAttackDirection = Vector2.down;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _playerController = GetComponent<PlayerController>();
        // Animator 常挂在子物体 Visual 上，只用 GetComponent 会得到 null
        _animator = GetComponent<Animator>();
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>(true);
        _playerEquipment = GetComponent<PlayerEquipment>();
    }

    // 仅冷却计时；左键攻击由当前 FSM 状态在 HandleInput 里调用 TryProcessAttackInput（常见写法）
    private void Update()
    {
        if (_attackTimer > 0f)
            _attackTimer -= Time.deltaTime;
    }

    // 供 Idle / Move 等状态在 HandleInput 中调用：本帧左键且通过校验则切入攻击，返回是否已处理
    public bool TryProcessAttackInput()
    {
        if (!Input.GetMouseButtonDown(0))
            return false;

        if (GameplayInputGate.ShouldBlockCombatInput())
            return false;

        // 建造预览时左键用于摆放，不与攻击抢同一次按键
        if (BuildingSystem.Instance != null && BuildingSystem.Instance.IsPlacing)
            return false;

        return TryAttackInternal();
    }

    // 前置检查（冷却 / 已在攻击中等），通过后触发攻击
    private bool TryAttackInternal()
    {
        if (_attackTimer > 0f)
            return false;

        if (_playerController != null)
        {
            if (_playerController.IsHarvesting)
                return false;
            if (_playerController.StateMachine.CurrentState == _playerController.AttackState)
                return false;
        }

        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
                return false;
        }

        Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        Vector2 attackDirection = (mouseWorldPos - transform.position).normalized;

        if (attackDirection.sqrMagnitude <= 0.001f)
            attackDirection = Vector2.down;

        _pendingAttackDirection = attackDirection;

        if (_playerController != null)
            _playerController.SetAttackDirection(attackDirection);

        if (_animator != null)
        {
            _playerController.StateMachine.ChangeState(_playerController.AttackState);
        }
        else
            ApplyAttackDamage();

        _attackTimer = attackCooldown;
        return true;
    }

    // 由攻击动画关键帧事件调用，与挥击时机对齐
    public void OnAttackHit()
    {
        ApplyAttackDamage();
    }

    // 以玩家为顶点、朝向为对称轴的扇形：先圆形粗筛，再按距离与角度精筛，对 IDamageable 扣血（含工具加成）
    private void ApplyAttackDamage()
    {
        Vector2 origin = transform.position;
        Vector2 dir = _pendingAttackDirection.sqrMagnitude > 0.0001f ? _pendingAttackDirection.normalized : Vector2.down;
        float rangeSqr = attackRange * attackRange;

        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, attackRange, enemyLayer);

        int totalDamage = attackDamage;
        if (_playerEquipment != null)
        {
            ItemData tool = _playerEquipment.GetActiveHandToolData();
            if (tool != null)
                totalDamage += tool.meleeDamageBonus;
        }

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D col = hits[i];
            if (col == null)
                continue;

            Vector2 closest = col.ClosestPoint(origin);
            Vector2 toTarget = closest - origin;
            if (toTarget.sqrMagnitude > rangeSqr)
                continue;

            if (toTarget.sqrMagnitude > 0.0001f && Vector2.Angle(dir, toTarget) > attackSectorHalfAngle)
                continue;

            IDamageable damageable = col.GetComponent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(totalDamage);
        }
    }

    // 编辑器中可视化攻击范围，方便调整参数
    private void OnDrawGizmosSelected()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        Vector2 attackDirection = (mouseWorldPos - transform.position).normalized;

        if (attackDirection.sqrMagnitude <= 0.001f)
            attackDirection = Vector2.down;

        Vector2 origin = transform.position;

        Gizmos.color = Color.red;
        DrawAttackSectorGizmo(origin, attackDirection, attackRange, attackSectorHalfAngle);
    }

    // 在 Scene 视图绘制扇形边界（两条边 + 弧）
    private static void DrawAttackSectorGizmo(Vector2 origin, Vector2 forward, float radius, float halfAngleDeg)
    {
        forward.Normalize();
        int segments = Mathf.Clamp(Mathf.RoundToInt(halfAngleDeg * 2f), 8, 48);
        Vector3 edgeNeg = origin + (Vector3)(RotateVector2(forward, -halfAngleDeg).normalized * radius);
        Gizmos.DrawLine((Vector3)origin, edgeNeg);
        Vector3 prev = edgeNeg;
        for (int s = 1; s <= segments; s++)
        {
            float t = Mathf.Lerp(-halfAngleDeg, halfAngleDeg, s / (float)segments);
            Vector3 next = origin + (Vector3)(RotateVector2(forward, t).normalized * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
        Vector3 edgePos = origin + (Vector3)(RotateVector2(forward, halfAngleDeg).normalized * radius);
        Gizmos.DrawLine((Vector3)origin, edgePos);
    }

    private static Vector2 RotateVector2(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float c = Mathf.Cos(rad);
        float s = Mathf.Sin(rad);
        return new Vector2(c * v.x - s * v.y, s * v.x + c * v.y);
    }
}
