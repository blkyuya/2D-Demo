// 事件总线使用的载荷结构体（值类型，减少堆分配）。
// 与 GameEvents 中旧式 static event 并存，便于渐进迁移到 EventBus。

// 物品拾取
public readonly struct ItemPickedUpPayload
{
    public readonly ItemType ItemType;
    public readonly int Amount;

    public ItemPickedUpPayload(ItemType itemType, int amount)
    {
        ItemType = itemType;
        Amount = amount;
    }
}

// 昼夜阶段变化
public readonly struct DayPhaseChangedPayload
{
    public readonly DayPhase Phase;

    public DayPhaseChangedPayload(DayPhase phase)
    {
        Phase = phase;
    }
}

// 建筑成功放置（任务 / 统计用）
public readonly struct BuildingPlacedPayload
{
    public readonly BuildingRecipeSO Recipe;

    public BuildingPlacedPayload(BuildingRecipeSO recipe)
    {
        Recipe = recipe;
    }
}

// 背包数据发生变化（UI 可订阅刷新，无需直接引用 PlayerInventory）
public readonly struct InventoryChangedPayload
{
    // 无额外字段：表示「仅通知变化」，具体数量由 Model 查询
}

// 玩家饥饿值变化
public readonly struct HungerChangedPayload
{
    public readonly int Current;
    public readonly int Max;

    public HungerChangedPayload(int current, int max)
    {
        Current = current;
        Max = max;
    }
}

// 玩家生命值变化
public readonly struct HealthChangedPayload
{
    public readonly int Current;
    public readonly int Max;

    public HealthChangedPayload(int current, int max)
    {
        Current = current;
        Max = max;
    }
}
