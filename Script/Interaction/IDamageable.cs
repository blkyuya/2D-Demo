// 可受伤对象接口：敌人、某些建筑等实现此接口。
// PlayerCombat 通过此接口施加伤害，不依赖具体的 EnemyHealth 等类。
public interface IDamageable
{
    void TakeDamage(int damage);
}
