namespace Nagule.Backend.OpenTK.Graphics;

using System.Numerics;
using System.Diagnostics.CodeAnalysis;

using global::OpenTK.Graphics.OpenGL4;

using Aeco;
using Aeco.Reactive;

using Nagule.Graphics;

public class CameraUniformBufferUpdator : ReactiveObjectUpdatorBase<Camera>, ILoadListener
{
    private Group<Camera> _g = new();
    [AllowNull] private IEnumerable<Guid> _dirtyCameraIds;

    public void OnLoad(IContext context)
    {
        _dirtyCameraIds = QueryUtil.Intersect(_g, context.DirtyTransformIds);
    }

    public unsafe override void OnUpdate(IContext context, float deltaTime)
    {
        base.OnUpdate(context, deltaTime);

        _g.Query(context);

        foreach (var id in _dirtyCameraIds) {
            ref var buffer = ref GetCameraBuffer(context, id, out bool exists);
            ref var pars = ref buffer.Parameters;

            pars.View = Matrix4x4.Transpose(context.UnsafeInspect<Transform>(id).View);
            pars.Proj = Matrix4x4.Transpose(context.UnsafeAcquire<CameraMatrices>(id).Projection);
            pars.ViewProj = pars.Proj * pars.View;
            pars.Position = context.Inspect<Transform>(id).Position;

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
            GL.BindBuffer(BufferTarget.UniformBuffer, buffer.Handle);
            buffer.Pointer = GLHelper.InitializeBuffer(BufferTarget.UniformBuffer, CameraParameters.MemorySize);
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, (int)UniformBlockBinding.Camera, buffer.Handle);
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