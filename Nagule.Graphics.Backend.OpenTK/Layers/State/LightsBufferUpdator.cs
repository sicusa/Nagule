namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using CommunityToolkit.HighPerformance.Buffers;

using Aeco;
using Aeco.Reactive;

using Nagule.Graphics;

public class LightsBufferUpdator : Layer, IEngineUpdateListener
{
    private struct DirtyLightEntry
    {
        public uint Id;
        public Vector3 Position;
        public Vector3 Direction;

        public struct IdComparable : IComparable<DirtyLightEntry>
        {
            public uint Id;
            public IdComparable(uint id) { Id = id; }

            public int CompareTo(DirtyLightEntry other)
                => Id.CompareTo(other.Id);
        }
    }

    private class UpdateCommand : Command<UpdateCommand, RenderTarget>
    {
        public MemoryOwner<DirtyLightEntry>? DirtyLights;

        public override uint? Id => 0;

        private List<MemoryOwner<DirtyLightEntry>> _mergedMems = new();

        private static Stack<HashSet<uint>> s_uniqueSetPool = new();

        public unsafe override void Execute(ICommandHost host)
        {
            ref var buffer = ref host.Require<LightsBuffer>();

            if (_mergedMems.Count == 0) {
                foreach (ref var entry in DirtyLights!.Span) {
                    UpdateEntry(host, in buffer, in entry);
                }
            }
            else{
                var updatedIds = s_uniqueSetPool.TryPop(out var set) ? set : new();

                foreach (ref var entry in DirtyLights!.Span) {
                    updatedIds.Add(entry.Id);
                    UpdateEntry(host, in buffer, in entry);
                }

                try {
                    foreach (var mem in _mergedMems) {
                        foreach (ref var entry in mem.Span) {
                            if (!updatedIds.Add(entry.Id)) {
                                continue;
                            }
                            UpdateEntry(host, in buffer, in entry);
                        }
                    }
                }
                finally {
                    s_uniqueSetPool.Push(updatedIds);
                }
            }
        }

        private unsafe void UpdateEntry(ICommandHost host, in LightsBuffer buffer, in DirtyLightEntry entry)
        {
            ref var data = ref host.RequireOrNullRef<LightData>(entry.Id);
            if (Unsafe.IsNullRef(ref data)) { return; }

            ref var pars = ref buffer.Parameters[data.Index];
            pars.Position = entry.Position;
            pars.Direction = entry.Direction;

            ((LightParameters*)buffer.Pointer)[data.Index] = pars;
        }

        public override void Merge(ICommand other)
        {
            if (other is not UpdateCommand otherCmd) {
                return;
            }
            _mergedMems.Add(otherCmd.DirtyLights!);
            otherCmd.DirtyLights = null;
            _mergedMems.AddRange(otherCmd._mergedMems);
        }

        public override void Dispose()
        {
            DirtyLights?.Dispose();
            DirtyLights = null;
            _mergedMems.Clear();
            base.Dispose();
        }
    }

    private Group<Resource<Light>, TransformDirty> _dirtyLightGroup = new();

    public unsafe void OnEngineUpdate(IContext context)
    {
        _dirtyLightGroup.Query(context);

        int count = _dirtyLightGroup.Count;
        if (count == 0) { return; }

        var dirtyLights = MemoryOwner<DirtyLightEntry>.Allocate(count);
        var dirtyLightsSpan = dirtyLights.Span;

        int n = 0;
        foreach (var id in _dirtyLightGroup) {
            ref readonly var transform = ref context.Inspect<Transform>(id);
            ref var entry = ref dirtyLightsSpan[n];
            entry.Id = id;
            entry.Position = transform.Position;
            entry.Direction = transform.Forward;
            ++n;
        }

        var cmd = UpdateCommand.Create();
        cmd.DirtyLights = dirtyLights;
        context.SendCommandBatched(cmd);
    }
}