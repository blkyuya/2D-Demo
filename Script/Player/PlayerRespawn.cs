using UnityEngine;

// 玩家死亡与复活逻辑。
// 死亡时不销毁 GameObject，而是通过 enabled = false 禁用相关组件，
// 复活时再重新启用，避免重新 Instantiate 带来的初始化开销和引用断裂。
public class PlayerRespawn : MonoBehaviour
{
    private PlayerStats _playerStats;
    private PlayerController _playerController;
    private PlayerInteraction _playerInteraction;
    private BuildingSystem _buildingSystem;

    private Vector3 _spawnPosition;
    private bool _isDead = false;

    public bool IsDead => _isDead;

    // 记录初始位置作为复活点，获取同物体的各组件引用
    private void Start()
    {
        _playerStats       = GetComponent<PlayerStats>();
        _playerController  = GetComponent<PlayerController>();
        _playerInteraction = GetComponent<PlayerInteraction>();
        _buildingSystem    = GetComponent<BuildingSystem>();

        _spawnPosition = transform.position;
    }

    // 死亡后每帧检测 R 键（复活键）
    private void Update()
    {
        if (_isDead && Input.GetKeyDown(KeyCode.R))
            Respawn();
    }

    // 死亡处理：禁用控制 / 交互 / 建造组件，广播死亡事件，显示死亡 UI
    public void HandleDeath()
    {
        if (_isDead)
            return;

        _isDead = true;
        GameEvents.RaisePlayerDied();

        if (_playerController != null)
        {
            _playerController.SetDead(true);
            _playerController.StateMachine.ChangeState(_playerController.DeadState);
            _playerController.enabled = false;
        }

        if (_playerInteraction != null)
            _playerInteraction.enabled = false;

        if (_buildingSystem != null)
            _buildingSystem.enabled = false;

        if (DeathUI.Instance != null)
            DeathUI.Instance.ShowDeathUI(true);
    }

    // 复活：回到出生点、恢复属性、重新启用组件
    private void Respawn()
    {
        _isDead = false;
        GameEvents.RaisePlayerRespawned();

        transform.position = _spawnPosition;

        if (_playerStats != null)
            _playerStats.ResetStatsAfterRespawn();

        if (_playerController != null)
        {
            _playerController.SetDead(false);
            _playerController.enabled = true;
            _playerController.StateMachine.ChangeState(_playerController.IdleState);
        }

        if (_playerInteraction != null)
            _playerInteraction.enabled = true;

        if (_buildingSystem != null)
            _buildingSystem.enabled = true;

        if (DeathUI.Instance != null)
            DeathUI.Instance.ShowDeathUI(false);
    }
}
