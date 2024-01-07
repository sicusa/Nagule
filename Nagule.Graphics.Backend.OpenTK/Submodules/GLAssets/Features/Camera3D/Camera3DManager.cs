namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Sia;

public partial class Camera3DManager
{
    internal float WindowAspectRatio { get; set; }

    [AllowNull] private RenderSettingsManager _renderSettingsManager;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        _renderSettingsManager = world.GetAddon<RenderSettingsManager>();

        Listen((EntityRef entity, ref Camera3D snapshot, in Camera3D.SetRenderSettings cmd) => {
            entity.UnreferAsset(_renderSettingsManager[snapshot.RenderSettings]);
            var renderSettingsEntity = _renderSettingsManager.Acquire(cmd.Value, entity);

            RenderFrame.Enqueue(entity, () => {
                ref var state = ref entity.GetState<Camera3DState>();
                state.RenderSettingsEntity = renderSettingsEntity;
                return true;
            });
        });

        Listen((EntityRef entity, in Camera3D.SetClearFlags cmd) => {
            var clearFlags = cmd.Value;
            RenderFrame.Enqueue(entity, () => {
                ref var state = ref entity.GetState<Camera3DState>();
                state.ClearFlags = clearFlags;
                return true;
            });
        });
    }

    protected override void LoadAsset(EntityRef entity, ref Camera3D asset, EntityRef stateEntity)
    {
        stateEntity.Get<RenderPipelineProvider>().Instance =
            GLPipelineModule.StandardPipelineProvider.Instance;

        ref var trans = ref entity.GetFeatureNode().Get<Transform3D>();
        var view = trans.View;
        var position = trans.Position;

        var renderSettingsEntity = _renderSettingsManager.Acquire(asset.RenderSettings, entity);
        var camera = asset;

        RenderFrame.Enqueue(entity, () => {
            var handle = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.UniformBuffer, handle);

            ref var state = ref stateEntity.Get<Camera3DState>();
            state = new Camera3DState {
                RenderSettingsEntity = renderSettingsEntity,
                Handle = new(handle),
                Pointer = GLUtils.InitializeBuffer(BufferTargetARB.UniformBuffer, Camera3DParameters.MemorySize),
                ClearFlags = camera.ClearFlags
            };

            UpdateCameraParameters(ref state, camera);
            UpdateCameraTransform(ref state, view, position);
            return true;
        });
    }

    protected override void UnloadAsset(EntityRef entity, ref Camera3D asset, EntityRef stateEntity)
    {
        RenderFrame.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<Camera3DState>();
            GL.DeleteBuffer(state.Handle.Handle);
            return true;
        });
    }

    internal void UpdateCameraParameters(EntityRef cameraEntity)
    {
        var camera = cameraEntity.Get<Camera3D>();
        RenderFrame.Enqueue(cameraEntity, () => {
            ref var state = ref cameraEntity.GetState<Camera3DState>();
            if (!state.Loaded) {
                return true;
            }
            UpdateCameraParameters(ref state, camera);
            return true;
        });
    }

    internal void UpdateCameraTransform(EntityRef cameraEntity)
    {
        ref var trans = ref cameraEntity.GetFeatureNode().Get<Transform3D>();
        var view = trans.View;
        var position = trans.WorldPosition;

        RenderFrame.Enqueue(cameraEntity, () => {
            ref var state = ref cameraEntity.GetState<Camera3DState>();
            if (!state.Loaded) {
                return true;
            }
            UpdateCameraTransform(ref state, view, position);
            return true;
        });
    }

    private unsafe void UpdateCameraParameters(ref Camera3DState state, in Camera3D camera)
    {
        state.ParametersVersion++;

        ref var pars = ref state.Parameters;
        float aspectRatio = camera.AspectRatio ?? WindowAspectRatio;

        if (state.ProjectionMode == ProjectionMode.Perspective) {
            state.Projection = Matrix4x4.CreatePerspectiveFieldOfView(
                camera.FieldOfView / 180 * MathF.PI,
                aspectRatio, camera.NearPlaneDistance, camera.FarPlaneDistance);
        }
        else {
            state.Projection = Matrix4x4.CreateOrthographic(
                camera.OrthographicWidth / aspectRatio, camera.OrthographicWidth,
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

    private void UpdateCameraTransform(ref Camera3DState state, in Matrix4x4 view, in Vector3 position)
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
}