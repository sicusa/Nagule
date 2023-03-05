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
        public Guid Id;
        public Vector3 Position;
        public Vector3 Direction;

        public struct IdComparable : IComparable<DirtyLightEntry>
        {
            public Guid Id;
            public IdComparable(Guid id) { Id = id; }

            public int CompareTo(DirtyLightEntry other)
                => Id.CompareTo(other.Id);
        }
    }

    private class UpdateCommand : Command<UpdateCommand, RenderTarget>
    {
        public MemoryOwner<DirtyLightEntry>? DirtyLights;

        public override Guid? Id => Guid.Empty;

        private Dictionary<Guid, DirtyLightEntry> _mergedEntries = new();

        public unsafe override void Execute(ICommandHost host)
        {
            ref var buffer = ref host.Require<LightsBuffer>();
            foreach (ref var entry in DirtyLights!.Span) {
                UpdateEntry(host, in buffer, in entry);
            }
            if (_mergedEntries.Count != 0) {
                foreach (var entry in _mergedEntries.Values) {
                    UpdateEntry(host, in buffer, in entry);
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

            var span = DirtyLights!.Span;
            SpanHelper.MergeOrdered(
                span, otherCmd.DirtyLights!.Span,
                (in DirtyLightEntry e) => e.Id,
                (in Guid id, in DirtyLightEntry e) =>
                    CollectionsMarshal.GetValueRefOrAddDefault(_mergedEntries, id, out bool _) = e);

            foreach (var (id, entry) in otherCmd._mergedEntries) {
                if (span.BinarySearch(new DirtyLightEntry.IdComparable(id)) < 0) {
                    _mergedEntries[id] = entry;
                }
            }
        }

        public override void Dispose()
        {
            DirtyLights!.Dispose();
            DirtyLights = null;
            _mergedEntries.Clear();
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