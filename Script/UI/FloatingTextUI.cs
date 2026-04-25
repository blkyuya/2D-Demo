using System.Collections;
using TMPro;
using UnityEngine;

// 单个飘字 UI：播放向上漂移并淡出的动画，结束后自动归还对象池。
public class FloatingTextUI : MonoBehaviour
{
    [Header("文本")]
    [SerializeField] private TMP_Text textLabel;

    [Header("动画设置")]
    [SerializeField] private float moveUpDistance = 50f;
    [SerializeField] private float duration = 1f;

    private CanvasGroup   _canvasGroup;
    private RectTransform _rectTransform;
    private Coroutine     _playCoroutine;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup   = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    // 设置文本内容和颜色后启动飘字动画（重复调用时自动中断上次动画）
    public void Play(string message, Color color)
    {
        if (textLabel != null)
        {
            textLabel.text  = message;
            textLabel.color = color;
        }

        if (_playCoroutine != null)
            StopCoroutine(_playCoroutine);

        _playCoroutine = StartCoroutine(PlayRoutine());
    }

    // 在 duration 时间内从起始位置向上漂移并同步淡出 alpha，结束后归池
    private IEnumerator PlayRoutine()
    {
        Vector2 startPos = _rectTransform.anchoredPosition;
        Vector2 endPos   = startPos + new Vector2(0f, moveUpDistance);

        float timer = 0f;
        _canvasGroup.alpha = 1f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            _rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        // 归池前重置位置和透明度，保证复用时从正确状态开始
        _rectTransform.anchoredPosition = startPos;
        _canvasGroup.alpha = 1f;

        PooledObject pooledObject = GetComponent<PooledObject>();
        if (pooledObject != null)
            pooledObject.ReturnToPool();
        else
            gameObject.SetActive(false);
    }
}
