namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using global::OpenTK.Graphics.OpenGL;

using Aeco;
using Aeco.Reactive;

using Nagule.Graphics;

public class CameraManager : ResourceManagerBase<Camera, CameraData>,
    ILoadListener, IWindowResizeListener, IEngineUpdateListener, IRenderListener
{
    private enum CommandType
    {
        Initialize,
        Reinitialize,
        Uninitialize,
        UpdateTransform,
    }

    private Group<Resource<Camera>> _cameraGroup = new();
    [AllowNull] private IEnumerable<Guid> _dirtyCameraIds;

    private ConcurrentQueue<(CommandType, Guid, Camera?)> _commandQueue = new();

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
                _commandQueue.Enqueue((CommandType.Reinitialize, id, resource));
            }
        }
    }

    public void OnEngineUpdate(IContext context, float deltaTime)
    {
        _cameraGroup.Query(context);

        foreach (var id in _dirtyCameraIds) {
            _commandQueue.Enqueue((CommandType.UpdateTransform, id, null));
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
        
        data.ClearFlags = resource.ClearFlags;
        data.Depth = resource.Depth;

        if (context.Singleton<MainCamera>() == null) {
            context.Acquire<MainCamera>(id);
        }
        _commandQueue.Enqueue(
            (updating ? CommandType.Reinitialize : CommandType.Initialize, id, resource));
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

        _commandQueue.Enqueue((CommandType.Uninitialize, id, null));
    }

    private void UnreferenceDependencies(IContext context, Guid id, in CameraData data)
    {
        ResourceLibrary<RenderPipeline>.Unreference(context, data.RenderPipelineId, id);

        if (data.RenderTextureId != null) {
            ResourceLibrary<RenderTexture>.Unreference(context, data.RenderTextureId.Value, id);
        }
    }

    public unsafe void OnRender(IContext context, float deltaTime)
    {
        while (_commandQueue.TryDequeue(out var command)) {
            var (commandType, id, resource) = command;
            ref var data = ref context.Require<CameraData>(id);

            switch (commandType) {
            case CommandType.Initialize:
                data.Handle = GL.GenBuffer();
                GL.BindBuffer(BufferTargetARB.UniformBuffer, data.Handle);
                data.Pointer = GLHelper.InitializeBuffer(BufferTargetARB.UniformBuffer, CameraParameters.MemorySize);
                UpdateCameraParameters(context, id, resource!, ref data);
                break;

            case CommandType.Reinitialize:
                UpdateCameraParameters(context, id, resource!, ref data);
                break;
            
            case CommandType.Uninitialize:
                GL.DeleteBuffer(data.Handle);
                break;
            
            case CommandType.UpdateTransform:
                ref readonly var transform = ref context.Inspect<Transform>(id);
                ref var pars = ref data.Parameters;

                pars.View = Matrix4x4.Transpose(transform.View);
                pars.ViewProj = pars.Proj * pars.View;
                pars.Position = transform.Position;

                *((CameraParameters*)data.Pointer) = data.Parameters;
                break;
            }
        }
    }

    public unsafe void UpdateCameraParameters(IContext context, Guid id, Camera resource, ref CameraData data)
    {
        ref var pars = ref data.Parameters;
        ref var matrices = ref context.Acquire<CameraMatrices>(id);

        if (resource.Mode == CameraMode.Perspective) {
            float aspectRatio = (float)_width / (float)_height;
            matrices.Projection = Matrix4x4.CreatePerspectiveFieldOfView(
                resource.FieldOfView / 180 * MathF.PI,
                aspectRatio, resource.NearPlaneDistance, resource.FarPlaneDistance);
        }
        else {
            matrices.Projection = Matrix4x4.CreateOrthographic(
                _width, _height, resource.NearPlaneDistance, resource.FarPlaneDistance);
        }

        pars.Proj = Matrix4x4.Transpose(matrices.Projection);
        pars.ViewProj = pars.Proj * pars.View;
        pars.NearPlaneDistance = resource!.NearPlaneDistance;
        pars.FarPlaneDistance = resource!.FarPlaneDistance;

        *((CameraParameters*)data.Pointer) = data.Parameters;
    }
}