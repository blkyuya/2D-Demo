using System.Collections.Generic;
using UnityEngine;
using System;

// 玩家背包 Model 层：用 Dictionary 存储物品数量，增删时触发 C# event 和 EventBus 双通道通知。
// UI 层（InventoryUI）订阅 OnInventoryChanged，无需直接引用本组件，符合 MVC 解耦原则。
public class PlayerInventory : MonoBehaviour
{
    private Dictionary<ItemType, int> _itemCounts = new Dictionary<ItemType, int>();

    // UI 层订阅此事件刷新显示
    public event Action OnInventoryChanged;

    // 初始给玩家一些起始物资，方便开局体验
    private void Start()
    {
        AddItem(ItemType.Wood, 5);
        AddItem(ItemType.Stone, 5);
    }

    // 增加物品数量（物品不存在则创建条目）
    public void AddItem(ItemType itemType, int amount)
    {
        if (!_itemCounts.ContainsKey(itemType))
            _itemCounts[itemType] = 0;

        _itemCounts[itemType] += amount;
        RefreshInventoryUI();
    }

    // 消耗物品（先检查是否足量），成功返回 true
    public bool UseItem(ItemType itemType, int amount)
    {
        if (!_itemCounts.ContainsKey(itemType)) return false;
        if (_itemCounts[itemType] < amount) return false;

        _itemCounts[itemType] -= amount;
        RefreshInventoryUI();
        return true;
    }

    // 检查某物品是否有足够数量（不扣减，仅查询）
    public bool HasEnoughItem(ItemType itemType, int amount)
    {
        return GetItemCount(itemType) >= amount;
    }

    // 移除物品，数量不足则直接返回 false 不做操作
    public bool RemoveItem(ItemType itemType, int amount)
    {
        if (!HasEnoughItem(itemType, amount)) return false;

        _itemCounts[itemType] -= amount;
        RefreshInventoryUI();
        return true;
    }

    // 查询某物品当前数量，不存在返回 0
    public int GetItemCount(ItemType itemType)
    {
        if (_itemCounts.ContainsKey(itemType))
            return _itemCounts[itemType];

        return 0;
    }

    // 序列化为存档用数据结构
    public InventorySaveData GetSaveData()
    {
        InventorySaveData data = new InventorySaveData();
        data.woodCount = GetItemCount(ItemType.Wood);
        data.stoneCount = GetItemCount(ItemType.Stone);
        data.berryCount = GetItemCount(ItemType.Berry);
        return data;
    }

    // 从存档数据恢复背包（先清空再写入，避免旧数据残留）
    public void LoadFromSaveData(InventorySaveData data)
    {
        if (data == null)
            return;

        _itemCounts.Clear();
        _itemCounts[ItemType.Wood]  = Mathf.Max(0, data.woodCount);
        _itemCounts[ItemType.Stone] = Mathf.Max(0, data.stoneCount);
        _itemCounts[ItemType.Berry] = Mathf.Max(0, data.berryCount);

        RefreshInventoryUI();
    }

    // 通知所有监听方背包已变化（C# event + EventBus 双通道）
    private void RefreshInventoryUI()
    {
        OnInventoryChanged?.Invoke();
        EventBus.Publish(new InventoryChangedPayload());
    }
}
