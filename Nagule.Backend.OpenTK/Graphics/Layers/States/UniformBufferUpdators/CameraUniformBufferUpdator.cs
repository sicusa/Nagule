namespace Nagule.Backend.OpenTK.Graphics;

using System.Numerics;
using System.Diagnostics.CodeAnalysis;

using global::OpenTK.Graphics.OpenGL;

using Aeco;
using Aeco.Reactive;

using Nagule.Graphics;

public class CameraUniformBufferUpdator : ReactiveObjectUpdatorBase<Camera>, ILoadListener
{
    private Group<Camera> _g = new();
    [AllowNull] private IEnumerable<Guid> _modifiedCameraIds;
    [AllowNull] private IEnumerable<Guid> _dirtyCameraIds;

    public void OnLoad(IContext context)
    {
        _modifiedCameraIds = QueryUtil.Intersect(_g, context.Query<Modified<Camera>>());
        _dirtyCameraIds = QueryUtil.Intersect(_g, context.DirtyTransformIds);
    }

    public unsafe override void OnEngineUpdate(IContext context, float deltaTime)
    {
        base.OnEngineUpdate(context, deltaTime);

        _g.Query(context);

        foreach (var id in _modifiedCameraIds) {
            ref var buffer = ref GetCameraBuffer(context, id, out bool exists);
            ref var pars = ref buffer.Parameters;
            pars.Proj = Matrix4x4.Transpose(context.UnsafeAcquire<CameraMatrices>(id).Projection);
            ((CameraParameters*)buffer.Pointer)->Proj = pars.Proj;
        }

        foreach (var id in _dirtyCameraIds) {
            ref var buffer = ref GetCameraBuffer(context, id, out bool exists);
            ref var pars = ref buffer.Parameters;
            ref readonly var transform = ref context.Inspect<Transform>(id);

            pars.View = Matrix4x4.Transpose(transform.View);
            pars.ViewProj = pars.Proj * pars.View;
            pars.Position = transform.Position;

            *((CameraParameters*)buffer.Pointer) = buffer.Parameters;
        }
    }

    protected unsafe override void UpdateObject(IContext context, Guid id)
    {
        ref readonly var camera = ref context.Inspect<Camera>(id);
        ref var buffer = ref GetCameraBuffer(context, id, out bool exists);

        ref var pars = ref buffer.Parameters;
        pars.NearPlaneDistance = camera.NearPlaneDistance;
        pars.FarPlaneDistance = camera.FarPlaneDistance;

        *((CameraParameters*)buffer.Pointer) = buffer.Parameters;
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

    protected override void ReleaseObject(IContext context, Guid id)
    {
        if (context.Remove<CameraUniformBuffer>(id, out var handle)) {
            GL.DeleteBuffer(handle.Handle);
        }
    }
}