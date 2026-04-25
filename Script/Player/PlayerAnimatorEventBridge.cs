using UnityEngine;

// Unity 的 Animation Event 在「挂 Animator 的 GameObject」上查找函数。
// 若 Animator 在子物体而 PlayerController 在父物体，把本脚本挂到子物体上，
// 事件触发时转发到父物体的 PlayerController，避免找不到方法的警告。
[DisallowMultipleComponent]
public class PlayerAnimatorEventBridge : MonoBehaviour
{
    [SerializeField] private PlayerController target;

    // 找不到显式引用时自动向上查找父级
    private void Awake()
    {
        if (target == null)
            target = GetComponentInParent<PlayerController>();
    }

    // 攻击动画最后一帧触发，通知 PlayerController 结束攻击硬直
    public void OnAttackAnimationFinished()
    {
        target?.OnAttackAnimationFinished();
    }
}
