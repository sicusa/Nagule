namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Diagnostics.CodeAnalysis;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Aeco;
using Aeco.Reactive;

using Nagule.Graphics;

public class MeshRenderableBufferUpdator : VirtualLayer, ILoadListener, IEngineUpdateListener
{
    private class UpdateVariantBufferCommand : Command<UpdateVariantBufferCommand>
    {
        public Guid MeshRenderableId;
        public Matrix4x4 World;

        public unsafe override void Execute(IContext context)
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

    private class DeleteVariantBufferCommand : Command<DeleteVariantBufferCommand>
    {
        public Guid MeshRenderableId;

        public override void Execute(IContext context)
        {
            ref var data = ref context.Require<MeshRenderableData>(MeshRenderableId);
            GL.DeleteBuffer(data.VariantBufferHandle);

            data.VariantBufferHandle = BufferHandle.Zero;
            data.VariantBufferPointer = IntPtr.Zero;
        }
    }

    private class UpdateCommand : Command<UpdateCommand>
    {
        public readonly List<(Guid, Matrix4x4)> DirtyMeshRenderableIds = new();

        public unsafe override void Execute(IContext context)
        {
            foreach (var (id, world) in DirtyMeshRenderableIds) {
                ref readonly var data = ref context.Inspect<MeshRenderableData>(id);
                var transposedWorld = Matrix4x4.Transpose(world);

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

        public override void Dispose()
        {
            base.Dispose();
            DirtyMeshRenderableIds.Clear();
        }
    }
    
    private Group<MeshRenderable> _renderables = new();
    private Query<Modified<MeshRenderable>, MeshRenderable> _modifiedRenderableQuery = new();
    private Group<MeshRenderable, Destroy> _destroyedRenderableGroup = new();

    [AllowNull] private IEnumerable<Guid> _dirtyRenderables;

    public void OnLoad(IContext context)
    {
        _dirtyRenderables = QueryUtil.Intersect(_renderables, context.DirtyTransformIds);
    }

    public unsafe void OnEngineUpdate(IContext context)
    {
        foreach (var id in _modifiedRenderableQuery.Query(context)) {
            ref readonly var renderable = ref context.Inspect<MeshRenderable>(id);
            bool hasVariant = false;

            foreach (var (_, mode) in renderable.Meshes) {
                if (mode == MeshRenderMode.Variant) {
                    hasVariant = true;
                    break;
                }
            }

            if (hasVariant) {
                context.Acquire<HasVariantBuffer>(id);
                var cmd = UpdateVariantBufferCommand.Create();
                cmd.MeshRenderableId = id;
                cmd.World = context.Inspect<Transform>(id).World;
                context.SendCommandBatched<RenderTarget>(cmd);
            }
            else if (context.Remove<HasVariantBuffer>(id)) {
                var cmd = DeleteVariantBufferCommand.Create();
                cmd.MeshRenderableId = id;
                context.SendCommandBatched<RenderTarget>(cmd);
            }
        }

        foreach (var id in _destroyedRenderableGroup.Query(context)) {
            if (context.Remove<HasVariantBuffer>(id)) {
                var cmd = DeleteVariantBufferCommand.Create();
                cmd.MeshRenderableId = id;
                context.SendCommandBatched<RenderTarget>(cmd);
            }
        }

        _renderables.Query(context);

        if (_dirtyRenderables.Any()) {
            var cmd = UpdateCommand.Create();
            var ids = cmd.DirtyMeshRenderableIds;

            foreach (var id in _dirtyRenderables) {
                ref readonly var transform = ref context.Inspect<Transform>(id);
                ids.Add((id, transform.World));
            }

            context.SendCommandBatched<RenderTarget>(cmd);
        }
    }
}
