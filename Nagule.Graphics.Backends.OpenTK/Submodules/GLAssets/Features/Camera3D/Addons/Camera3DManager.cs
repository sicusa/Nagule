namespace Nagule.Graphics.Backends.OpenTK;

using System.Numerics;
using Sia;

public partial class Camera3DManager
{
    private Camera3DRenderer _renderer = null!;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        _renderer = world.GetAddon<Camera3DRenderer>();

        Listen((in EntityRef entity, in Camera3D.OnRenderPipelineDirty e) => {
            var stateEntity = entity.GetStateEntity();
            ref var state = ref stateEntity.Get<Camera3DState>();

            ref var renderSettingsState = ref state.SettingsState.Get<RenderSettingsState>();

            stateEntity.Get<RenderPipelineProvider>().Instance =
                new RenderPipelineProviders.Const(renderSettingsState.RenderPassChain);

            var renderPipeline = entity.FindReferred<RenderPipeline>();
            if (renderPipeline != null) {
                ref var settings = ref entity.FindReferred<RenderSettings>()!.Value.Get<RenderSettings>();
                var passes = RenderPipelineUtils.ConstructChain(entity.GetFeatureNode(), settings);
                renderPipeline.Value.RenderPipeline_SetPasses(passes);
            }
        });

        Listen((in EntityRef entity, in Camera3D.SetPriority cmd) => {
            var renderPipeline = entity.FindReferred<RenderPipeline>();
            if (renderPipeline == null) {
                return;
            }
            var pipelineStateEntity = renderPipeline.Value.GetStateEntity();
            var priority = cmd.Value;

            RenderFramer.Enqueue(entity, () => {
                ref var pipelineState = ref pipelineStateEntity.Get<RenderPipelineState>();
                while (!pipelineState.Loaded) {
                    return false;
                }
                _renderer.Unregister(pipelineStateEntity);
                _renderer.Register(priority, pipelineStateEntity);
                return true;
            });
        });

        Listen((in EntityRef entity, ref Camera3D snapshot, in Camera3D.SetSettings cmd) => {
            entity.Unrefer(world.GetAsset(snapshot.Settings));

            var renderSettingsEntity = world.AcquireAsset(cmd.Value, entity);
            var renderSettingsStateEntity = renderSettingsEntity.GetStateEntity();
            var stateEntity = entity.GetStateEntity();

            RenderFramer.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<Camera3DState>();
                state.SettingsState = renderSettingsStateEntity;
            });

            world.Send(entity, Camera3D.OnRenderPipelineDirty.Instance);
        });

        Listen((in EntityRef entity, in Camera3D.SetClearFlags cmd) => {
            var clearFlags = cmd.Value;
            var stateEntity = entity.GetStateEntity();

            RenderFramer.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<Camera3DState>();
                state.ClearFlags = clearFlags;
            });
        });

        Listen((EntityRef entity, in Camera3D.SetTarget cmd) => {
            var target = CreateRenderTarget(cmd.Value);
            var stateEntity = entity.GetStateEntity();
            target?.OnInitialize(world, entity);

            RenderFramer.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<Camera3DState>();
                var prevTarget = state.RenderTarget;
                if (prevTarget != null) {
                    SimulationFramer.Enqueue(entity, () => prevTarget.OnInitialize(world, entity));
                }
                state.RenderTarget = target;
            });
        });
    }

    public override void LoadAsset(in EntityRef entity, ref Camera3D asset, EntityRef stateEntity)
    {
        var camera = asset;

        var settingsEntity = World.AcquireAsset(asset.Settings, entity);
        var settingsStateEntity = settingsEntity.GetStateEntity();

        stateEntity.Get<RenderPipelineProvider>().Instance =
            new RenderPipelineProviders.Const(
                settingsStateEntity.Get<RenderSettingsState>().RenderPassChain);

        ref var trans = ref entity.GetFeatureNode<Transform3D>();
        var view = trans.View;
        var position = trans.Position;
        var direction = trans.Forward;

        var target = CreateRenderTarget(asset.Target);
        target?.OnInitialize(World, entity);

        var priority = camera.Priority;
        var cameraEntity = entity;

        SimulationFramer.Enqueue(entity, () => {
            ref var settings = ref settingsEntity.Get<RenderSettings>();
            var pipelineEntity = CreateRenderPipeline(World, cameraEntity, settings);
            var pipelineStateEntity = pipelineEntity.GetStateEntity();

            RenderFramer.Enqueue(cameraEntity, () => {
                ref var pipelineState = ref pipelineStateEntity.Get<RenderPipelineState>();
                if (!pipelineState.Loaded) {
                    return false;
                }
                _renderer.Register(camera.Priority, pipelineStateEntity);
                return true;
            });
        });

        RenderFramer.Enqueue(entity, () => {
            var handle = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.UniformBuffer, handle);

            ref var state = ref stateEntity.Get<Camera3DState>();
            state = new Camera3DState {
                Handle = new(handle),
                Pointer = GLUtils.InitializeBuffer(BufferTargetARB.UniformBuffer, Camera3DParameters.MemorySize),
                ClearFlags = camera.ClearFlags,
                SettingsState = settingsStateEntity,
                RenderTarget = target
            };

            UpdateCameraParameters(camera, ref state);
            UpdateCameraTransform(ref state, view, position);
        });
    }

    public override void UnloadAsset(in EntityRef entity, in Camera3D asset, EntityRef stateEntity)
    {
        RenderFramer.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<Camera3DState>();
            GL.DeleteBuffer(state.Handle.Handle);
        });
    }

    private static IRenderTarget? CreateRenderTarget(RenderTarget target)
        => target switch {
            RenderTarget.RenderTexture conv =>
                new TextureRenderTarget(conv.Texture),
            RenderTarget.Window conv =>
                new WindowRenderTarget(conv.Index),
            _ => null
        };
    
    private static EntityRef CreateRenderPipeline(
        World world, in EntityRef cameraEntity, in RenderSettings settings)
    {
        var passes = RenderPipelineUtils.ConstructChain(cameraEntity.GetFeatureNode(), settings);
        return RenderPipeline.CreateEntity(world, new() {
            Camera = cameraEntity,
            Passes = passes
        }, cameraEntity);
    }

    internal void UpdateCameraParameters(EntityRef cameraEntity)
    {
        var camera = cameraEntity.Get<Camera3D>();
        var stateEntity = cameraEntity.GetStateEntity();

        ref var trans = ref cameraEntity.GetFeatureNode<Transform3D>();
        var direction = trans.WorldForward;

        RenderFramer.Enqueue(cameraEntity, () => {
            ref var state = ref stateEntity.Get<Camera3DState>();
            if (!state.Loaded) {
                return;
            }
            UpdateCameraParameters(camera, ref state);
        });
    }

    public static AABB CalculateBoundingBox(
        in Camera3D camera, ref Camera3DState state, in Vector3 direction, float near, float far)
    {
        float fov = camera.FieldOfView;
        float aspect = GetAspectRatio(camera, state);
        ref var pos = ref state.Parameters.Position;

        float nh, nw; // near height & weight
        float fh, fw; // far height & weight

        if (camera.ProjectionMode == ProjectionMode.Perspective) {
            float factor = 2 * MathF.Tan(fov / 180f * MathF.PI / 2f);
            nh = factor * near;
            nw = nh * aspect;
            fh = factor * far;
            fw = fh * aspect;
        }
        else {
            var width = camera.OrthographicWidth;
            nh = fh = width / aspect;
            nw = fw = width;
        }

        var nearCenter = pos + direction * near;
        var nearHalfUp = new Vector3(0f, nh / 2f, 0f);
        var nearHalfRight = new Vector3(nw / 2f, 0f, 0f);

        var farCenter = pos + direction * far;
        var farHalfUp = new Vector3(0f, fh / 2f, 0f);
        var farHalfRight = new Vector3(fw / 2f, 0f, 0f);

        Vector3 min, max;
        min = max = nearCenter + nearHalfUp + nearHalfRight;

        Span<Vector3> points = [
            nearCenter + nearHalfUp - nearHalfRight,
            nearCenter - nearHalfUp + nearHalfRight,
            nearCenter - nearHalfUp - nearHalfRight,

            farCenter + farHalfUp + farHalfRight,
            farCenter + farHalfUp - farHalfRight,
            farCenter - farHalfUp + farHalfRight,
            farCenter - farHalfUp - farHalfRight
        ];

        foreach (ref var point in points) {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }
        return new(min, max);
    }

    private unsafe void UpdateCameraParameters(in Camera3D camera, ref Camera3DState state)
    {
        state.ParametersVersion++;

        ref var pars = ref state.Parameters;
        float aspectRatio = GetAspectRatio(camera, state);

        if (camera.ProjectionMode == ProjectionMode.Perspective) {
            state.Projection = Matrix4x4.CreatePerspectiveFieldOfView(
                camera.FieldOfView / 180 * MathF.PI,
                aspectRatio, camera.NearPlaneDistance, camera.FarPlaneDistance);
        }
        else {
            state.Projection = Matrix4x4.CreateOrthographic(
                camera.OrthographicWidth, camera.OrthographicWidth / aspectRatio,
                camera.NearPlaneDistance, camera.FarPlaneDistance);
        }

        pars.Proj = state.Projection;
        Matrix4x4.Invert(pars.Proj, out pars.ProjInv);
        pars.ViewProj = pars.View * pars.Proj;
        pars.NearPlaneDistance = camera.NearPlaneDistance;
        pars.FarPlaneDistance = camera.FarPlaneDistance;

        unsafe {
            ref var mem = ref *(Camera3DParameters*)state.Pointer;
            mem.Proj = pars.Proj;
            mem.ProjInv = pars.ProjInv;
            mem.ViewProj = pars.ViewProj;
            mem.NearPlaneDistance = pars.NearPlaneDistance;
            mem.FarPlaneDistance = pars.FarPlaneDistance;
        }
    }

    internal void UpdateCameraTransform(ref Camera3DState state, in Matrix4x4 view, in Vector3 position)
    {
        ref var pars = ref state.Parameters;
        pars.View = view;
        pars.ViewProj = pars.View * pars.Proj;
        pars.Position = position;

        unsafe {
            ref var mem = ref *(Camera3DParameters*)state.Pointer;
            mem.View = pars.View;
            mem.ViewProj = pars.ViewProj;
            mem.Position = pars.Position;
        }
    }

    private static float GetAspectRatio(in Camera3D camera, in Camera3DState state)
    {
        if (camera.AspectRatio != null) {
            return camera.AspectRatio.Value;
        }
        else {
            var (width, height) = state.RenderTarget?.ViewportSize ?? (1, 1);
            return width / (float)height;
        }
    }
}