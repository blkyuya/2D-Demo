using UnityEngine;
using UnityEngine.UI;

// 建造菜单中的单个配方格子：显示图标和选中高亮，未解锁时降低透明度。
public class BuildingUISlot : MonoBehaviour
{
    [Header("UI 引用")]
    public Image      icon;
    public GameObject highlight;

    // 统一刷新格子的显示状态：图标、选中高亮、可见性、是否解锁
    public void SetData(Sprite sprite, bool selected, bool visible, bool unlocked = true)
    {
        gameObject.SetActive(visible);
        if (!visible) return;

        if (icon != null)
        {
            icon.sprite  = sprite;
            icon.enabled = sprite != null;
            // 未解锁时显示半透明，表示此配方当前无法使用
            icon.color   = unlocked ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        }

        if (highlight != null)
            highlight.SetActive(selected);
    }
}
