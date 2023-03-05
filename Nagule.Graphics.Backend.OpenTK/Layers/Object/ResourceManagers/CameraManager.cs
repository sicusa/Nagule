namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Reactive.Disposables;
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

        public int WindowWidth;
        public int WindowHeight;

        public override Guid? Id => CameraId;

        public override void Execute(ICommandHost host)
        {
            ref var data = ref host.Acquire<CameraData>(CameraId, out bool exists);
            if (!exists) {
                data.Handle = GL.GenBuffer();
                GL.BindBuffer(BufferTargetARB.UniformBuffer, data.Handle);
                data.Pointer = GLHelper.InitializeBuffer(BufferTargetARB.UniformBuffer, CameraParameters.MemorySize);
            }

            data.RenderSettingsId = RenderSettingsId;
            data.RenderTextureId = RenderTextureId;

            data.ProjectionMode = Resource!.ProjectionMode;
            data.FieldOfView = Resource.FieldOfView;
            data.NearPlaneDistance = Resource.NearPlaneDistance;
            data.FarPlaneDistance = Resource.FarPlaneDistance;
            data.ClearFlags = Resource.ClearFlags;
            data.Depth = Resource.Depth;

            UpdateCameraParameters(host, CameraId, ref data, WindowWidth, WindowHeight);
            host.Acquire<CameraGroupDirty>();
        }
    }

    private class ResizeCommand : Command<ResizeCommand, RenderTarget>
    {
        public int Width;
        public int Height;

        public override Guid? Id { get; } = Guid.Empty;

        public override void Execute(ICommandHost host)
        {
            foreach (var id in host.Query<CameraData>()) {
                ref var data = ref host.Require<CameraData>(id);
                if (data.RenderTextureId == null) {
                    RawUpdateCameraParameters(ref data, Width, Height);
                }
            }
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

        var cmd = ResizeCommand.Create();
        cmd.Width = _width;
        cmd.Height = _height;
        context.SendCommandBatched(cmd);
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

        Camera.GetProps(context, id).Set(resource);

        var cmd = InitializeCommand.Create();
        cmd.CameraId = id;
        cmd.Resource = resource;
        cmd.WindowWidth = _width;
        cmd.WindowHeight = _height;

        cmd.RenderSettingsId = ResourceLibrary.Reference(context, id, resource.RenderSettings);
        cmd.RenderTextureId = resource.RenderTexture != null
            ? ResourceLibrary.Reference(context, id, resource.RenderTexture)
            : null;

        context.SendCommandBatched(cmd);
    }

    protected override IDisposable Subscribe(IContext context, Guid id, Camera resource)
    {
        ref var props = ref Camera.GetProps(context, id);

        return new CompositeDisposable(
            props.ProjectionMode.SubscribeCommand<ProjectionMode, RenderTarget>(
                context, (host, mode) => {
                    ref var data = ref host.Require<CameraData>(id);
                    data.ProjectionMode = mode;
                    UpdateCameraParameters(host, id, ref data, _width, _height);
                }),

            props.ClearFlags.SubscribeCommand<ClearFlags, RenderTarget>(
                context, (host, flags) => {
                    ref var data = ref host.Require<CameraData>(id);
                    data.ClearFlags = flags;
                }),

            props.FieldOfView.SubscribeCommand<float, RenderTarget>(
                context, (host, fov) => {
                    ref var data = ref host.Require<CameraData>(id);
                    data.FieldOfView = fov;
                    UpdateCameraParameters(host, id, ref data, _width, _height);
                }),

            props.NearPlaneDistance.SubscribeCommand<float, RenderTarget>(
                context, (host, distance) => {
                    ref var data = ref host.Require<CameraData>(id);
                    data.NearPlaneDistance = distance;
                    UpdateCameraParameters(host, id, ref data, _width, _height);
                }),

            props.FarPlaneDistance.SubscribeCommand<float, RenderTarget>(
                context, (host, distance) => {
                    ref var data = ref host.Require<CameraData>(id);
                    data.FarPlaneDistance = distance;
                    UpdateCameraParameters(host, id, ref data, _width, _height);
                }),

            props.Depth.SubscribeCommand<int, RenderTarget>(
                context, (host, depth) => {
                    ref var data = ref host.Require<CameraData>(id);
                    data.Depth = depth;
                    host.Acquire<CameraGroupDirty>();
                }),

            props.RenderSettings.Modified.Subscribe(tuple => {
                var (prevSettings, settings) = tuple;

                if (prevSettings != null) {
                    ResourceLibrary.Unreference(context, id, prevSettings);
                }
                var settingsId = ResourceLibrary.Reference(context, id, settings);

                context.SendCommandBatched<RenderTarget>(Command.Do(host => {
                    ref var data = ref host.Require<CameraData>(id);
                    data.RenderSettingsId = settingsId;
                }));
            }),
            
            props.RenderTexture.Modified.Subscribe(tuple => {
                var (prevTexture, texture) = tuple;

                if (prevTexture != null) {
                    ResourceLibrary.Unreference(context, id, prevTexture);
                }
                Guid? textureId = texture != null
                    ? ResourceLibrary.Reference(context, id, texture)
                    : null;

                context.SendCommandBatched<RenderTarget>(Command.Do(host => {
                    ref var data = ref host.Require<CameraData>(id);
                    data.RenderTextureId = textureId;
                }));
            })
        );
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

    public static void UpdateCameraParameters(ICommandHost host, Guid id, ref CameraData data, int windowWidth, int windowHeight)
    {
        if (data.RenderTextureId == null) {
            RawUpdateCameraParameters(ref data, windowWidth, windowHeight);
            return;
        }

        ref var renderTexData = ref host.RequireOrNullRef<RenderTextureData>(data.RenderTextureId.Value);
        if (Unsafe.IsNullRef(ref renderTexData)) {
            RawUpdateCameraParameters(ref data, windowWidth, windowHeight);
            return;
        }

        RawUpdateCameraParameters(ref data, renderTexData.Width, renderTexData.Height);
    }

    public static unsafe void RawUpdateCameraParameters(ref CameraData data, int width, int height)
    {
        ref var pars = ref data.Parameters;

        if (data.ProjectionMode == ProjectionMode.Perspective) {
            float aspectRatio = (float)width / (float)height;
            data.Projection = Matrix4x4.CreatePerspectiveFieldOfView(
                data.FieldOfView / 180 * MathF.PI,
                aspectRatio, data.NearPlaneDistance, data.FarPlaneDistance);
        }
        else {
            data.Projection = Matrix4x4.CreateOrthographic(
                width, height, data.NearPlaneDistance, data.FarPlaneDistance);
        }

        pars.Proj = data.Projection;
        Matrix4x4.Invert(pars.Proj, out pars.ProjInv);
        pars.ViewProj = pars.View * pars.Proj;
        pars.NearPlaneDistance = data.NearPlaneDistance;
        pars.FarPlaneDistance = data.FarPlaneDistance;

        *((CameraParameters*)data.Pointer) = data.Parameters;
    }
}