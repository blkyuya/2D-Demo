using System;

// 全局游戏事件（兼容旧代码的静态 event）。
// 每次 Raise 时同步广播到 EventBus，新模块优先订阅 EventBus 泛型事件；
// 旧模块可继续订阅本类事件，实现渐进式解耦。
public static class GameEvents
{
    public static event Action<ItemType, int> OnItemPickedUp;
    public static event Action OnPlayerDied;
    public static event Action OnPlayerRespawned;
    public static event Action<DayPhase> OnDayPhaseChanged;
    public static event Action<BuildingRecipeSO> OnBuildingPlaced;
    public static event Action OnCampfireExtinguished;

    // 物品拾取：旧订阅继续收到，同时广播到 EventBus 供新模块使用
    public static void RaiseItemPickedUp(ItemType itemType, int amount)
    {
        OnItemPickedUp?.Invoke(itemType, amount);
        EventBus.Publish(new ItemPickedUpPayload(itemType, amount));
    }

    // 玩家死亡
    public static void RaisePlayerDied()
    {
        OnPlayerDied?.Invoke();
    }

    // 玩家复活
    public static void RaisePlayerRespawned()
    {
        OnPlayerRespawned?.Invoke();
    }

    // 昼夜阶段变化（白天 / 黄昏 / 夜晚）
    public static void RaiseDayPhaseChanged(DayPhase phase)
    {
        OnDayPhaseChanged?.Invoke(phase);
        EventBus.Publish(new DayPhaseChangedPayload(phase));
    }

    // 建筑成功放置到世界
    public static void RaiseBuildingPlaced(BuildingRecipeSO recipe)
    {
        OnBuildingPlaced?.Invoke(recipe);
        if (recipe != null)
            EventBus.Publish(new BuildingPlacedPayload(recipe));
    }

    // 营火燃料耗尽
    public static void RaiseCampfireExtinguished()
    {
        OnCampfireExtinguished?.Invoke();
    }
}
