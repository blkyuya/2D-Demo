using UnityEngine;

// 玩家交互检测：每帧用 OverlapCircleAll 找最近的可交互对象，按 E 触发。
// 可采集对象（IHarvestable）走动画流程；普通交互对象（箱子、营火等）直接调用 Interact()。
public class PlayerInteraction : MonoBehaviour
{
    public float interactionRadius = 1.5f;
    public LayerMask interactableLayer;

    private IInteractable _currentInteractable;
    private IInteractable _pendingHarvestInteractable;

    private bool _isHarvesting = false;

    private void Update()
    {
        if (PauseMenu.IsPaused)
            return;

        DetectInteractable();
        HandleInteractionInput();
        UpdateInteractionUI();
    }

    // 响应 E 键：采集对象走动画流程，其他对象直接交互
    private void HandleInteractionInput()
    {
        if (_isHarvesting)
            return;

        if (_currentInteractable != null && Input.GetKeyDown(KeyCode.E))
        {
            PlayerController playerController = GetComponent<PlayerController>();

            if (_currentInteractable is IHarvestable harvestable)
            {
                if (!harvestable.CanHarvest())
                    return;

                // 暂存采集目标，等动画关键帧事件再执行实际采集
                _pendingHarvestInteractable = _currentInteractable;
                _isHarvesting = true;

                if (playerController != null)
                {
                    playerController.SetHarvesting(true);
                    playerController.PlayHarvestAnimation();
                }
            }
            else
            {
                _currentInteractable.Interact();
            }
        }
    }

    // 圆形范围内找最近的 IInteractable（多个时取距离最近的那个）
    private void DetectInteractable()
    {
        _currentInteractable = null;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRadius, interactableLayer);

        float closestDistance = Mathf.Infinity;

        foreach (Collider2D hit in hits)
        {
            IInteractable interactable = hit.GetComponent<IInteractable>();

            if (interactable != null)
            {
                float distance = Vector2.Distance(transform.position, hit.transform.position);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    _currentInteractable = interactable;
                }
            }
        }
    }

    // 有可交互对象时显示提示文本，否则隐藏
    private void UpdateInteractionUI()
    {
        if (InteractionUI.Instance == null)
            return;

        if (_currentInteractable != null)
            InteractionUI.Instance.Show(_currentInteractable.GetInteractionText());
        else
            InteractionUI.Instance.Hide();
    }

    // 采集动画关键帧事件：执行实际采集逻辑
    public void ExecuteHarvestInteraction()
    {
        if (_pendingHarvestInteractable != null)
            _pendingHarvestInteractable.Interact();
    }

    // 采集动画结束后清理状态，让玩家恢复正常操作
    public void FinishHarvestAnimation()
    {
        _isHarvesting = false;
        _pendingHarvestInteractable = null;

        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
            playerController.SetHarvesting(false);
    }

    // 编辑器中可视化交互范围
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
