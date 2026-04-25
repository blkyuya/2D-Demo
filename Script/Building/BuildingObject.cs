using UnityEngine;

// 可拆除的建筑：按 E 交互后退还材料并销毁自身。
// 挂载在需要支持「拆除返还」的建筑预制体上。
public class BuildingObject : MonoBehaviour, IInteractable
{
    [Header("返还材料")]
    public int refundWood = 0;
    public int refundStone = 0;

    // 拆除时退还材料并销毁建筑
    public void Interact()
    {
        PlayerInventory playerInventory = FindObjectOfType<PlayerInventory>();

        if (playerInventory == null)
            return;

        if (refundWood > 0)
            playerInventory.AddItem(ItemType.Wood, refundWood);

        if (refundStone > 0)
            playerInventory.AddItem(ItemType.Stone, refundStone);

        Destroy(gameObject);
    }

    public string GetInteractionText()
    {
        return "按 E 拆除建筑";
    }
}
