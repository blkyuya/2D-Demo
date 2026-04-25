using System.Collections.Generic;

// 命令调度器：维护撤销栈，供建造等系统调用。
// 用静态类实现，不需要在场景中挂载额外物体。
public static class CommandInvoker
{
    private static readonly Stack<IGameCommand> _undoStack = new Stack<IGameCommand>();

    // 最大撤销步数，防止无限堆叠占用内存
    public const int MaxUndoDepth = 32;

    // 执行命令；成功则压入撤销栈
    public static bool Execute(IGameCommand command)
    {
        if (command == null)
            return false;

        if (!command.Execute())
            return false;

        _undoStack.Push(command);
        return true;
    }

    // 撤销上一条命令
    public static bool UndoLast()
    {
        if (_undoStack.Count == 0)
            return false;

        IGameCommand cmd = _undoStack.Pop();
        return cmd != null && cmd.Undo();
    }

    // 清空撤销栈（读档、切场景时调用，避免旧命令引用已销毁的对象）
    public static void ClearUndoStack()
    {
        _undoStack.Clear();
    }
}
