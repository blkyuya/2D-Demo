using System.Collections.Generic;
using UnityEngine;

// 对象池管理器：用 Dictionary<string, Queue<GameObject>> 管理各类对象池，
// 减少频繁 Instantiate/Destroy 造成的 GC 压力。
// 双缓冲回收设计：归还对象先入「待合并队列」，取出前统一合并回主池，
// 避免同帧内 Get/Return 互相干扰。
public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance;

    [System.Serializable]
    public class PoolConfig
    {
        public string key;
        public GameObject prefab;
        public int initialSize = 10;
    }

    [Header("对象池配置")]
    [SerializeField] private PoolConfig[] pools;

    // 主池：存放空闲可复用的实例
    private readonly Dictionary<string, Queue<GameObject>> _poolDictionary   = new Dictionary<string, Queue<GameObject>>();
    private readonly Dictionary<string, GameObject>         _prefabDictionary = new Dictionary<string, GameObject>();

    // 回收缓冲：本帧归还的对象先在这里等，取出时合并回主池
    private readonly Dictionary<string, Queue<GameObject>> _returnBuffer = new Dictionary<string, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializePools();
    }

    // 按配置数组初始化各对象池
    private void InitializePools()
    {
        _poolDictionary.Clear();
        _prefabDictionary.Clear();

        if (pools == null || pools.Length == 0)
        {
            Debug.LogWarning("PoolManager: 没有配置任何对象池");
            return;
        }

        for (int i = 0; i < pools.Length; i++)
        {
            PoolConfig config = pools[i];

            if (config == null)
                continue;

            if (string.IsNullOrWhiteSpace(config.key))
            {
                Debug.LogWarning("PoolManager: 存在空 key，已跳过");
                continue;
            }

            if (config.prefab == null)
            {
                Debug.LogWarning("PoolManager: key = " + config.key + " 的 prefab 为空，已跳过");
                continue;
            }

            RegisterPool(config.key.Trim(), config.prefab, config.initialSize);
        }
    }

    // 注册一个对象池并预创建初始数量的实例
    private void RegisterPool(string key, GameObject prefab, int initialSize)
    {
        if (_poolDictionary.ContainsKey(key))
            return;

        Queue<GameObject> queue = new Queue<GameObject>();
        _poolDictionary.Add(key, queue);
        _returnBuffer[key] = new Queue<GameObject>();
        _prefabDictionary.Add(key, prefab);

        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = CreateNewObject(key, prefab);
            obj.SetActive(false);
            queue.Enqueue(obj);
        }
    }

    // 创建新实例并绑定 PoolKey（用于归还时找到正确的池）
    private GameObject CreateNewObject(string key, GameObject prefab)
    {
        GameObject obj = Instantiate(prefab);
        obj.name = prefab.name + "_Pooled";

        PooledObject pooledObject = obj.GetComponent<PooledObject>();
        if (pooledObject == null)
            pooledObject = obj.AddComponent<PooledObject>();

        pooledObject.SetPoolKey(key);
        return obj;
    }

    // 运行时按需注册：GetObject 找不到 key 时尝试从配置数组补注册
    private bool TryRegisterPoolAtRuntime(string key)
    {
        if (pools == null)
            return false;

        string requestKey = key.Trim();

        for (int i = 0; i < pools.Length; i++)
        {
            PoolConfig config = pools[i];
            if (config == null || string.IsNullOrWhiteSpace(config.key) || config.prefab == null)
                continue;

            if (config.key.Trim() == requestKey)
            {
                RegisterPool(requestKey, config.prefab, config.initialSize);
                return true;
            }
        }

        return false;
    }

    // 从对象池取出实例，池为空时自动扩容（Instantiate 兜底）
    public GameObject GetObject(string key, Vector3 position, Quaternion rotation)
    {
        string requestKey = key.Trim();

        if (!_poolDictionary.ContainsKey(requestKey))
        {
            bool registered = TryRegisterPoolAtRuntime(requestKey);

            if (!registered || !_poolDictionary.ContainsKey(requestKey))
            {
                Debug.LogWarning("对象池中不存在 key: " + requestKey);
                return null;
            }
        }

        // 取出前先合并回收缓冲，同帧内可复用刚归还的实例
        MergeReturnBuffer(requestKey);

        Queue<GameObject> queue = _poolDictionary[requestKey];
        GameObject obj = null;

        while (queue.Count > 0 && obj == null)
            obj = queue.Dequeue();

        // 池中没有空闲实例时扩容
        if (obj == null)
            obj = CreateNewObject(requestKey, _prefabDictionary[requestKey]);

        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        return obj;
    }

    // 归还实例：先入回收缓冲，由 MergeReturnBuffer 合并回主池
    public void ReturnObject(string key, GameObject obj)
    {
        if (obj == null)
            return;

        string requestKey = key.Trim();

        if (!_poolDictionary.ContainsKey(requestKey))
        {
            obj.SetActive(false);
            return;
        }

        obj.SetActive(false);

        if (!_returnBuffer.TryGetValue(requestKey, out Queue<GameObject> buf))
        {
            buf = new Queue<GameObject>();
            _returnBuffer[requestKey] = buf;
        }

        buf.Enqueue(obj);
    }

    // 每帧末尾统一合并一次，降低高频 Get/Return 时的队列抖动
    private void LateUpdate()
    {
        foreach (string k in _poolDictionary.Keys)
            MergeReturnBuffer(k);
    }

    // 将回收缓冲中的对象并入主池
    private void MergeReturnBuffer(string requestKey)
    {
        if (!_returnBuffer.TryGetValue(requestKey, out Queue<GameObject> buf) || buf == null || buf.Count == 0)
            return;

        Queue<GameObject> main = _poolDictionary[requestKey];
        while (buf.Count > 0)
            main.Enqueue(buf.Dequeue());
    }
}
