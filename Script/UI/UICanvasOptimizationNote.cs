using UnityEngine;

// UGUI 性能优化说明（动静分离）：此组件无逻辑，仅作文档锚点，挂在 Canvas 根物体上方便查阅。
// 实践要点（需在编辑器手动调整，代码不覆盖 Inspector 布局）：
// 1. 将"长期不变"的背景/边框 与 "频繁刷新"的文本/图标 拆到不同 Canvas，减少网格重建范围；
// 2. 同一界面图标打入同一 Sprite Atlas，提高合批并降低 DrawCall；
// 3. 不接收射线的 Image/Text 关闭 Raycast Target，降低 Overdraw 和射线检测开销。
public sealed class UICanvasOptimizationNote : MonoBehaviour { }
