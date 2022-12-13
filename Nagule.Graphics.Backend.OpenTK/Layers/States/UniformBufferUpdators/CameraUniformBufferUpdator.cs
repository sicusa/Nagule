namespace Nagule.Graphics.Backend.OpenTK.Graphics;

using System.Numerics;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using global::OpenTK.Graphics.OpenGL;

using Aeco;
using Aeco.Reactive;

using Nagule.Graphics;

public class CameraUniformBufferUpdator : ReactiveUpdatorBase<Camera>, ILoadListener, IRenderListener
{
    private Group<Camera> _g = new();
    [AllowNull] private IEnumerable<Guid> _dirtyCameraIds;

    private ConcurrentQueue<(bool, Guid)> _commandQueue = new();
    private ConcurrentQueue<Guid> _dirtyCameraQueue = new();

    public void OnLoad(IContext context)
    {
        _dirtyCameraIds = QueryUtil.Intersect(_g, context.DirtyTransformIds);
    }

    public unsafe override void OnEngineUpdate(IContext context, float deltaTime)
    {
        base.OnEngineUpdate(context, deltaTime);

        _g.Query(context);
        foreach (var id in _dirtyCameraIds) {
            _dirtyCameraQueue.Enqueue(id);
        }
    }

    protected unsafe override void Update(IContext context, Guid id)
        => _commandQueue.Enqueue((true, id));

    protected override void Release(IContext context, Guid id)
        => _commandQueue.Enqueue((false, id));

    public unsafe void OnRender(IContext context, float deltaTime)
    {
        while (_commandQueue.TryDequeue(out var command)) {
            var (commandType, id) = command;

            if (commandType) {
                ref readonly var camera = ref context.Inspect<Camera>(id);
                ref var buffer = ref GetCameraBuffer(context, id, out bool exists);

                ref var pars = ref buffer.Parameters;
                pars.Proj = Matrix4x4.Transpose(context.UnsafeAcquire<CameraMatrices>(id).Projection);
                pars.NearPlaneDistance = camera.NearPlaneDistance;
                pars.FarPlaneDistance = camera.FarPlaneDistance;

                *((CameraParameters*)buffer.Pointer) = buffer.Parameters;
            }
            else {
                if (context.Remove<CameraUniformBuffer>(id, out var handle)) {
                    GL.DeleteBuffer(handle.Handle);
                }
            }
        }

        while (_dirtyCameraQueue.TryDequeue(out var id)) {
            ref var buffer = ref GetCameraBuffer(context, id, out bool exists);
            ref var pars = ref buffer.Parameters;
            ref readonly var transform = ref context.Inspect<Transform>(id);

            pars.View = Matrix4x4.Transpose(transform.View);
            pars.ViewProj = pars.Proj * pars.View;
            pars.Position = transform.Position;

            *((CameraParameters*)buffer.Pointer) = buffer.Parameters;
        }
    }


    private ref CameraUniformBuffer GetCameraBuffer(IContext context, Guid id, out bool exists)
    {
        ref var buffer = ref context.Acquire<CameraUniformBuffer>(id, out exists);
        if (!exists) {
            buffer.Handle = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.UniformBuffer, buffer.Handle);
            buffer.Pointer = GLHelper.InitializeBuffer(BufferTargetARB.UniformBuffer, CameraParameters.MemorySize);
            GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Camera, buffer.Handle);
        }
        return ref buffer;
    }
}