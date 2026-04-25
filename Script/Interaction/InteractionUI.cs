using UnityEngine;
using TMPro;

// 交互提示 UI：当玩家靠近可交互对象时显示提示文本，离开时隐藏。
// 使用单例供 PlayerInteraction 直接调用，无需事件总线（低频 UI，直接引用即可）。
public class InteractionUI : MonoBehaviour
{
    public static InteractionUI Instance;

    // 文字根物体，用来整体显示/隐藏（含背景图等子物体）
    public GameObject textRoot;

    // TextMeshPro 文字组件，用来改显示内容
    public TMP_Text interactionText;

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    // 显示指定内容的提示文本
    public void Show(string content)
    {
        textRoot.SetActive(true);
        interactionText.text = content;
    }

    // 隐藏提示
    public void Hide()
    {
        textRoot.SetActive(false);
    }
}
