namespace Nagule;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CommunityToolkit.HighPerformance;
using Microsoft.Extensions.Logging;
using Sia;

using TaskFunc = Func<object?, bool>;
using DelayQueue = Queue<ParallelFramer.TaskEntry>;

public abstract class ParallelFramer : Frame
{
    public record struct TaskEntry(object? Argument, TaskFunc Task);

    [AllowNull] protected ILogger Logger { get; private set; }

    private readonly ThreadLocal<SwappingQueue<(EntityRef?, TaskEntry)>> _queue =
        new(() => new(), trackAllValues: true);

    private readonly LinkedList<TaskEntry> _globalDelayedTasks = new();
    private readonly List<(TaskEntry, LinkedListNode<TaskEntry>)> _globalDelayedTasksList = [];
    private bool _globalDelayedTasksDirty = false;

    private readonly Dictionary<EntityRef, DelayQueue> _delayQueues = [];
    private readonly List<EntityRef> _delayQueuesToRemove = [];

    private readonly Stack<DelayQueue> _delayQueuePool = new();
    private static readonly TaskFunc s_terminateTask = _ => false;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        Logger = CreateLogger(world, world.AcquireAddon<LogLibrary>());
    }

    protected abstract ILogger CreateLogger(World world, LogLibrary logLib);

    private DelayQueue CreateDeleyQueue()
        => _delayQueuePool.TryPop(out var pooled) ? pooled : new();
    
    private void ReleaseDeleyQueue(DelayQueue queue)
        => _delayQueuePool.Push(queue);

    public void Start<TArg>(TArg argument, Func<TArg, bool> action)
        where TArg : class
        => Start(argument, Unsafe.As<TaskFunc>(action));

    public void Start(Func<bool> action)
    {
        _queue.Value!.Add((null, new(action,
            static action => Unsafe.As<Func<bool>>(action)!.Invoke())));
    }

    public void Start(object argument, TaskFunc action)
    {
        _queue.Value!.Add((null, new(argument, action)));
    }

    public void Enqueue<TArg>(in EntityRef entity, TArg argument, Func<TArg, bool> action)
        where TArg : class
        => Enqueue(entity, argument, Unsafe.As<TaskFunc>(action));

    public void Enqueue(in EntityRef entity, object argument, TaskFunc action)
    {
        _queue.Value!.Add((entity, new(argument, action)));
    }

    public void Enqueue(in EntityRef entity, Func<bool> action)
    {
        _queue.Value!.Add((entity, new(action,
            static action => Unsafe.As<Func<bool>>(action)!.Invoke())));
    }

    public void Terminate(in EntityRef entity)
    {
        _queue.Value!.Add((entity, new(null!, s_terminateTask)));
    }

    protected override void OnTick()
    {
        foreach (var (entry, node) in _globalDelayedTasksList.AsSpan()) {
            if (RunTaskSafely(entry)) {
                _globalDelayedTasks.Remove(node);
                _globalDelayedTasksDirty = true;
            }
        }

        if (_globalDelayedTasksDirty) {
            _globalDelayedTasksDirty = false;
            _globalDelayedTasksList.Clear();

            var node = _globalDelayedTasks.First;
            while (node != null) {
                ref var taskEntry = ref node.ValueRef;
                _globalDelayedTasksList.Add((taskEntry, node));
                node = node.Next;
            }
        }

        foreach (var (entity, queue) in _delayQueues) {
            while (queue.TryPeek(out var entry)) {
                if (!RunTaskSafely(entry)) { break; }
                queue.Dequeue();
            }
            if (queue.Count == 0) {
                _delayQueuesToRemove.Add(entity);
            }
        }

        if (_delayQueuesToRemove.Count != 0) {
            foreach (var queueEntity in _delayQueuesToRemove) {
                _delayQueues.Remove(queueEntity, out var queue);
                ReleaseDeleyQueue(queue!);
            }
            _delayQueuesToRemove.Clear();
        }

        foreach (var seq in _queue.Values) {
            var queue = seq.Swap();
            foreach (var (entityRaw, entry) in queue.AsSpan()) {
                if (entityRaw is not EntityRef entity) {
                    if (!RunTaskSafely(entry)) {
                        var node = _globalDelayedTasks.AddLast(entry);
                        _globalDelayedTasksList.Add((entry, node));
                    }
                    continue;
                }
                if (entry.Task == s_terminateTask) {
                    _delayQueues.Remove(entity);
                    continue;
                }
                if (_delayQueues.TryGetValue(entity, out var delayQueue)) {
                    delayQueue.Enqueue(entry);
                    continue;
                }
                if (!RunTaskSafely(entry)) {
                    delayQueue = CreateDeleyQueue();
                    delayQueue.Enqueue(entry);
                    _delayQueues.Add(entity, delayQueue);
                }
            }
            queue.Clear();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool RunTaskSafely(in TaskEntry entry)
    {
        try {
            return entry.Task(entry.Argument);
        }
        catch (Exception e) {
            Logger.LogError("Unhandled exception: {Exception}", e);
            return true;
        }
    }
}