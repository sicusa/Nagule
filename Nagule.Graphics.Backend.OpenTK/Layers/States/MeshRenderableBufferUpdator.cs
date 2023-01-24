namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using Aeco;
using Aeco.Reactive;

using Nagule.Graphics;

public class MeshRenderableBufferUpdator : Layer, ILoadListener, IEngineUpdateListener
{
    private record struct DirtyMeshRenderableEntry(in Guid Id, in Matrix4x4 World);

    private class UpdateCommand : Command<UpdateCommand, RenderTarget>
    {
        public readonly List<DirtyMeshRenderableEntry> DirtyMeshRenderables = new();

        public override Guid? Id => Guid.Empty;

        public unsafe override void Execute(ICommandContext context)
        {
            var span = CollectionsMarshal.AsSpan(DirtyMeshRenderables);

            foreach (ref var tuple in span) {
                ref readonly var data = ref context.Inspect<MeshRenderableData>(tuple.Id);
                var transposedWorld = Matrix4x4.Transpose(tuple.World);

                foreach (var (meshId, index) in data.Entries) {
                    ref MeshData meshData = ref context.Require<MeshData>(meshId);
                    ref var meshState = ref context.Require<MeshRenderState>(meshId);
                    meshState.Instances[index].ObjectToWorld = transposedWorld;
                    ((MeshInstance*)meshData.InstanceBufferPointer + index)->ObjectToWorld = transposedWorld;
                }
            }
        }

        public override void Merge(ICommand other)
        {
            if (other is not UpdateCommand converted) {
                return;
            }
            OrderedListHelper.Union(DirtyMeshRenderables, converted.DirtyMeshRenderables,
                (in DirtyMeshRenderableEntry e1, in DirtyMeshRenderableEntry e2) =>
                    e1.Id.CompareTo(e2.Id));
        }

        public override void Dispose()
        {
            base.Dispose();
            DirtyMeshRenderables.Clear();
        }
    }
    
    private Group<Resource<MeshRenderable>> _renderables = new();

    [AllowNull] private IEnumerable<Guid> _dirtyRenderables;

    public void OnLoad(IContext context)
    {
        _dirtyRenderables = QueryUtil.Intersect(_renderables, context.DirtyTransformIds);
    }

    public unsafe void OnEngineUpdate(IContext context)
    {
        _renderables.Query(context);

        if (_dirtyRenderables.Any()) {
            var cmd = UpdateCommand.Create();

            foreach (var id in _dirtyRenderables) {
                ref readonly var transform = ref context.Inspect<Transform>(id);
                cmd.DirtyMeshRenderables.Add(new(id, transform.World));
            }

            context.SendCommandBatched(cmd);
        }
    }
}
