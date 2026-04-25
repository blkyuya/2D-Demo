using UnityEngine;

// 建造命令：封装「扣材料 + 生成实体 + 广播事件」，支持撤销（销毁建筑 + 退回材料）。
// 与 BuildingSystem 协作，保持原有建造规则不变。
public sealed class PlaceBuildingCommand : IGameCommand
{
    private readonly BuildingSystem _system;
    private readonly BuildingRecipeSO _recipe;
    private readonly Vector3 _worldPosition;

    // 成功放置后的实例，撤销时用于销毁
    private GameObject _placedInstance;

    public PlaceBuildingCommand(BuildingSystem system, BuildingRecipeSO recipe, Vector3 worldPosition)
    {
        _system = system;
        _recipe = recipe;
        _worldPosition = worldPosition;
    }

    // 执行：委托 BuildingSystem 扣费并生成实体
    public bool Execute()
    {
        if (_system == null || _recipe == null)
            return false;

        _placedInstance = _system.CommitPlacement(_recipe, _worldPosition);
        return _placedInstance != null;
    }

    // 撤销：销毁实体并退还材料
    public bool Undo()
    {
        if (_system == null || _recipe == null)
            return false;

        return _system.UndoPlacement(_placedInstance, _recipe);
    }
}
