namespace Nagule;

using System.Runtime.InteropServices;

public sealed class CommandRecorder
{
    public int Count => _commandList.Count;

    private List<(int, ICommand)> _commandList = new();
    private Dictionary<(Type, Guid), int> _commandMap = new();

    public void Record(ICommand command)
    {
        if (command is BatchedCommand batchedCmd) {
            batchedCmd.Commands.ForEach(Record);
            batchedCmd.Dispose();
            return;
        }

        var commandId = command.Id;
        if (commandId == null) {
            _commandList.Add((_commandList.Count, command));
            return;
        }

        var key = (command.GetType(), commandId.Value);
        if (_commandMap.TryGetValue(key, out var index)) {
            var tuple = _commandList[index];
            command.Merge(tuple.Item2);
            tuple.Item2.Dispose();
            tuple.Item2 = command;
            _commandList[index] = tuple;
        }
        else {
            index = _commandList.Count;
            _commandList.Add((index, command));
            _commandMap.Add(key, index);
        }
    }

    public void Execute(Action<ICommand> action)
    {
        _commandList.Sort(Command.IndexedComparePriority);

        foreach (ref var tuple in CollectionsMarshal.AsSpan(_commandList)) {
            action(tuple.Item2);
        }

        Clear();
    }

    public void Execute<TArg>(TArg arg, Action<TArg, ICommand> action)
    {
        _commandList.Sort(Command.IndexedComparePriority);

        foreach (ref var tuple in CollectionsMarshal.AsSpan(_commandList)) {
            action(arg, tuple.Item2);
        }

        Clear();
    }

    public void Clear()
    {
        _commandList.Clear();
        _commandMap.Clear();
    }
}