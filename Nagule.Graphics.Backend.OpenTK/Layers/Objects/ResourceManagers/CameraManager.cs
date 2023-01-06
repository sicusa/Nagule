namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using global::OpenTK.Graphics.OpenGL;

using Aeco;
using Aeco.Reactive;

using Nagule.Graphics;

public class CameraManager : ResourceManagerBase<Camera, CameraData>,
    ILoadListener, IWindowResizeListener, IEngineUpdateListener
{
    private enum CommandType
    {
        Initialize,
        Reinitialize,
        Uninitialize,
        UpdateTransform,
    }

    private class InitializeCommand : Command<InitializeCommand>
    {
        public Guid CameraId;
        public Camera? Resource;
        public float Width;
        public float Height;

        public override void Execute(IContext context)
        {
            ref var data = ref context.Require<CameraData>(CameraId);
            data.Handle = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.UniformBuffer, data.Handle);
            data.Pointer = GLHelper.InitializeBuffer(BufferTargetARB.UniformBuffer, CameraParameters.MemorySize);
            UpdateCameraParameters(context, CameraId, Resource!, ref data, Width, Height);
        }
    }

    private class ReinitializeCommand : Command<ReinitializeCommand>
    {
        public Guid CameraId;
        public Camera? Resource;
        public float Width;
        public float Height;

        public override void Execute(IContext context)
        {
            ref var data = ref context.Require<CameraData>(CameraId);
            UpdateCameraParameters(context, CameraId, Resource!, ref data, Width, Height);
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand>
    {
        public Guid CameraId;

        public override void Execute(IContext context)
        {
            ref var data = ref context.Require<CameraData>(CameraId);
            GL.DeleteBuffer(data.Handle);
        }
    }

    private class UpdateTransformCommand : Command<UpdateTransformCommand>
    {
        public Guid CameraId;
        public Vector3 Position;
        public Matrix4x4 View;

        public unsafe override void Execute(IContext context)
        {
            ref var data = ref context.Require<CameraData>(CameraId);
            ref var pars = ref data.Parameters;

            pars.View = Matrix4x4.Transpose(View);
            pars.ViewProj = pars.Proj * pars.View;
            pars.Position = Position;

            *((CameraParameters*)data.Pointer) = data.Parameters;
        }
    }

    private Group<Resource<Camera>> _cameraGroup = new();
    [AllowNull] private IEnumerable<Guid> _dirtyCameraIds;

    private int _width;
    private int _height;

    public void OnLoad(IContext context)
    {
        _dirtyCameraIds = QueryUtil.Intersect(_cameraGroup, context.DirtyTransformIds);
    }

    public void OnWindowResize(IContext context, int width, int height)
    {
        _width = width;
        _height = height;

        foreach (var id in context.Query<CameraData>()) {
            var resource = context.Inspect<Resource<Camera>>(id).Value;
            if (resource != null && resource.RenderTexture == null) {
                var cmd = Command<ReinitializeCommand>.Create();
                cmd.CameraId = id;
                cmd.Resource = resource;
                cmd.Width = _width;
                cmd.Height = _height;
                context.SendCommand<RenderTarget>(cmd);
            }
        }
    }

    public void OnEngineUpdate(IContext context)
    {
        _cameraGroup.Query(context);

        foreach (var id in _dirtyCameraIds) {
            ref readonly var transform = ref context.Inspect<Transform>(id);
            var cmd = Command<UpdateTransformCommand>.Create();
            cmd.CameraId = id;
            cmd.Position = transform.Position;
            cmd.View = transform.View;
            context.SendCommand<RenderTarget>(cmd);
        }
    }

    protected override void Initialize(IContext context, Guid id, Camera resource, ref CameraData data, bool updating)
    {
        if (updating) {
            UnreferenceDependencies(context, id, in data);
        }

        if (resource.RenderPipeline != null) {
            data.RenderPipelineId = ResourceLibrary<RenderPipeline>.Reference(context, resource.RenderPipeline, id);
        }
        else {
            ref readonly var spec = ref context.InspectAny<GraphicsSpecification>();
            var pipeline = spec.IsResizable
                ? new RenderPipeline { AutoResizeByWindow = true }
                : new RenderPipeline {
                    Width = spec.Width,
                    Height = spec.Height
                };
            data.RenderPipelineId = ResourceLibrary<RenderPipeline>.Reference(context, pipeline, id);
        }

        data.RenderTextureId = resource.RenderTexture != null
            ? ResourceLibrary<RenderTexture>.Reference(context, resource.RenderTexture, id)
            : null;
        
        data.NearPlaneDistance = resource.NearPlaneDistance;
        data.FarPlaneDistance = resource.FarPlaneDistance;
        data.ClearFlags = resource.ClearFlags;
        data.Depth = resource.Depth;

        if (context.Singleton<MainCamera>() == null) {
            context.Acquire<MainCamera>(id);
        }

        if (updating) {
            var cmd = Command<ReinitializeCommand>.Create();
            cmd.CameraId = id;
            cmd.Resource = resource;
            cmd.Width = _width;
            cmd.Height = _height;
            context.SendCommand<RenderTarget>(cmd);
        }
        else {
            var cmd = Command<InitializeCommand>.Create();
            cmd.CameraId = id;
            cmd.Resource = resource;
            cmd.Width = _width;
            cmd.Height = _height;
            context.SendCommand<RenderTarget>(cmd);
        }
    }

    protected override void Uninitialize(IContext context, Guid id, Camera resource, in CameraData data)
    {
        UnreferenceDependencies(context, id, in data);

        if (id == context.Singleton<MainCamera>()) {
            context.Remove<MainCamera>(id);
            foreach (var cameraId in _cameraGroup) {
                if (cameraId != id) {
                    context.Acquire<MainCamera>(cameraId);
                    break;
                }
            }
        }

        var cmd = Command<UninitializeCommand>.Create();
        cmd.CameraId = id;
        context.SendCommand<RenderTarget>(cmd);
    }

    private void UnreferenceDependencies(IContext context, Guid id, in CameraData data)
    {
        ResourceLibrary<RenderPipeline>.Unreference(context, data.RenderPipelineId, id);

        if (data.RenderTextureId != null) {
            ResourceLibrary<RenderTexture>.Unreference(context, data.RenderTextureId.Value, id);
        }
    }

    public static unsafe void UpdateCameraParameters(IContext context, Guid id, Camera resource, ref CameraData data, float width, float height)
    {
        ref var pars = ref data.Parameters;

        if (resource.Mode == CameraMode.Perspective) {
            float aspectRatio = (float)width / (float)height;
            data.Projection = Matrix4x4.CreatePerspectiveFieldOfView(
                resource.FieldOfView / 180 * MathF.PI,
                aspectRatio, resource.NearPlaneDistance, resource.FarPlaneDistance);
        }
        else {
            data.Projection = Matrix4x4.CreateOrthographic(
                width, height, resource.NearPlaneDistance, resource.FarPlaneDistance);
        }

        pars.Proj = Matrix4x4.Transpose(data.Projection);
        pars.ViewProj = pars.Proj * pars.View;
        pars.NearPlaneDistance = resource!.NearPlaneDistance;
        pars.FarPlaneDistance = resource!.FarPlaneDistance;

        *((CameraParameters*)data.Pointer) = data.Parameters;
    }
}