using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 在角色手部叠加武器贴图，随朝向翻转。
// 优先使用子物体「WeaponSlot」作为手部挂点（在 Scene 里拖到手掌位置）；
// 找不到时运行时自动创建，不需要强制场景配置。
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerEquipment))]
[DefaultExecutionOrder(100)]
public class PlayerWeaponVisual : MonoBehaviour
{
    [Header("引用（可不填：自动从本物体 / 子物体查找）")]
    [Tooltip("与 Player 同物体上的 PlayerController")]
    [SerializeField] private PlayerController playerController;

    [Tooltip("身体贴图 SpriteRenderer，用于排序对齐")]
    [SerializeField] private SpriteRenderer bodySpriteRenderer;

    [Tooltip("武器父节点，自动找子物体「WeaponSlot」；没有则运行时创建")]
    [SerializeField] private Transform weaponSlotRoot;

    [SerializeField] private Transform weaponAnchor;
    [SerializeField] private SpriteRenderer weaponSpriteRenderer;

    [Header("显示模式")]
    [Tooltip("关闭后不在角色身上画武器（可用底部 UI 表示已装备）")]
    [SerializeField] private bool showWorldHandWeapon = false;

    [Tooltip("朝左时自动把挂点镜像到另一侧；若 Player 根物体已用 Scale X=-1 翻面请取消，否则会重复镜像")]
    [SerializeField] private bool mirrorWeaponSlotForFacing = true;

    [Header("自动查找子物体名")]
    [SerializeField] private string weaponSlotChildName = "WeaponSlot";
    [SerializeField] private string visualChildName = "Visual";
    [SerializeField] private string legacyAxeChildName = "Axe";

    private PlayerEquipment _equipment;
    private Transform _dynamicWeaponChild;

    // 记录朝右时 WeaponSlot 的本地坐标，朝左时对称翻转
    private Vector3 _weaponSlotBaseline;
    private bool _hasWeaponSlotBaseline;

    private ItemData _cachedHandheldData;

#if UNITY_EDITOR
    // 编辑模式下从组件右键菜单创建持久化 WeaponSlot 子物体
    [ContextMenu("【生成】子物体 WeaponSlot（手部挂点，可再微调位置）")]
    private void EditorCreatePersistentWeaponSlot()
    {
        if (transform.Find(weaponSlotChildName) != null)
        {
            Debug.LogWarning("[PlayerWeaponVisual] 已存在名为 " + weaponSlotChildName + " 的子物体。");
            weaponSlotRoot = transform.Find(weaponSlotChildName);
            EditorUtility.SetDirty(this);
            return;
        }

        var go = new GameObject(weaponSlotChildName);
        Undo.RegisterCreatedObjectUndo(go, "Create WeaponSlot");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0.35f, 0.05f, 0f);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        weaponSlotRoot = go.transform;
        EditorUtility.SetDirty(this);
        Selection.activeTransform = go.transform;
        Debug.Log("[PlayerWeaponVisual] 已创建 " + weaponSlotChildName + "，请在 Scene 视图拖到手掌附近。");
    }
#endif

    // 编辑模式下自动补全引用，Inspector 里不会一直显示 None
    private void OnValidate()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (bodySpriteRenderer == null)
        {
            Transform visual = transform.Find(visualChildName);
            if (visual != null)
                bodySpriteRenderer = visual.GetComponent<SpriteRenderer>();
            if (bodySpriteRenderer == null)
                bodySpriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (weaponSlotRoot == null)
            weaponSlotRoot = transform.Find(weaponSlotChildName);
    }

    private void Reset()
    {
        playerController = GetComponent<PlayerController>();
        OnValidate();
    }

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();
        _equipment = GetComponent<PlayerEquipment>();
        if (_equipment == null)
            _equipment = GetComponentInParent<PlayerEquipment>();

        ResolveBodySprite();
        ResolveWeaponSlotRoot();
        EnsureWeaponObjects();
    }

    // 记录 WeaponSlot 初始坐标（朝右时的基准位置）
    private void Start()
    {
        if (weaponSlotRoot != null)
        {
            _weaponSlotBaseline = weaponSlotRoot.localPosition;
            _hasWeaponSlotBaseline = true;
        }
    }

    // 查找身体 SpriteRenderer，供排序层对齐使用
    private void ResolveBodySprite()
    {
        if (bodySpriteRenderer != null)
            return;
        Transform visual = transform.Find(visualChildName);
        if (visual != null)
            bodySpriteRenderer = visual.GetComponent<SpriteRenderer>();
        if (bodySpriteRenderer == null)
            bodySpriteRenderer = GetComponent<SpriteRenderer>();
        if (bodySpriteRenderer == null)
            bodySpriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    // 查找或创建 WeaponSlot 根物体
    private void ResolveWeaponSlotRoot()
    {
        if (weaponSlotRoot != null)
            return;

        weaponSlotRoot = transform.Find(weaponSlotChildName);

        if (weaponSlotRoot == null)
        {
            foreach (Transform c in transform)
            {
                if (c.name == weaponSlotChildName)
                {
                    weaponSlotRoot = c;
                    break;
                }
            }
        }

        // 仍没有时自动创建，避免武器贴在角色根 pivot
        if (weaponSlotRoot == null)
        {
            var go = new GameObject(weaponSlotChildName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0.35f, 0.05f, 0f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            weaponSlotRoot = go.transform;
        }
    }

    // 装备变化时刷新手持武器贴图
    private void OnEnable()
    {
        if (_equipment != null)
            _equipment.OnEquipmentChanged += RefreshHandheld;
        RefreshHandheld();
    }

    private void OnDisable()
    {
        if (_equipment != null)
            _equipment.OnEquipmentChanged -= RefreshHandheld;
    }

    // 每帧镜像挂点坐标，同步武器贴图的朝向和排序层
    private void LateUpdate()
    {
        if (playerController == null)
            return;

        float face = GetFacingSignX();

        // 朝左时把挂点 X 镜像到另一侧，保持武器在「手」的正确位置
        if (mirrorWeaponSlotForFacing && weaponSlotRoot != null && _hasWeaponSlotBaseline)
        {
            Vector3 p = _weaponSlotBaseline;
            p.x = Mathf.Abs(p.x) * face;
            weaponSlotRoot.localPosition = p;
        }

        if (weaponAnchor == null || weaponSpriteRenderer == null)
            return;
        if (!weaponSpriteRenderer.enabled)
            return;

        // ItemData 手持偏移的 X 随朝向镜像
        if (_cachedHandheldData != null)
        {
            Vector2 off = _cachedHandheldData.handheldLocalOffset;
            off.x *= face;
            weaponAnchor.localPosition = off;
        }

        weaponAnchor.localScale = Vector3.one;
        weaponSpriteRenderer.flipX = face < 0f;

        // 与身体 SpriteRenderer 同步排序层，避免武器与场景精灵错误穿插闪烁
        if (bodySpriteRenderer != null)
        {
            weaponSpriteRenderer.sortingLayerID = bodySpriteRenderer.sortingLayerID;
            int off = _cachedHandheldData != null ? _cachedHandheldData.handheldSortingOrderOffset : 1;
            weaponSpriteRenderer.sortingOrder = bodySpriteRenderer.sortingOrder + off;
        }
    }

    // 移动时取移动方向的 X；静止时取最近一次攻击方向的 X
    private float GetFacingSignX()
    {
        float lx = playerController.LastMoveDirection.x;
        if (Mathf.Abs(lx) > 0.05f)
            return Mathf.Sign(lx);
        return playerController.LastAttackDirectionX >= 0f ? 1f : -1f;
    }

    // 确保武器锚点和 SpriteRenderer 存在，关闭预制体里的旧占位 Axe 子物体
    private void EnsureWeaponObjects()
    {
        Transform parentForWeapon = weaponSlotRoot != null ? weaponSlotRoot : transform;

        if (weaponAnchor == null)
        {
            _dynamicWeaponChild = parentForWeapon.Find("手部武器显示");
            if (_dynamicWeaponChild != null)
            {
                weaponAnchor = _dynamicWeaponChild;
            }
            else
            {
                var go = new GameObject("手部武器显示");
                go.transform.SetParent(parentForWeapon, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                weaponAnchor = go.transform;
                _dynamicWeaponChild = weaponAnchor;
            }
        }

        if (weaponSpriteRenderer == null)
        {
            weaponSpriteRenderer = weaponAnchor.GetComponent<SpriteRenderer>();
            if (weaponSpriteRenderer == null)
                weaponSpriteRenderer = weaponAnchor.gameObject.AddComponent<SpriteRenderer>();
        }

        weaponSpriteRenderer.enabled = false;

        DisableLegacyAxeIfAny();
    }

    // 关闭预制体里占位的 Axe 子物体，避免与动态手持图重叠
    private void DisableLegacyAxeIfAny()
    {
        if (weaponSlotRoot == null)
            return;
        Transform legacy = weaponSlotRoot.Find(legacyAxeChildName);
        if (legacy != null)
            legacy.gameObject.SetActive(false);
    }

    // 装备变化时刷新手持武器贴图：找到工具类物品则显示对应 Sprite，否则隐藏
    private void RefreshHandheld()
    {
        EnsureWeaponObjects();
        _cachedHandheldData = null;
        if (_equipment == null || weaponSpriteRenderer == null)
            return;

        ItemType? item = _equipment.GetFirstHandheldToolItem();
        if (!item.HasValue)
        {
            weaponSpriteRenderer.sprite = null;
            weaponSpriteRenderer.enabled = false;
            return;
        }

        ItemData data = ItemDatabase.Instance != null
            ? ItemDatabase.Instance.GetItemData(item.Value)
            : null;
        _cachedHandheldData = data;

        if (!showWorldHandWeapon)
        {
            weaponSpriteRenderer.sprite = null;
            weaponSpriteRenderer.enabled = false;
            return;
        }

        Sprite sprite = data != null ? data.GetHandheldDisplaySprite() : null;
        if (sprite == null)
        {
            weaponSpriteRenderer.sprite = null;
            weaponSpriteRenderer.enabled = false;
            return;
        }

        weaponSpriteRenderer.sprite = sprite;
        weaponSpriteRenderer.enabled = true;

        // localPosition 在 LateUpdate 里按朝向写入，此处只设排序层
        if (bodySpriteRenderer != null)
        {
            weaponSpriteRenderer.sortingLayerID = bodySpriteRenderer.sortingLayerID;
            weaponSpriteRenderer.sortingOrder = bodySpriteRenderer.sortingOrder +
                (data != null ? data.handheldSortingOrderOffset : 1);
        }
        else
        {
            weaponSpriteRenderer.sortingOrder = 1;
        }
    }
}
