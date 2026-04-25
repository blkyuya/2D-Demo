using UnityEngine;

// 挂在池化对象上，记录所属对象池的 Key，调用 ReturnToPool 时自动归还。
public class PooledObject : MonoBehaviour
{
    public string PoolKey { get; private set; }

    // 由 PoolManager 在创建时调用，绑定 Key
    public void SetPoolKey(string key)
    {
        PoolKey = key;
    }

    // 将自身归还到对应的对象池；没有 PoolManager 时退化为 SetActive(false)
    public void ReturnToPool()
    {
        if (!string.IsNullOrEmpty(PoolKey) && PoolManager.Instance != null)
            PoolManager.Instance.ReturnObject(PoolKey, gameObject);
        else
            gameObject.SetActive(false);
    }
}
