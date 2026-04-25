// 可采集对象接口：资源节点（树木、石头等）实现此接口。
// PlayerInteraction 通过 is 模式匹配判断是否需要走采集动画流程，
// 普通交互（营火、箱子等）直接调用 Interact()，两者由此接口区分。
public interface IHarvestable
{
    bool CanHarvest();
}
