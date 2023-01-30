namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Runtime.InteropServices;

using CommunityToolkit.HighPerformance.Buffers;

using Aeco;
using Aeco.Reactive;

using Nagule.Graphics;

public class MeshRenderableBufferUpdator : Layer, IEngineUpdateListener
{
    private record struct DirtyMeshRenderableEntry(in Guid Id, in Matrix4x4 World)
    {
        public struct IdComparable : IComparable<DirtyMeshRenderableEntry>
        {
            public Guid Id;
            public IdComparable(Guid id) { Id = id; }

            public int CompareTo(DirtyMeshRenderableEntry other)
                => Id.CompareTo(other.Id);
        }
    }

    private class UpdateCommand : Command<UpdateCommand, RenderTarget>
    {
        public MemoryOwner<DirtyMeshRenderableEntry>? DirtyMeshRenderables;

        public override Guid? Id => Guid.Empty;

        private Dictionary<Guid, DirtyMeshRenderableEntry> _mergedEntries = new();

        public unsafe override void Execute(ICommandContext context)
        {
            foreach (ref var entry in DirtyMeshRenderables!.Span) {
                UpdateEntry(context, in entry);
            }
            if (_mergedEntries.Count != 0) {
                foreach (var entry in _mergedEntries.Values) {
                    UpdateEntry(context, in entry);
                }
            }
        }

        private unsafe void UpdateEntry(ICommandContext context, in DirtyMeshRenderableEntry entry)
        {
            ref readonly var data = ref context.Inspect<MeshRenderableData>(entry.Id);
            var world = entry.World;

            foreach (var (meshId, index) in data.Entries) {
                ref MeshData meshData = ref context.Require<MeshData>(meshId);
                ref var meshState = ref context.Require<MeshRenderState>(meshId);

                meshState.Instances[index].ObjectToWorld = world;
                ((MeshInstance*)meshData.InstanceBufferPointer + index)->ObjectToWorld = world;
            }
        }

        public override void Merge(ICommand other)
        {
            if (other is not UpdateCommand otherCmd) {
                return;
            }

            var span = DirtyMeshRenderables!.Span;
            SpanHelper.MergeOrdered(
                span, otherCmd.DirtyMeshRenderables!.Span,
                (in DirtyMeshRenderableEntry e) => e.Id,
                (in Guid id, in DirtyMeshRenderableEntry e) =>
                    CollectionsMarshal.GetValueRefOrAddDefault(_mergedEntries, id, out bool _) = e);
            
            foreach (var (id, entry) in otherCmd._mergedEntries) {
                if (span.BinarySearch(new DirtyMeshRenderableEntry.IdComparable(id)) < 0) {
                    _mergedEntries[id] = entry;
                }
            }
        }

        public override void Dispose()
        {
            DirtyMeshRenderables!.Dispose();
            DirtyMeshRenderables = null;
            _mergedEntries.Clear();
            base.Dispose();
        }
    }
    
    private Group<Resource<MeshRenderable>, TransformDirty> _dirtyRenderableGroup = new();

    public unsafe void OnEngineUpdate(IContext context)
    {
        _dirtyRenderableGroup.Query(context);

        int count = _dirtyRenderableGroup.Count;
        if (count == 0) {
            return;
        }

        var dirtyRenderables = MemoryOwner<DirtyMeshRenderableEntry>.Allocate(count);
        var dirtyRenderableSpan = dirtyRenderables.Span;

        int n = 0;
        foreach (var id in _dirtyRenderableGroup) {
            ref readonly var transform = ref context.Inspect<Transform>(id);
            ref var entry = ref dirtyRenderableSpan[n];
            entry.Id = id;
            entry.World = transform.World;
            ++n;
        }

        var cmd = UpdateCommand.Create();
        cmd.DirtyMeshRenderables = dirtyRenderables;
        context.SendCommandBatched(cmd);
    }
}
