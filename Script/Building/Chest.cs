using System.Collections.Generic;
using UnityEngine;

// 箱子交互逻辑：内部用 Dictionary 存储任意 ItemType 的数量，
// 支持通过 ChestUI 的拖拽 UI 与玩家背包互相转移物品。
public class Chest : MonoBehaviour, IInteractable
{
    // 场景预置的初始物品（Inspector 可配），运行时迁移到 Dictionary 后由 Dictionary 接管
    [Header("箱子初始库存（场景预置值，运行时由 Dictionary 接管）")]
    [SerializeField] private int woodCount  = 3;
    [SerializeField] private int stoneCount = 1;
    [SerializeField] private int berryCount = 5;

    // 运行时内部存储，支持所有 ItemType
    private Dictionary<ItemType, int> _items = new Dictionary<ItemType, int>();

    // 对外暴露只读属性（保持对旧代码的兼容）
    public int WoodCount  => GetItemCount(ItemType.Wood);
    public int StoneCount => GetItemCount(ItemType.Stone);
    public int BerryCount => GetItemCount(ItemType.Berry);

    // 将 Inspector 配置的旧字段迁移到 Dictionary，保证初始值生效
    private void Awake()
    {
        _items[ItemType.Wood]  = woodCount;
        _items[ItemType.Stone] = stoneCount;
        _items[ItemType.Berry] = berryCount;
    }

    public string GetInteractionText()
    {
        return "按 E 打开箱子";
    }

    // 按 E 打开箱子 UI
    public void Interact()
    {
        if (ChestUI.Instance != null)
            ChestUI.Instance.OpenChest(this);
    }

    // 向箱子添加物品，支持所有 ItemType
    public void AddItem(ItemType itemType, int amount)
    {
        if (amount <= 0) return;

        if (!_items.ContainsKey(itemType))
            _items[itemType] = 0;

        _items[itemType] += amount;
    }

    // 从箱子移除物品，数量不足时返回 false，不做扣减
    public bool RemoveItem(ItemType itemType, int amount)
    {
        if (amount <= 0) return true;

        if (!_items.ContainsKey(itemType) || _items[itemType] < amount)
            return false;

        _items[itemType] -= amount;
        return true;
    }

    // 查询箱子中某物品的数量，无则返回 0
    public int GetItemCount(ItemType itemType)
    {
        return _items.TryGetValue(itemType, out int count) ? count : 0;
    }
}
