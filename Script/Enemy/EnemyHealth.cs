using System;
using UnityEngine;

// 敌人生命值和死亡掉落，实现 IDamageable 接口供 PlayerCombat 统一调用。
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Serializable]
    public class EnemyDropEntry
    {
        public ItemType itemType;
        public int minAmount = 1;
        public int maxAmount = 1;
        [Range(0f, 1f)] public float dropChance = 1f;
    }

    [Header("生命值")]
    [SerializeField] private int maxHealth = 20;

    [Header("通用掉落 Prefab")]
    [SerializeField] private DroppedItem droppedItemPrefab;

    [Header("掉落表")]
    [SerializeField] private EnemyDropEntry[] dropTable;

    [Header("死亡特效")]
    [SerializeField] private GameObject deathEffectPrefab;

    private int _currentHealth;
    private bool _isDead = false;

    private void Awake()
    {
        _currentHealth = maxHealth;
    }

    // 受击：扣血，降到 0 时触发死亡
    public void TakeDamage(int damage)
    {
        if (_isDead)
            return;

        _currentHealth -= damage;

        if (_currentHealth <= 0)
            Die();
    }

    // 死亡：按掉落表生成物品，播放特效，销毁自身（_isDead 防止重复触发）
    private void Die()
    {
        if (_isDead)
            return;

        _isDead = true;

        SpawnDrops();
        SpawnDeathEffect();

        Destroy(gameObject);
    }

    // 按掉落表随机生成掉落物（概率判断 + 数量随机）
    private void SpawnDrops()
    {
        if (droppedItemPrefab == null)
        {
            Debug.LogWarning("[EnemyHealth] droppedItemPrefab 为空，无法生成掉落物");
            return;
        }

        if (dropTable == null || dropTable.Length == 0)
        {
            Debug.LogWarning("[EnemyHealth] dropTable 为空");
            return;
        }

        for (int i = 0; i < dropTable.Length; i++)
        {
            EnemyDropEntry entry = dropTable[i];
            if (entry == null)
                continue;

            // 概率检定
            if (UnityEngine.Random.value > entry.dropChance)
                continue;

            int amount = UnityEngine.Random.Range(entry.minAmount, entry.maxAmount + 1);
            if (amount <= 0)
                continue;

            // 随机偏移，避免掉落物全部叠在同一点
            Vector3 offset = new Vector3(
                UnityEngine.Random.Range(-0.3f, 0.3f),
                UnityEngine.Random.Range(-0.3f, 0.3f),
                0f
            );

            DroppedItem dropped = Instantiate(droppedItemPrefab, transform.position + offset, Quaternion.identity);

            Sprite icon = null;
            if (ItemDatabase.Instance != null)
            {
                ItemData data = ItemDatabase.Instance.GetItemData(entry.itemType);
                if (data != null)
                    icon = data.icon;
            }

            dropped.Initialize(entry.itemType, amount, icon);
        }
    }

    // 死亡时播放粒子特效
    private void SpawnDeathEffect()
    {
        if (deathEffectPrefab == null)
            return;

        Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
    }
}
