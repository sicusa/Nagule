namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Reactive.Disposables;

using Aeco.Reactive;

public class CameraManager : ResourceManagerBase<Camera>,
    IWindowResizeListener, IEngineUpdateListener
{
    private class InitializeCommand : Command<InitializeCommand, RenderTarget>
    {
        public uint CameraId;
        public Camera? Resource;
        public uint RenderSettingsId;
        public float WindowAspectRatio;

        public override uint? Id => CameraId;

        public override void Execute(ICommandHost host)
        {
            ref var data = ref host.Acquire<CameraData>(CameraId, out bool exists);
            if (!exists) {
                data.Handle = GL.GenBuffer();
                GL.BindBuffer(BufferTargetARB.UniformBuffer, data.Handle);
                data.Pointer = GLHelper.InitializeBuffer(BufferTargetARB.UniformBuffer, CameraParameters.MemorySize);
            }

            data.RenderSettingsId = RenderSettingsId;
            data.ProjectionMode = Resource!.ProjectionMode;

            data.AspectRatio = Resource.AspectRatio;
            data.FieldOfView = Resource.FieldOfView;
            data.OrthographicSize = Resource.OrthographicSize;

            data.NearPlaneDistance = Resource.NearPlaneDistance;
            data.FarPlaneDistance = Resource.FarPlaneDistance;
            data.ClearFlags = Resource.ClearFlags;
            data.Depth = Resource.Depth;

            UpdateCameraParameters(ref data, WindowAspectRatio);
            host.Acquire<CameraGroupDirty>();
        }
    }

    private class ResizeCommand : Command<ResizeCommand, RenderTarget>
    {
        public float WindowAspectRatio;

        public override uint? Id { get; } = 0;

        public override void Execute(ICommandHost host)
        {
            foreach (var id in host.Query<CameraData>()) {
                ref var data = ref host.Require<CameraData>(id);
                if (data.AspectRatio == null) {
                    UpdateCameraParameters(ref data, WindowAspectRatio);
                }
            }
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand, RenderTarget>
    {
        public uint CameraId;

        public override void Execute(ICommandHost host)
        {
            if (host.Remove<CameraData>(CameraId, out var data)) {
                GL.DeleteBuffer(data.Handle);
            }
        }
    }

    private class UpdateTransformCommand : Command<UpdateTransformCommand, RenderTarget>
    {
        public uint CameraId;
        public Vector3 Position;
        public Matrix4x4 View;

        public override uint? Id => CameraId;

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

    private int _windowWidth;
    private int _windowHeight;
    private float _windowAspectRatio;

    public void OnWindowResize(IContext context, int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
        _windowAspectRatio = (float)width / (float)height;

        var cmd = ResizeCommand.Create();
        cmd.WindowAspectRatio = _windowAspectRatio;
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

    protected override void Initialize(IContext context, uint id, Camera resource, Camera? prevResource)
    {
        var resLib = context.GetResourceLibrary();
        if (prevResource != null) {
            resLib.UnreferenceAll(id);
        }
        
        if (context.Singleton<MainCamera>() == null) {
            context.Acquire<MainCamera>(id);
        }

        Camera.GetProps(context, id).Set(resource);

        var cmd = InitializeCommand.Create();
        cmd.CameraId = id;
        cmd.Resource = resource;
        cmd.WindowAspectRatio = _windowAspectRatio;
        cmd.RenderSettingsId = resLib.Reference(id, resource.RenderSettings);
        context.SendCommandBatched(cmd);
    }

    protected override IDisposable Subscribe(IContext context, uint id, Camera resource)
    {
        ref var props = ref Camera.GetProps(context, id);
        var resLib = context.GetResourceLibrary();

        return new CompositeDisposable(
            props.ProjectionMode.SubscribeCommand<ProjectionMode, RenderTarget>(
                context, (host, mode) => {
                    ref var data = ref host.Require<CameraData>(id);
                    data.ProjectionMode = mode;
                    UpdateCameraParameters(ref data, _windowAspectRatio);
                }),

            props.ClearFlags.SubscribeCommand<ClearFlags, RenderTarget>(
                context, (host, flags) => {
                    ref var data = ref host.Require<CameraData>(id);
                    data.ClearFlags = flags;
                }),

            props.AspectRatio.SubscribeCommand<float?, RenderTarget>(
                context, (host, aspectRatio) => {
                    ref var data = ref host.Require<CameraData>(id);
                    data.AspectRatio = aspectRatio;
                    UpdateCameraParameters(ref data, _windowAspectRatio);
                }),

            props.FieldOfView.SubscribeCommand<float, RenderTarget>(
                context, (host, fov) => {
                    ref var data = ref host.Require<CameraData>(id);
                    data.FieldOfView = fov;
                    if (data.ProjectionMode == ProjectionMode.Perspective) {
                        UpdateCameraParameters(ref data, _windowAspectRatio);
                    }
                }),

            props.OrthographicSize.SubscribeCommand<float, RenderTarget>(
                context, (host, fov) => {
                    ref var data = ref host.Require<CameraData>(id);
                    data.OrthographicSize = fov;
                    if (data.ProjectionMode == ProjectionMode.Orthographic) {
                        UpdateCameraParameters(ref data, _windowAspectRatio);
                    }
                }),

            props.NearPlaneDistance.SubscribeCommand<float, RenderTarget>(
                context, (host, distance) => {
                    ref var data = ref host.Require<CameraData>(id);
                    data.NearPlaneDistance = distance;
                    UpdateCameraParameters(ref data, _windowAspectRatio);
                }),

            props.FarPlaneDistance.SubscribeCommand<float, RenderTarget>(
                context, (host, distance) => {
                    ref var data = ref host.Require<CameraData>(id);
                    data.FarPlaneDistance = distance;
                    UpdateCameraParameters(ref data, _windowAspectRatio);
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
                    resLib.Unreference(id, prevSettings);
                }
                var settingsId = resLib.Reference(id, settings);

                context.SendCommandBatched<RenderTarget>(Command.Do(host => {
                    ref var data = ref host.Require<CameraData>(id);
                    data.RenderSettingsId = settingsId;
                }));
            })
        );
    }

    protected override void Uninitialize(IContext context, uint id, Camera resource)
    {
        context.GetResourceLibrary().UnreferenceAll(id);

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

    public static unsafe void UpdateCameraParameters(ref CameraData data, float windowAspectRatio)
    {
        ref var pars = ref data.Parameters;
        float aspectRatio = data.AspectRatio ?? windowAspectRatio;

        if (data.ProjectionMode == ProjectionMode.Perspective) {
            data.Projection = Matrix4x4.CreatePerspectiveFieldOfView(
                data.FieldOfView / 180 * MathF.PI,
                aspectRatio, data.NearPlaneDistance, data.FarPlaneDistance);
        }
        else {
            data.Projection = Matrix4x4.CreateOrthographic(
                data.OrthographicSize / aspectRatio, data.OrthographicSize, data.NearPlaneDistance, data.FarPlaneDistance);
        }

        pars.Proj = data.Projection;
        Matrix4x4.Invert(pars.Proj, out pars.ProjInv);
        pars.ViewProj = pars.View * pars.Proj;
        pars.NearPlaneDistance = data.NearPlaneDistance;
        pars.FarPlaneDistance = data.FarPlaneDistance;

        *((CameraParameters*)data.Pointer) = data.Parameters;
    }
}