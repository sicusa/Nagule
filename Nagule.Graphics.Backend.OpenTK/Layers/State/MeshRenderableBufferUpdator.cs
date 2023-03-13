namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;

using CommunityToolkit.HighPerformance.Buffers;

using Aeco;
using Aeco.Reactive;

using Nagule.Graphics;

public class MeshRenderableBufferUpdator : Layer, IEngineUpdateListener
{
    private struct DirtyMeshRenderableEntry
    {
        public uint Id;
        public Matrix4x4 World;

        public struct IdComparable : IComparable<DirtyMeshRenderableEntry>
        {
            public uint Id;
            public IdComparable(uint id) { Id = id; }

            public int CompareTo(DirtyMeshRenderableEntry other)
                => Id.CompareTo(other.Id);
        }
    }

    private class UpdateCommand : Command<UpdateCommand, RenderTarget>
{
        public MemoryOwner<DirtyMeshRenderableEntry>? DirtyMeshRenderables;

        public override uint? Id => 0;

        private List<MemoryOwner<DirtyMeshRenderableEntry>> _mergedMems = new();

        private static Stack<HashSet<uint>> s_uniqueSetPool = new();

        public unsafe override void Execute(ICommandHost host)
        {
            if (_mergedMems.Count == 0) {
                foreach (ref var entry in DirtyMeshRenderables!.Span) {
                    UpdateEntry(host, in entry);
                }
            }
            else {
                var updatedIds = s_uniqueSetPool.TryPop(out var set) ? set : new();

                foreach (ref var entry in DirtyMeshRenderables!.Span) {
                    updatedIds.Add(entry.Id);
                    UpdateEntry(host, in entry);
                }

                try {
                    foreach (var mem in _mergedMems) {
                        foreach (ref var entry in mem.Span) {
                            if (!updatedIds.Add(entry.Id)) {
                                continue;
                            }
                            UpdateEntry(host, in entry);
                        }
                    }
                }
                finally {
                    s_uniqueSetPool.Push(updatedIds);
                }
            }
        }

        private unsafe void UpdateEntry(ICommandHost host, in DirtyMeshRenderableEntry entry)
        {
            ref readonly var data = ref host.Inspect<MeshRenderableData>(entry.Id);
            var world = entry.World;

            foreach (var (meshId, index) in data.Entries) {
                ref MeshData meshData = ref host.Require<MeshData>(meshId);
                ref var meshState = ref host.Require<MeshRenderState>(meshId);

                meshState.Instances[index].ObjectToWorld = world;
                ((MeshInstance*)meshData.InstanceBufferPointer)[index].ObjectToWorld = world;
            }
        }

        public override void Merge(ICommand other)
        {
            if (other is not UpdateCommand otherCmd) {
                return;
            }
            _mergedMems.Add(otherCmd.DirtyMeshRenderables!);
            otherCmd.DirtyMeshRenderables = null;
            _mergedMems.AddRange(otherCmd._mergedMems);
        }

        public override void Dispose()
        {
            DirtyMeshRenderables?.Dispose();
            DirtyMeshRenderables = null;
            _mergedMems.Clear();
            base.Dispose();
        }
    }
    
    private Group<Resource<MeshRenderable>, TransformDirty> _dirtyRenderableGroup = new();

    public unsafe void OnEngineUpdate(IContext context)
    {
        _dirtyRenderableGroup.Query(context);

        int count = _dirtyRenderableGroup.Count;
        if (count == 0) { return; }

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
