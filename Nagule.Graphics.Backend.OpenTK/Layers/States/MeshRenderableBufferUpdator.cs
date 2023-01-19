namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Aeco;
using Aeco.Reactive;

using Nagule.Graphics;

public class MeshRenderableBufferUpdator : Layer, ILoadListener, IEngineUpdateListener
{
    private class UpdateVariantBufferCommand : Command<UpdateVariantBufferCommand, RenderTarget>
    {
        public Guid MeshRenderableId;
        public Matrix4x4 World;

        public override Guid? Id => MeshRenderableId;

        public unsafe override void Execute(ICommandContext context)
        {
            ref var data = ref context.Require<MeshRenderableData>(MeshRenderableId);

            if (data.VariantBufferHandle == BufferHandle.Zero) {
                data.VariantBufferHandle = GL.GenBuffer();
                GL.BindBuffer(BufferTargetARB.UniformBuffer, data.VariantBufferHandle);
                data.VariantBufferPointer = GLHelper.InitializeBuffer(
                    BufferTargetARB.UniformBuffer, MeshInstance.MemorySize + 4);
            }

            var ptr = (Matrix4x4*)data.VariantBufferPointer;
            *ptr = Matrix4x4.Transpose(World);
            *((bool*)(ptr + 1)) = true;
        }
    }

    private class DeleteVariantBufferCommand : Command<DeleteVariantBufferCommand, RenderTarget>
    {
        public Guid MeshRenderableId;

        public override void Execute(ICommandContext context)
        {
            ref var data = ref context.Require<MeshRenderableData>(MeshRenderableId);
            GL.DeleteBuffer(data.VariantBufferHandle);

            data.VariantBufferHandle = BufferHandle.Zero;
            data.VariantBufferPointer = IntPtr.Zero;
        }
    }

    private record struct DirtyMeshRenderableEntry(Guid Id, Matrix4x4 World);

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
                    if (index == -1) {
                        continue;
                    }
                    ref MeshData meshData = ref context.Require<MeshData>(meshId);
                    if (meshData.InstanceBufferPointer != IntPtr.Zero) {
                        ref var meshState = ref context.Require<MeshRenderState>(meshId);
                        meshState.Instances[index].ObjectToWorld = transposedWorld;
                        ((MeshInstance*)meshData.InstanceBufferPointer + index)->ObjectToWorld = transposedWorld;
                    }
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
    private Query<Modified<Resource<MeshRenderable>>, Resource<MeshRenderable>> _modifiedRenderableQuery = new();
    private Group<Resource<MeshRenderable>, Destroy> _destroyedRenderableGroup = new();

    [AllowNull] private IEnumerable<Guid> _dirtyRenderables;

    public void OnLoad(IContext context)
    {
        _dirtyRenderables = QueryUtil.Intersect(_renderables, context.DirtyTransformIds);
    }

    public unsafe void OnEngineUpdate(IContext context)
    {
        foreach (var id in _modifiedRenderableQuery.Query(context)) {
            var renderable = context.Inspect<Resource<MeshRenderable>>(id).Value;
            bool hasVariant = false;

            foreach (var (_, mode) in renderable.Meshes) {
                if (mode == MeshBufferMode.Variant) {
                    hasVariant = true;
                    break;
                }
            }

            if (hasVariant) {
                context.Acquire<HasVariantBuffer>(id);
                var cmd = UpdateVariantBufferCommand.Create();
                cmd.MeshRenderableId = id;
                cmd.World = context.Inspect<Transform>(id).World;
                context.SendCommandBatched(cmd);
            }
            else if (context.Remove<HasVariantBuffer>(id)) {
                var cmd = DeleteVariantBufferCommand.Create();
                cmd.MeshRenderableId = id;
                context.SendCommandBatched(cmd);
            }
        }

        foreach (var id in _destroyedRenderableGroup.Query(context)) {
            if (context.Remove<HasVariantBuffer>(id)) {
                var cmd = DeleteVariantBufferCommand.Create();
                cmd.MeshRenderableId = id;
                context.SendCommandBatched(cmd);
            }
        }

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
