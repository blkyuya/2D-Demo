using System;
using UnityEngine;

// 玩家装备数据（与 InventoryUI 的 6 格装备槽同步）。
// 挂载在 Player 根物体上，装备变化时广播 OnEquipmentChanged 事件，
// 武器显示（PlayerWeaponVisual）和战斗系统（PlayerCombat）订阅此事件刷新。
public class PlayerEquipment : MonoBehaviour
{
    public static PlayerEquipment Instance { get; private set; }

    // 装备变化时通知手持武器显示等系统刷新
    public event Action OnEquipmentChanged;

    private readonly ItemType?[] _slots = new ItemType?[6];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // 由 InventoryUI 在刷新或关闭面板时调用，将 UI 状态同步到数据层
    public void ApplyEquippedSlots(ItemType?[] equipped)
    {
        if (equipped == null || equipped.Length != _slots.Length)
            return;

        bool changed = false;
        for (int i = 0; i < _slots.Length; i++)
        {
            ItemType? a = _slots[i];
            ItemType? b = equipped[i];
            if (a.HasValue != b.HasValue || (a.HasValue && b.HasValue && a.Value != b.Value))
                changed = true;
            _slots[i] = b;
        }

        if (changed)
            OnEquipmentChanged?.Invoke();
    }

    // 检查某物品是否已装备（任意一格）
    public bool IsItemEquipped(ItemType itemType)
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i].HasValue && _slots[i].Value == itemType)
                return true;
        }

        return false;
    }

    // 用于手持武器显示：从左到右找第一个工具类且有手持贴图的槽位
    public ItemType? GetFirstHandheldToolItem()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (!_slots[i].HasValue)
                continue;
            ItemData data = ItemDatabase.Instance != null
                ? ItemDatabase.Instance.GetItemData(_slots[i].Value)
                : null;
            if (data == null || data.category != ItemCategory.Tool)
                continue;
            if (data.GetHandheldDisplaySprite() != null)
                return _slots[i];
        }

        return null;
    }

    // 近战玩法用：找第一个工具类装备（不需要手持贴图）
    public ItemType? GetFirstEquippedToolForGameplay()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (!_slots[i].HasValue)
                continue;
            ItemData data = ItemDatabase.Instance != null
                ? ItemDatabase.Instance.GetItemData(_slots[i].Value)
                : null;
            if (data != null && data.category == ItemCategory.Tool)
                return _slots[i];
        }

        return null;
    }

    // 返回当前主手工具的 ItemData（近战伤害加成使用）
    public ItemData GetActiveHandToolData()
    {
        ItemType? t = GetFirstEquippedToolForGameplay();
        if (!t.HasValue || ItemDatabase.Instance == null)
            return null;
        return ItemDatabase.Instance.GetItemData(t.Value);
    }
}
