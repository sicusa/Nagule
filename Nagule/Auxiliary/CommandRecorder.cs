namespace Nagule;

using System.Runtime.InteropServices;

public sealed class CommandRecorder
{
    public string ProfileCategory { get; private set; }
    public int Count => _commands.Count;

    private List<(int, ICommand)> _commands = new();
    private Dictionary<(Type, Guid), int> _commandMap = new();
    private LinkedList<IDeferrableCommand> _deferredCommands = new();

    public CommandRecorder(string profileCategory)
    {
        ProfileCategory = profileCategory;
    }

    public void Record(ICommand command)
    {
        if (command is BatchedCommand batchedCmd) {
            batchedCmd.Commands.ForEach(Record);
            batchedCmd.Dispose();
            return;
        }

        var commandId = command.Id;
        if (commandId == null) {
            _commands.Add((_commands.Count, command));
            return;
        }

        var key = (command.GetType(), commandId.Value);
        if (_commandMap.TryGetValue(key, out var index)) {
            var tuple = _commands[index];
            command.Merge(tuple.Item2);
            tuple.Item2.Dispose();
            tuple.Item2 = command;
            _commands[index] = tuple;
        }
        else {
            index = _commands.Count;
            _commands.Add((index, command));
            _commandMap.Add(key, index);
        }
    }

    public void Execute(ICommandHost host)
    {
        var deferredCmdNode = _deferredCommands.First;
        while (deferredCmdNode != null) {
            var cmd = deferredCmdNode.Value;
            var nextNode = deferredCmdNode.Next;

            if (cmd.ShouldExecute(host)) {
                using (host.Profile(ProfileCategory, cmd)) {
                    cmd.SafeExecuteAndDispose(host);
                }
                _deferredCommands.Remove(deferredCmdNode);
            }

            deferredCmdNode = nextNode;
        }

        _commands.Sort(Command.IndexedComparePriority);

        foreach (var (_, cmd) in CollectionsMarshal.AsSpan(_commands)) {
            if (cmd is IDeferrableCommand delayedCmd && !delayedCmd.ShouldExecute(host)) {
                _deferredCommands.AddLast(delayedCmd);
                continue;
            }
            using (host.Profile(ProfileCategory, cmd)) {
                cmd.SafeExecuteAndDispose(host);
            }
        }

        Clear();
    }

    public void Clear()
    {
        _commands.Clear();
        _commandMap.Clear();
    }
}