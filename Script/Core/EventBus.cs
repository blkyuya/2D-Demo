using System;
using System.Collections.Generic;

// 全局事件总线（发布/订阅模式）。
// 模块之间通过「载荷结构体」通信，Controller 改 Model 后 Publish，View 订阅刷新自身。
// 用 lock 保证线程安全（Job 回调若需切主线程再 Publish）；
// 广播时先对订阅列表做快照拷贝，避免回调内增删订阅时遍历集合抛异常。
public static class EventBus
{
    private static readonly object _lock = new object();
    private static readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();

    // 订阅某类型载荷的事件；同一 handler 不重复添加
    public static void Subscribe<T>(Action<T> handler) where T : struct
    {
        if (handler == null) return;

        lock (_lock)
        {
            Type key = typeof(T);
            if (!_subscribers.TryGetValue(key, out List<Delegate> list))
            {
                list = new List<Delegate>(4);
                _subscribers[key] = list;
            }

            if (!list.Contains(handler))
                list.Add(handler);
        }
    }

    // 取消订阅；列表清空时顺手移除 key 减少遍历开销
    public static void Unsubscribe<T>(Action<T> handler) where T : struct
    {
        if (handler == null) return;

        lock (_lock)
        {
            Type key = typeof(T);
            if (!_subscribers.TryGetValue(key, out List<Delegate> list))
                return;

            list.Remove(handler);
            if (list.Count == 0)
                _subscribers.Remove(key);
        }
    }

    // 发布事件。先快照再逐个 Invoke，单个订阅者抛异常不影响其他订阅者
    public static void Publish<T>(T payload) where T : struct
    {
        List<Delegate> snapshot;

        lock (_lock)
        {
            Type key = typeof(T);
            if (!_subscribers.TryGetValue(key, out List<Delegate> list) || list.Count == 0)
                return;

            snapshot = new List<Delegate>(list);
        }

        for (int i = 0; i < snapshot.Count; i++)
        {
            if (snapshot[i] is Action<T> action)
            {
                try
                {
                    action.Invoke(payload);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }
    }

    // 清空所有订阅（场景切换或退出时调用，防止残留引用导致内存泄漏）
    public static void ClearAllSubscribers()
    {
        lock (_lock)
        {
            _subscribers.Clear();
        }
    }
}
