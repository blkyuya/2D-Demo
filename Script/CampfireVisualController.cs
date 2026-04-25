using UnityEngine;

// 营火视觉控制器：根据 Campfire.IsLit() 控制火焰和光晕的显示/隐藏，
// 点燃状态下使用 Perlin Noise 驱动缩放和透明度的随机抖动，模拟火焰摇曳效果。
public class CampfireVisualController : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private Campfire      campfire;
    [SerializeField] private Transform     fireVisual;
    [SerializeField] private SpriteRenderer fireSpriteRenderer;
    [SerializeField] private Transform     glowVisual;
    [SerializeField] private SpriteRenderer glowSpriteRenderer;

    [Header("火焰闪动参数")]
    [SerializeField] private float flickerSpeed    = 6f;
    [SerializeField] private float scaleVariation  = 0.08f;
    [SerializeField] private float alphaVariation  = 0.15f;

    private Vector3 _fireBaseScale;
    private Vector3 _glowBaseScale;
    private Color   _fireBaseColor;
    private Color   _glowBaseColor;

    private void Awake()
    {
        // 未手动赋值时尝试从同级 Campfire 获取
        if (campfire == null)
            campfire = GetComponent<Campfire>();

        if (fireVisual != null)         _fireBaseScale = fireVisual.localScale;
        if (glowVisual != null)         _glowBaseScale = glowVisual.localScale;
        if (fireSpriteRenderer != null) _fireBaseColor = fireSpriteRenderer.color;
        if (glowSpriteRenderer != null) _glowBaseColor = glowSpriteRenderer.color;
    }

    private void Update()
    {
        if (campfire == null) return;

        bool isLit = campfire.IsLit();
        UpdateActiveState(isLit);

        if (isLit)
            UpdateFlicker();
    }

    // 控制火焰和光晕节点的激活状态（熄灭时全部隐藏）
    private void UpdateActiveState(bool isLit)
    {
        if (fireVisual != null) fireVisual.gameObject.SetActive(isLit);
        if (glowVisual != null) glowVisual.gameObject.SetActive(isLit);
    }

    // 基于 Perlin Noise 对缩放和 alpha 施加随机偏移，产生自然的火焰闪烁感
    private void UpdateFlicker()
    {
        float noise1 = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
        float noise2 = Mathf.PerlinNoise(0f, Time.time * flickerSpeed);

        float scaleOffset = Mathf.Lerp(-scaleVariation, scaleVariation, noise1);
        float alphaOffset = Mathf.Lerp(-alphaVariation, alphaVariation, noise2);

        if (fireVisual != null)
            fireVisual.localScale = _fireBaseScale + Vector3.one * scaleOffset;

        // 光晕缩放幅度是火焰的一半，避免看起来过于夸张
        if (glowVisual != null)
            glowVisual.localScale = _glowBaseScale + Vector3.one * (scaleOffset * 0.5f);

        if (fireSpriteRenderer != null)
        {
            Color c = _fireBaseColor;
            c.a = Mathf.Clamp01(_fireBaseColor.a + alphaOffset);
            fireSpriteRenderer.color = c;
        }

        if (glowSpriteRenderer != null)
        {
            Color c = _glowBaseColor;
            c.a = Mathf.Clamp01(_glowBaseColor.a + alphaOffset * 0.5f);
            glowSpriteRenderer.color = c;
        }
    }
}
