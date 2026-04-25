// 命令模式接口：将「建造 / 合成 / 使用物品」等操作封装为对象，
// 便于后续扩展撤销、重做、操作日志、联机回放等功能。
public interface IGameCommand
{
    // 执行命令，成功返回 true
    bool Execute();

    // 撤销命令，成功返回 true（不支持撤销时返回 false）
    bool Undo();
}
