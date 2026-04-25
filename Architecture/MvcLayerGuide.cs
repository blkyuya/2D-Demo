// MVC 分层说明：本项目采用增量式 MVC，不一次性搬迁所有 MonoBehaviour，新代码优先按下列职责归类。
//
// Model（数据层）：
//   纯数据结构：SaveData、InventorySaveData 等存档类；
//   运行时数据组件：PlayerInventory（物品字典）、PlayerStats（生命/饥饿）——
//   仍挂在玩家 GameObject 上，对外通过事件/EventBus 通知，不暴露内部字段写入。
//
// View（视图层）：
//   InventoryUI、PlayerStatsUI、QuestUI 等只负责显示和 UI 输入转发；
//   通过订阅 GameEvents 或 EventBus 驱动刷新，不直接修改 Model。
//
// Controller（控制层）：
//   PlayerController + FSM、BuildingSystem、SaveManager、QuestManager 等负责规则和流程；
//   修改 Model 后调用 GameEvents.Raise* 或 EventBus.Publish 广播变化。
//
// 全局基础设施：EventBus（观察者/发布订阅）、CommandInvoker（命令模式/撤销）、
//   PoolManager（对象池）、ResManager（Addressables 异步加载）。
public static class MvcLayerGuide
{
    public const string DocumentationVersion = "1.0";
}
