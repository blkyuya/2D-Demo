using UnityEngine;

// 通用单例 MonoBehaviour 基类，规范各 Manager 的 Instance 访问方式，
// 避免每个管理器都手写一遍重复的单例逻辑。
// 若场景中存在多个同类组件，保留最先 Awake 的，后续的直接销毁。
public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : SingletonMonoBehaviour<T>
{
    // 全局访问点；子类可额外定义 public static Xxx Instance 以兼容旧代码
    public static T Instance { get; protected set; }

    // 是否跨场景保留，默认随场景卸载（如需常驻可在子类 override 返回 true）
    protected virtual bool PersistAcrossScenes => false;

    // 子类可 override（如 ResManager 需单独处理 DDOL 范围）
    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this as T;

        if (PersistAcrossScenes)
            DontDestroyOnLoad(gameObject);
    }

    // 子类如有额外清理，务必 base.OnDestroy()
    protected virtual void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
