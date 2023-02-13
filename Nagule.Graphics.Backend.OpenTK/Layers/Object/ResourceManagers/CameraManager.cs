namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Runtime.CompilerServices;

using Aeco.Reactive;

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
        public Guid? RenderTextureId;

        public int Width;
        public int Height;

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
            data.RenderTextureId = RenderTextureId;

            UpdateCameraParameters(Resource!, ref data, Width, Height);
        }
    }

    private class ResizeCommand : Command<ResizeCommand, RenderTarget>
    {
        public Guid CameraId;
        public Camera? Resource;
        public int Width;
        public int Height;

        public override Guid? Id => CameraId;

        public override void Execute(ICommandHost host)
        {
            ref var data = ref host.RequireOrNullRef<CameraData>(CameraId);
            if (Unsafe.IsNullRef(ref data)) { return; }
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
            ResourceLibrary.UnreferenceAll(context, id);
        }
        
        if (context.Singleton<MainCamera>() == null) {
            context.Acquire<MainCamera>(id);
        }

        var cmd = InitializeCommand.Create();
        cmd.CameraId = id;
        cmd.Resource = resource;
        cmd.Width = _width;
        cmd.Height = _height;

        cmd.RenderSettingsId = ResourceLibrary.Reference(context, id, resource.RenderSettings);
        cmd.RenderTextureId = resource.RenderTexture != null
            ? ResourceLibrary.Reference(context, id, resource.RenderTexture)
            : null;

        context.SendCommandBatched(cmd);
    }

    protected override void Uninitialize(IContext context, Guid id, Camera resource)
    {
        ResourceLibrary.UnreferenceAll(context, id);

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

    public static unsafe void UpdateCameraParameters(Camera resource, ref CameraData data, int width, int height)
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