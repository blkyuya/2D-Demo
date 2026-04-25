// 可交互对象接口：树木、石头、箱子、营火、工作台等统一实现此接口。
// 玩家的 PlayerInteraction 只依赖接口，不关心具体类型，便于后续扩展新交互对象。
public interface IInteractable
{
    void Interact();
    string GetInteractionText();
}
