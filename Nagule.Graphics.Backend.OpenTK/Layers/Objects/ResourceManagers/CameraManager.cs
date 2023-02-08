namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;

using global::OpenTK.Graphics.OpenGL;

using Aeco.Reactive;

using Nagule.Graphics;

public class CameraManager : ResourceManagerBase<Camera>,
    IWindowResizeListener, IEngineUpdateListener
{
    private enum CommandType
    {
        Initialize,
        Reinitialize,
        Uninitialize,
        UpdateTransform,
    }

    private class InitializeCommand : Command<InitializeCommand, RenderTarget>
    {
        public Guid CameraId;
        public Camera? Resource;

        public Guid RenderSettingsId;
        public Guid RenderPipelineId;
        public Guid? RenderTextureId;

        public float Width;
        public float Height;

        public override Guid? Id => CameraId;

        public override void Execute(ICommandHost host)
        {
            ref var data = ref host.Acquire<CameraData>(CameraId, out bool exists);

            if (!exists) {
                data.Handle = GL.GenBuffer();
                GL.BindBuffer(BufferTargetARB.UniformBuffer, data.Handle);
                data.Pointer = GLHelper.InitializeBuffer(BufferTargetARB.UniformBuffer, CameraParameters.MemorySize);
            }

            data.NearPlaneDistance = Resource!.NearPlaneDistance;
            data.FarPlaneDistance = Resource.FarPlaneDistance;
            data.ClearFlags = Resource.ClearFlags;
            data.Depth = Resource.Depth;

            data.RenderSettingsId = RenderSettingsId;
            data.RenderPipelineId = RenderPipelineId;
            data.RenderTextureId = RenderTextureId;

            UpdateCameraParameters(Resource!, ref data, Width, Height);
        }
    }

    private class ResizeCommand : Command<ResizeCommand, RenderTarget>
    {
        public Guid CameraId;
        public Camera? Resource;
        public float Width;
        public float Height;

        public override Guid? Id => CameraId;

        public override void Execute(ICommandHost host)
        {
            if (!host.Contains<CameraData>(CameraId)) {
                return;
            }
            ref var data = ref host.Acquire<CameraData>(CameraId);
            UpdateCameraParameters(Resource!, ref data, Width, Height);
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand, RenderTarget>
    {
        public Guid CameraId;

        public override void Execute(ICommandHost host)
        {
            if (host.Remove<CameraData>(CameraId, out var data)) {
                GL.DeleteBuffer(data.Handle);
            }
        }
    }

    private class UpdateTransformCommand : Command<UpdateTransformCommand, RenderTarget>
    {
        public Guid CameraId;
        public Vector3 Position;
        public Matrix4x4 View;

        public override Guid? Id => CameraId;

        public unsafe override void Execute(ICommandHost host)
        {
            ref var data = ref host.Require<CameraData>(CameraId);
            ref var pars = ref data.Parameters;

            pars.View = View;
            pars.ViewProj = pars.View * pars.Proj;
            pars.Position = Position;

            *((CameraParameters*)data.Pointer) = data.Parameters;
        }
    }

    private Group<Resource<Camera>, TransformDirty> _dirtyCameraGroup = new();

    private int _width;
    private int _height;

    public void OnWindowResize(IContext context, int width, int height)
    {
        _width = width;
        _height = height;

        foreach (var id in context.Query<Resource<Camera>>()) {
            var resource = context.Inspect<Resource<Camera>>(id).Value;
            if (resource == null || resource.RenderTexture != null) {
                continue;
            }
            var cmd = ResizeCommand.Create();
            cmd.CameraId = id;
            cmd.Resource = resource;
            cmd.Width = _width;
            cmd.Height = _height;
            context.SendCommandBatched(cmd);
        }
    }

    public void OnEngineUpdate(IContext context)
    {
        foreach (var id in _dirtyCameraGroup.Query(context)) {
            ref readonly var transform = ref context.Inspect<Transform>(id);
            var cmd = UpdateTransformCommand.Create();
            cmd.CameraId = id;
            cmd.Position = transform.Position;
            cmd.View = transform.View;
            context.SendCommandBatched(cmd);
        }
    }

    protected override void Initialize(IContext context, Guid id, Camera resource, Camera? prevResource)
    {
        if (prevResource != null) {
            UnreferenceDependencies(context, id, prevResource);
        }
        
        if (context.Singleton<MainCamera>() == null) {
            context.Acquire<MainCamera>(id);
        }

        var cmd = InitializeCommand.Create();
        cmd.CameraId = id;
        cmd.Resource = resource;
        cmd.Width = _width;
        cmd.Height = _height;

        cmd.RenderSettingsId = ResourceLibrary<RenderSettings>.Reference(context, id, resource.RenderSettings);

        if (resource.RenderPipeline != null) {
            cmd.RenderPipelineId = ResourceLibrary<RenderPipeline>.Reference(context, id, resource.RenderPipeline);
        }
        else {
            ref readonly var spec = ref context.InspectAny<GraphicsSpecification>();
            var pipeline = spec.IsResizable
                ? new RenderPipeline { AutoResizeByWindow = true }
                : new RenderPipeline {
                    Width = spec.Width,
                    Height = spec.Height
                };
            cmd.RenderPipelineId = ResourceLibrary<RenderPipeline>.Reference(context, id, pipeline);
        }

        cmd.RenderTextureId = resource.RenderTexture != null
            ? ResourceLibrary<RenderTexture>.Reference(context, id, resource.RenderTexture)
            : null;

        context.SendCommandBatched(cmd);
    }

    protected override void Uninitialize(IContext context, Guid id, Camera resource)
    {
        UnreferenceDependencies(context, id, resource);

        if (id == context.Singleton<MainCamera>()) {
            context.Remove<MainCamera>(id);
            foreach (var cameraId in context.Query<Resource<Camera>>()) {
                if (cameraId != id) {
                    context.Acquire<MainCamera>(cameraId);
                    break;
                }
            }
        }

        var cmd = UninitializeCommand.Create();
        cmd.CameraId = id;
        context.SendCommandBatched(cmd);
    }

    private void UnreferenceDependencies(IContext context, Guid id, Camera resource)
    {
        ResourceLibrary<RenderSettings>.UnreferenceAll(context, id);
        ResourceLibrary<RenderPipeline>.UnreferenceAll(context, id);
        ResourceLibrary<RenderTexture>.UnreferenceAll(context, id);
    }

    public static unsafe void UpdateCameraParameters(Camera resource, ref CameraData data, float width, float height)
    {
        ref var pars = ref data.Parameters;

        if (resource.ProjectionMode == ProjectionMode.Perspective) {
            float aspectRatio = (float)width / (float)height;
            data.Projection = Matrix4x4.CreatePerspectiveFieldOfView(
                resource.FieldOfView / 180 * MathF.PI,
                aspectRatio, resource.NearPlaneDistance, resource.FarPlaneDistance);
        }
        else {
            data.Projection = Matrix4x4.CreateOrthographic(
                width, height, resource.NearPlaneDistance, resource.FarPlaneDistance);
        }

        pars.Proj = data.Projection;
        Matrix4x4.Invert(pars.Proj, out pars.ProjInv);
        pars.ViewProj = pars.View * pars.Proj;
        pars.NearPlaneDistance = resource!.NearPlaneDistance;
        pars.FarPlaneDistance = resource!.FarPlaneDistance;

        *((CameraParameters*)data.Pointer) = data.Parameters;
    }
}