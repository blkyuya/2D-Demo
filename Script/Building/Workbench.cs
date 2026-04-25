using System.Collections.Generic;
using UnityEngine;

// 工作台交互。OnEnable 时注册到全局列表，
// 建造系统检测「是否在工作台附近」时直接遍历列表，
// 避免每次放置建筑都 FindObjectsOfType(Workbench) 全场景扫描。
public class Workbench : MonoBehaviour, IInteractable
{
    public static readonly List<Workbench> RegisteredInstances = new List<Workbench>(4);

    // 注册到全局列表
    private void OnEnable()
    {
        if (!RegisteredInstances.Contains(this))
            RegisteredInstances.Add(this);
    }

    // 从全局列表移除
    private void OnDisable()
    {
        RegisteredInstances.Remove(this);
    }

    public string GetInteractionText()
    {
        return "按 E 打开工作台";
    }

    // 按 E 打开合成面板，同时关闭背包防止两个面板叠开
    public void Interact()
    {
        if (WorkbenchUI.Instance != null)
        {
            if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen)
                InventoryUI.Instance.ClosePanel();

            WorkbenchUI.Instance.OpenPanel(isAtWorkbench: true);
            return;
        }

        Debug.LogWarning("[Workbench] 未在场景中找到 WorkbenchUI 实例，请在 Canvas 下挂载 WorkbenchUI 脚本。");
    }
}
