namespace Nagule.Graphics.Backend.OpenTK;

using System.Reactive.Disposables;

using Nagule.Graphics;

public class MeshManager : ResourceManagerBase<Mesh>
{
    private class InitializeCommand : Command<InitializeCommand, RenderTarget>
    {
        public uint MeshId;
        public Mesh? Resource;
        public uint MaterialId;

        public override uint? Id => MeshId;

        public unsafe override void Execute(ICommandHost host)
        {
            ref var data = ref host.Acquire<MeshData>(MeshId);
            host.Acquire<MeshGroupDirty>();

            var buffers = data.BufferHandles;

            data.PrimitiveType = MeshHelper.Cast(Resource!.PrimitiveType);
            data.IndexCount = Resource.Indices!.Length;
            data.IsOccluder = Resource.IsOccluder;
            data.MaterialId = MaterialId;

            data.UniformBufferHandle = GL.GenBuffer();
            data.VertexArrayHandle = GL.GenVertexArray();
            data.CullingVertexArrayHandle = GL.GenVertexArray();
            data.CulledQueryHandle = GL.GenQuery();

            GL.GenBuffers(buffers.Raw);

            MeshHelper.InitializeUniformBuffer(in data, Resource!);

            // instancing

            GL.BindVertexArray(data.VertexArrayHandle);
            MeshHelper.InitializeDrawVertexArray(in data, Resource!);

            if (host.TryGet<MeshRenderState>(MeshId, out var state)
                    && state.InstanceCount != 0) {
                var capacity = state.InstanceCount;
                data.InstanceCapacity = capacity;
                MeshHelper.InitializeInstanceBuffer(ref data);

                var srcSpan = state.Instances.AsSpan(0, state.InstanceCount);
                var dstSpan = new Span<MeshInstance>((void*)data.InstanceBufferPointer, capacity);
                srcSpan.CopyTo(dstSpan);
            }
            else {
                MeshHelper.InitializeInstanceBuffer(ref data);
            }
            MeshHelper.InitializeInstanceCulling(in data);

            GL.BindVertexArray(VertexArrayHandle.Zero);
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand, RenderTarget>
    {
        public uint MeshId;

        public override void Execute(ICommandHost host)
        {
            if (!host.Remove<MeshData>(MeshId, out var data)) {
                return;
            }
            host.Acquire<MeshGroupDirty>();

            GL.DeleteBuffer(data.UniformBufferHandle);
            GL.DeleteBuffers(data.BufferHandles.Raw);
            GL.DeleteVertexArray(data.VertexArrayHandle);
            GL.DeleteQuery(data.CulledQueryHandle);
        }
    }

    protected unsafe override void Initialize(
        IContext context, uint id, Mesh resource, Mesh? prevResource)
    {
        if (prevResource != null) {
            Uninitialize(context, id, prevResource);
        }

        Mesh.GetProps(context, id).Set(resource);

        var cmd = InitializeCommand.Create();
        cmd.MeshId = id;
        cmd.Resource = resource;
        cmd.MaterialId = context.GetResourceLibrary().Reference(id, resource.Material);

        context.SendCommandBatched(cmd);
    }

    protected override IDisposable? Subscribe(IContext context, uint id, Mesh resource)
    {
        ref var props = ref Mesh.GetProps(context, id);
        var resLib = context.GetResourceLibrary();

        return new CompositeDisposable(
            props.Material.Modified.Subscribe(tuple => {
                var (prevMaterial, material) = tuple;

                if (prevMaterial != null) {
                    resLib.Unreference(id, prevMaterial);
                }
                var matId = resLib.Reference(id, material);

                context.SendCommandBatched<RenderTarget>(Command.Do(host => {
                    ref var data = ref host.Require<MeshData>(id);
                    data.MaterialId = matId;
                }));
            }),

            props.IsOccluder.SubscribeCommand<bool, RenderTarget>(
                context, (host, value) => {
                    ref var data = ref host.Require<MeshData>(id);
                    if (data.IsOccluder != value) {
                        data.IsOccluder = value;
                        host.Acquire<MeshGroupDirty>();
                    }
                })
        );
    }

    protected override void Uninitialize(IContext context, uint id, Mesh resource)
    {
        context.GetResourceLibrary().UnreferenceAll(id);

        var cmd = UninitializeCommand.Create();
        cmd.MeshId = id;
        context.SendCommandBatched(cmd);
    }
}