using UnityEngine;

// 2D 精灵 Y 轴排序：按世界坐标 Y 驱动 SortingOrder，越靠下 Y 越小则 Order 越大（显示在前）。
// 防闪烁三重机制：
//   1. Y 量化 (yQuantizationWorldUnits)：将 Y 值取整到最近网格，消除动画/根骨骼带来的微小抖动；
//   2. 稳定帧数 (requiredStableFrames)：连续若干帧结果相同才真正写入，防止 Order 在相邻值间来回跳；
//   3. InstanceID tie-break (tieBreakOffset)：同层同 Y 时加入实例唯一偏移，区分不同对象避免 Z 争抢。
[DefaultExecutionOrder(0)]
[DisallowMultipleComponent]
public class SpriteStableYSort2D : MonoBehaviour
{
    [Header("排序")]
    [Tooltip("基准 SortingOrder，可与敌人/环境错开不同 base")]
    [SerializeField] private int baseSortingOrder;

    [Tooltip("世界 Y 每变化 1 单位对应的 Order 变化量（通常角色越靠下 Y 越小越应显示在前，取负 Y * 正系数）")]
    [SerializeField] private float orderPerWorldYUnit = 100f;

    [Header("防闪烁")]
    [Tooltip("排序锚点（如脚底空物体）；为空则用本物体 Transform，动画带根位移时建议单独挂锚点")]
    [SerializeField] private Transform sortAnchor;

    [Tooltip("Y 值量化精度（世界单位），抑制动画微小 Y 抖动；≤0 表示不量化")]
    [SerializeField] private float yQuantizationWorldUnits = 0.035f;

    [Tooltip("连续多少帧得到相同 Order 才真正写入，防止 Order 在相邻值间来回跳")]
    [SerializeField] private int requiredStableFrames = 2;

    [Tooltip("0~99 的同层同 Y tie-break 偏移；设为 0 时 Awake 自动用 InstanceID 取模生成")]
    [SerializeField] private int tieBreakOffset;

    [Tooltip("目标 SpriteRenderer；为空则取本物体上的；多子物体请用 Sorting Group")]
    [SerializeField] private SpriteRenderer targetSpriteRenderer;

    private int  _publishedOrder;
    private bool _hasPublished;
    private int  _candidateOrder  = int.MaxValue;
    private int  _candidateStreak;

    private void Awake()
    {
        if (targetSpriteRenderer == null)
            targetSpriteRenderer = GetComponent<SpriteRenderer>();

        // tie-break 未手动设置时，用 InstanceID 的低两位确保每个实例不同
        if (tieBreakOffset == 0)
            tieBreakOffset = Mathf.Abs(gameObject.GetInstanceID() % 100);

        if (targetSpriteRenderer != null)
        {
            _publishedOrder = targetSpriteRenderer.sortingOrder;
            _hasPublished   = true;
        }
    }

    private void LateUpdate()
    {
        if (targetSpriteRenderer == null) return;

        Transform t = sortAnchor != null ? sortAnchor : transform;
        float y = t.position.y;

        // 量化处理：将 Y 四舍五入到最近网格，消除微小抖动
        if (yQuantizationWorldUnits > 0.0001f)
            y = Mathf.Round(y / yQuantizationWorldUnits) * yQuantizationWorldUnits;

        int computed = baseSortingOrder + Mathf.RoundToInt(-y * orderPerWorldYUnit) + tieBreakOffset;

        // 结果与已发布值相同则跳过
        if (_hasPublished && computed == _publishedOrder) return;

        // 候选值变化时重置连续帧计数
        if (computed != _candidateOrder)
        {
            _candidateOrder  = computed;
            _candidateStreak = 1;
            return;
        }

        // 累积稳定帧数，达到阈值才写入
        _candidateStreak++;
        if (_candidateStreak < requiredStableFrames) return;

        _publishedOrder = computed;
        _hasPublished   = true;
        targetSpriteRenderer.sortingOrder = computed;
    }
}
