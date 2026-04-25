using UnityEngine;

// 建造系统：管理建筑配方选择、放置预览、碰撞检测和命令执行。
// 实际扣费 + 生成建筑封装在 PlaceBuildingCommand 里，保持命令模式的可撤销能力。
public class BuildingSystem : MonoBehaviour
{
    public static BuildingSystem Instance;
    public bool IsPlacing => _isPlacing;

    [Header("建造配方")]
    public BuildingRecipeSO[] recipes;

    [Header("放置检测")]
    public float checkRadius = 0.4f;
    public LayerMask blockingLayerMask;

    [Header("网格吸附")]
    public bool snapToGrid = true;
    public float gridSize = 1f;

    [Header("预览颜色")]
    public Color validColor = new Color(1f, 1f, 1f, 0.6f);
    public Color invalidColor = new Color(1f, 0.3f, 0.3f, 0.6f);

    [SerializeField] private Collider2D buildableArea;

    [Header("工作台范围限制")]
    [SerializeField] private float workbenchRange = 5f;

    private PlayerInventory _playerInventory;
    private Camera _mainCamera;

    private bool _isPlacing = false;
    private int _selectedRecipeIndex = 0;

    private GameObject _previewObject;
    private SpriteRenderer _previewSpriteRenderer;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _playerInventory = GetComponent<PlayerInventory>();
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        if (PauseMenu.IsPaused)
            return;

        HandleRecipeSelection();

        // C 键进入建造模式（背包打开时不响应）
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen)
                return;

            StartPlacingSelectedBuilding();
        }

        if (_isPlacing)
        {
            UpdatePreviewPosition();

            if (Input.GetMouseButtonDown(0))
                TryPlaceSelectedBuilding();

            if (Input.GetMouseButtonDown(1))
                CancelPlacement();
        }
    }

    // 数字键 1-9/0 切换选中的建筑配方
    private void HandleRecipeSelection()
    {
        for (int i = 0; i < 10; i++)
        {
            KeyCode key = i == 9 ? KeyCode.Alpha0 : KeyCode.Alpha1 + i;

            if (Input.GetKeyDown(key))
                SelectRecipe(i);
        }
    }

    // 切换配方索引，放置中时同步刷新 UI 和预览物体
    private void SelectRecipe(int index)
    {
        if (recipes == null || index < 0 || index >= recipes.Length)
            return;

        if (recipes[index] == null)
            return;

        _selectedRecipeIndex = index;

        if (_isPlacing)
        {
            RefreshBuildingUI();

            if (BuildingUI.Instance != null)
                BuildingUI.Instance.HideWarning();

            BuildingRecipeSO recipe = recipes[_selectedRecipeIndex];
            if (recipe.buildingPrefab != null)
                CreatePreviewObject(recipe.buildingPrefab);
        }
    }

    // 开始放置：创建预览物体，显示建造 UI
    private void StartPlacingSelectedBuilding()
    {
        if (_playerInventory == null) return;
        if (recipes == null || recipes.Length == 0) return;
        if (_selectedRecipeIndex < 0 || _selectedRecipeIndex >= recipes.Length) return;

        BuildingRecipeSO recipe = recipes[_selectedRecipeIndex];
        if (recipe == null || recipe.buildingPrefab == null) return;

        _isPlacing = true;
        CreatePreviewObject(recipe.buildingPrefab);
        RefreshBuildingUI();

        if (BuildingUI.Instance != null)
        {
            BuildingUI.Instance.ShowBuildingUI(true);
            BuildingUI.Instance.HideWarning();
        }
    }

    // 检查背包是否有足够材料
    private bool HasEnoughMaterials(BuildingRecipeSO recipe)
    {
        if (_playerInventory == null || recipe == null || recipe.costs == null)
            return false;

        for (int i = 0; i < recipe.costs.Length; i++)
        {
            BuildingCostData cost = recipe.costs[i];
            if (cost == null)
                continue;

            if (!_playerInventory.HasEnoughItem(cost.itemType, cost.amount))
                return false;
        }

        return true;
    }

    // 从背包扣除配方所需的所有材料
    private void RemoveMaterials(BuildingRecipeSO recipe)
    {
        if (_playerInventory == null || recipe == null || recipe.costs == null)
            return;

        for (int i = 0; i < recipe.costs.Length; i++)
        {
            BuildingCostData cost = recipe.costs[i];
            if (cost == null)
                continue;

            if (cost.amount > 0)
                _playerInventory.RemoveItem(cost.itemType, cost.amount);
        }
    }

    // 生成预览物体：禁用碰撞和功能组件，只保留视觉
    private void CreatePreviewObject(GameObject prefab)
    {
        if (_previewObject != null)
            Destroy(_previewObject);

        _previewObject = Instantiate(prefab);
        _previewObject.name = "建筑预览";

        ApplyEntitySortingLayer(_previewObject);

        Collider2D previewCollider = _previewObject.GetComponent<Collider2D>();
        if (previewCollider != null)
            previewCollider.enabled = false;

        Campfire previewCampfire = _previewObject.GetComponent<Campfire>();
        if (previewCampfire != null)
            previewCampfire.enabled = false;

        _previewSpriteRenderer = _previewObject.GetComponent<SpriteRenderer>();
        if (_previewSpriteRenderer != null)
            _previewSpriteRenderer.color = validColor;
    }

    // 每帧更新预览位置，并根据放置是否合法切换颜色
    private void UpdatePreviewPosition()
    {
        if (_mainCamera == null || _previewObject == null)
            return;

        Vector3 placePosition = GetCurrentPlacePosition();
        _previewObject.transform.position = placePosition;

        BuildingRecipeSO previewRecipe = recipes != null && _selectedRecipeIndex >= 0 && _selectedRecipeIndex < recipes.Length
            ? recipes[_selectedRecipeIndex]
            : null;

        bool canPlace = CanPlaceAtPosition(placePosition) && IsInsideBuildableArea(placePosition);
        // 需要工作台的配方：必须存在一台工作台，使角色与落点同时在其半径内（同一台）
        if (previewRecipe != null && previewRecipe.requiresWorkbench)
            canPlace = canPlace && IsPlayerAndPlacementNearSameWorkbench(placePosition);

        if (_previewSpriteRenderer != null)
            _previewSpriteRenderer.color = canPlace ? validColor : invalidColor;
    }

    // 前置检查全部通过后，通过命令模式提交放置
    private void TryPlaceSelectedBuilding()
    {
        if (_mainCamera == null)
            return;

        if (recipes == null || _selectedRecipeIndex < 0 || _selectedRecipeIndex >= recipes.Length)
            return;

        BuildingRecipeSO recipe = recipes[_selectedRecipeIndex];
        if (recipe == null)
            return;

        Vector3 placePosition = GetCurrentPlacePosition();

        if (!HasEnoughMaterials(recipe))
        {
            BuildingUI.Instance?.ShowWarning("材料不足");
            return;
        }

        if (recipe.requiresWorkbench && !ValidateWorkbenchPlacement(placePosition, out string workbenchWarning))
        {
            BuildingUI.Instance?.ShowWarning(workbenchWarning);
            return;
        }

        if (!IsInsideBuildableArea(placePosition))
        {
            BuildingUI.Instance?.ShowWarning("超出建造范围");
            return;
        }

        if (!CanPlaceAtPosition(placePosition))
        {
            BuildingUI.Instance?.ShowWarning("当前位置无法建造");
            return;
        }

        // 命令模式：建造操作封装为可撤销命令
        var cmd = new PlaceBuildingCommand(this, recipe, placePosition);
        CommandInvoker.Execute(cmd);

        BuildingUI.Instance?.HideWarning();
    }

    // 由 PlaceBuildingCommand 调用：扣费并生成建筑实体。
    // 扣费在 Instantiate 之前；若后续任一步抛异常，则退还材料并销毁已生成物体，避免背包与场景不一致。
    public GameObject CommitPlacement(BuildingRecipeSO recipe, Vector3 placePosition)
    {
        if (recipe == null || recipe.buildingPrefab == null)
            return null;

        RemoveMaterials(recipe);

        GameObject placedObject = null;
        try
        {
            placedObject = Instantiate(recipe.buildingPrefab, placePosition, Quaternion.identity);
            ApplyEntitySortingLayer(placedObject);

            PlacedBuilding placedBuilding = placedObject.GetComponent<PlacedBuilding>();
            if (placedBuilding == null)
                placedBuilding = placedObject.AddComponent<PlacedBuilding>();

            placedBuilding.Initialize(recipe, true);
            GameEvents.RaiseBuildingPlaced(recipe);
            return placedObject;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[BuildingSystem] CommitPlacement 失败，已退回材料: " + ex.Message);
            RefundMaterials(recipe);
            if (placedObject != null)
                Destroy(placedObject);
            return null;
        }
    }

    // 由 PlaceBuildingCommand.Undo 调用：销毁实体并退回材料
    public bool UndoPlacement(GameObject placedInstance, BuildingRecipeSO recipe)
    {
        if (recipe == null)
            return false;

        if (placedInstance != null)
            UnityEngine.Object.Destroy(placedInstance);

        RefundMaterials(recipe);
        return true;
    }

    // 撤销时将配方消耗的材料退还背包
    private void RefundMaterials(BuildingRecipeSO recipe)
    {
        if (_playerInventory == null || recipe == null || recipe.costs == null)
            return;

        for (int i = 0; i < recipe.costs.Length; i++)
        {
            BuildingCostData cost = recipe.costs[i];
            if (cost == null || cost.amount <= 0)
                continue;

            _playerInventory.AddItem(cost.itemType, cost.amount);
        }
    }

    // 鼠标世界坐标，开启网格吸附时对齐到网格
    private Vector3 GetCurrentPlacePosition()
    {
        Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        return snapToGrid ? GetSnappedPosition(mouseWorldPos) : mouseWorldPos;
    }

    // 将世界坐标吸附到最近的网格点
    private Vector3 GetSnappedPosition(Vector3 worldPosition)
    {
        float snappedX = Mathf.Round(worldPosition.x / gridSize) * gridSize;
        float snappedY = Mathf.Round(worldPosition.y / gridSize) * gridSize;
        return new Vector3(snappedX, snappedY, 0f);
    }

    // 检查目标位置有没有阻挡碰撞体
    private bool CanPlaceAtPosition(Vector3 position)
    {
        Collider2D hit = Physics2D.OverlapCircle(position, checkRadius, blockingLayerMask);
        return hit == null;
    }

    // 取消放置：清理预览物体，关闭建造 UI
    private void CancelPlacement()
    {
        _isPlacing = false;

        if (_previewObject != null)
            Destroy(_previewObject);

        _previewObject = null;
        _previewSpriteRenderer = null;

        BuildingUI.Instance?.ShowBuildingUI(false);
    }

    // 刷新建造 UI 的蓝图槽和费用文本
    private void RefreshBuildingUI()
    {
        if (BuildingUI.Instance == null || recipes == null || recipes.Length == 0)
            return;

        BuildingUI.Instance.UpdateBlueprintUI(recipes, _selectedRecipeIndex);
    }

    // 检查目标位置是否在允许建造的区域内（Collider2D 限制区域）
    private bool IsInsideBuildableArea(Vector3 position)
    {
        if (buildableArea == null)
            return true;

        return buildableArea.OverlapPoint(position);
    }

    // 将对象及所有子物体的 SpriteRenderer 设为 Entity 排序层，确保建筑不被地表遮挡
    private static void ApplyEntitySortingLayer(GameObject obj)
    {
        if (obj == null) return;
        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in renderers)
            sr.sortingLayerName = "Entity";
    }

    // 是否存在「同一台」工作台：角色在其半径内，且落点也在该台半径内（避免人站台边却把建筑摆到远处）
    private bool IsPlayerAndPlacementNearSameWorkbench(Vector3 placePosition)
    {
        for (int i = 0; i < Workbench.RegisteredInstances.Count; i++)
        {
            Workbench workbench = Workbench.RegisteredInstances[i];
            if (workbench == null)
                continue;

            Vector2 wbPos = workbench.transform.position;
            if (Vector2.Distance(transform.position, wbPos) > workbenchRange)
                continue;
            if (Vector2.Distance(placePosition, wbPos) > workbenchRange)
                continue;

            return true;
        }

        return false;
    }

    // 需要工作台时的完整校验，并区分提示文案（先站近台子 / 再把落点摆近同一台）
    private bool ValidateWorkbenchPlacement(Vector3 placePosition, out string message)
    {
        message = null;

        bool playerNearAnyWorkbench = false;
        for (int i = 0; i < Workbench.RegisteredInstances.Count; i++)
        {
            Workbench workbench = Workbench.RegisteredInstances[i];
            if (workbench == null)
                continue;
            if (Vector2.Distance(transform.position, workbench.transform.position) <= workbenchRange)
            {
                playerNearAnyWorkbench = true;
                break;
            }
        }

        if (!playerNearAnyWorkbench)
        {
            message = "请先靠近工作台";
            return false;
        }

        if (!IsPlayerAndPlacementNearSameWorkbench(placePosition))
        {
            message = "请将建筑摆在工作台附近（与角色同一工作台范围内）";
            return false;
        }

        return true;
    }
}
