using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Addressables 资源管理入口，支持开局按标签预加载。
// 优化要点：整标签 LoadAssetsAsync 完成时一次性反序列化大量资源，易造成进度条结束后长时间无响应；
// 改为按 IResourceLocation 逐个 LoadAssetAsync 并在每个资源之间 yield，把峰值摊到多帧。
public class ResManager : SingletonMonoBehaviour<ResManager>
{
    [Header("开局预加载")]
    [Tooltip("关闭则只做 InitializeAsync，不在开局拉取整包标签资源（显著缩短进 Play 后等待）")]
    [SerializeField] private bool preloadOnStartup = false;

    [Tooltip("要预加载的标签名，与 Addressables 面板 Labels 一致")]
    [SerializeField] private string[] startupPreloadLabels = new string[0];

    [Header("预加载策略")]
    [Tooltip("true：按地址逐个加载且每资源后分帧；false：整标签一次性加载（更快但更容易造成卡顿）")]
    [SerializeField] private bool gradualPreloadPerAsset = true;

    // Addressables 初始化是否完成
    public bool IsReady { get; private set; }

    // 整标签批量加载句柄（gradualPreloadPerAsset=false 时使用）
    private readonly List<AsyncOperationHandle<IList<Object>>> _batchHandles =
        new List<AsyncOperationHandle<IList<Object>>>(4);

    // 逐个资源加载句柄（用于 Release）
    private readonly List<AsyncOperationHandle<Object>> _singleAssetHandles = new List<AsyncOperationHandle<Object>>(64);

    protected override bool PersistAcrossScenes => true;

    // ResManager 需要只让本节点 DDOL，避免把同 Managers 物体下的 PauseMenu 等一起带过去
    protected override void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        // 先把子物体全部脱离父级，再对本节点单独 DDOL
        while (transform.childCount > 0)
            transform.GetChild(0).SetParent(null, worldPositionStays: true);

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(InitializeAddressablesRoutine());

#if UNITY_EDITOR
        // 即将退出 Play Mode 时提前释放句柄，避免 OnDestroy 阶段等待异步任务卡数十秒
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
    }

#if UNITY_EDITOR
    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            ReleaseTrackedPreloads();
        }
    }
#endif

    // 初始化 Addressables，成功后按配置决定是否预加载
    private IEnumerator InitializeAddressablesRoutine()
    {
        var init = Addressables.InitializeAsync();
        yield return init;

        if (init.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogWarning("[ResManager] Addressables 初始化未成功，请检查 Addressables Groups。");
            yield break;
        }

        IsReady = true;

        if (!preloadOnStartup || startupPreloadLabels == null)
            yield break;

        for (int i = 0; i < startupPreloadLabels.Length; i++)
        {
            string label = startupPreloadLabels[i];
            if (string.IsNullOrWhiteSpace(label))
                continue;

            if (gradualPreloadPerAsset)
                yield return PreloadLabelGradualRoutine(label.Trim());
            else
                PreloadByLabelBatch(label.Trim());

            yield return null;
        }
    }

    // 旧版：整标签一次性加载（完成时可能较重，不推荐）
    private void PreloadByLabelBatch(string label)
    {
        if (string.IsNullOrEmpty(label) || !IsReady)
            return;

        AsyncOperationHandle<IList<Object>> handle = Addressables.LoadAssetsAsync<Object>(label, null);
        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
                _batchHandles.Add(op);
#if UNITY_EDITOR
            else
                Debug.LogWarning($"[ResManager] 批量预加载失败: {label}");
#endif
        };
    }

    // 推荐：先取地址列表，再逐个 LoadAssetAsync，每步 yield 分帧，避免连续解压假死
    private IEnumerator PreloadLabelGradualRoutine(string label)
    {
        AsyncOperationHandle<IList<IResourceLocation>> locHandle =
            Addressables.LoadResourceLocationsAsync(label, typeof(UnityEngine.Object));
        yield return locHandle;

        if (locHandle.Status != AsyncOperationStatus.Succeeded || locHandle.Result == null)
        {
            Addressables.Release(locHandle);
            yield break;
        }

        IList<IResourceLocation> locs = locHandle.Result;
        Addressables.Release(locHandle);

        for (int i = 0; i < locs.Count; i++)
        {
            IResourceLocation loc = locs[i];
            AsyncOperationHandle<Object> assetHandle = Addressables.LoadAssetAsync<Object>(loc);
            yield return assetHandle;

            if (assetHandle.Status == AsyncOperationStatus.Succeeded)
                _singleAssetHandles.Add(assetHandle);
            else
                Addressables.Release(assetHandle);

            // 每加载一个资源让出一帧，把峰值摊平
            yield return null;
        }
    }

    // 对外 API：按标签预加载（走分帧逐个加载逻辑）
    public void PreloadByLabel(string label)
    {
        if (string.IsNullOrEmpty(label) || !IsReady)
            return;

        if (gradualPreloadPerAsset)
            StartCoroutine(PreloadLabelGradualRoutine(label));
        else
            PreloadByLabelBatch(label);
    }

    // 释放本管理器持有的所有 Addressables 句柄
    public void ReleaseTrackedPreloads()
    {
        for (int i = 0; i < _batchHandles.Count; i++)
        {
            if (_batchHandles[i].IsValid())
                Addressables.Release(_batchHandles[i]);
        }
        _batchHandles.Clear();

        for (int i = 0; i < _singleAssetHandles.Count; i++)
        {
            if (_singleAssetHandles[i].IsValid())
                Addressables.Release(_singleAssetHandles[i]);
        }
        _singleAssetHandles.Clear();
    }

    protected override void OnDestroy()
    {
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
        ReleaseTrackedPreloads();
        base.OnDestroy();
    }
}
