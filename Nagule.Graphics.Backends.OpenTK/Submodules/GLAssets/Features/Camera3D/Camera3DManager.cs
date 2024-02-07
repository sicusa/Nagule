namespace Nagule.Graphics.Backends.OpenTK;

using System.Numerics;
using Sia;

public partial class Camera3DManager
{
    internal float WindowAspectRatio { get; set; }

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        Listen((in EntityRef entity, ref Camera3D snapshot, in Camera3D.SetRenderSettings cmd) => {
            entity.Unrefer(world.GetAssetEntity(snapshot.RenderSettings));

            var renderSettingsEntity = world.AcquireAssetEntity(cmd.Value, entity);
            var renderSettingsStateEntity = renderSettingsEntity.GetStateEntity();
            var stateEntity = entity.GetStateEntity();

            RenderFramer.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<Camera3DState>();
                state.RenderSettingsState = renderSettingsStateEntity;
            });
        });

        Listen((in EntityRef entity, in Camera3D.SetClearFlags cmd) => {
            var clearFlags = cmd.Value;
            var stateEntity = entity.GetStateEntity();
            RenderFramer.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<Camera3DState>();
                state.ClearFlags = clearFlags;
            });
        });

        Listen((in EntityRef entity, ref Camera3D snapshot, in Camera3D.SetTargetTexture cmd) => {
            var prevTex = snapshot.TargetTexture;
            if (prevTex != null) {
                entity.Unrefer(world.GetAssetEntity(prevTex));
            }

            var tex = cmd.Value;
            EntityRef? texEntity = tex != null
                ? world.AcquireAssetEntity(tex, entity) : null;
            var texStateEntity = texEntity?.GetStateEntity();

            var stateEntity = entity.GetStateEntity();
            RenderFramer.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<Camera3DState>();
                state.TargetTextureState = texStateEntity;
            });
        });
    }

    public override void LoadAsset(in EntityRef entity, ref Camera3D asset, EntityRef stateEntity)
    {
        var camera = asset;

        stateEntity.Get<RenderPipelineProvider>().Instance =
            asset.RenderSettings.PipelineProvider
                ?? new GLPipelineModule.StandardPipelineProvider(
                    asset.RenderSettings.IsDepthOcclusionEnabled);

        ref var trans = ref entity.GetFeatureNode<Transform3D>();
        var view = trans.View;
        var position = trans.Position;
        var direction = trans.Forward;

        EntityRef? texEntity = asset.TargetTexture != null
            ? World.AcquireAssetEntity(asset.TargetTexture, entity) : null;
        var texStateEntity = texEntity?.GetStateEntity();

        var renderSettingsEntity = World.AcquireAssetEntity(asset.RenderSettings, entity);
        var renderSettingsStateEntity = renderSettingsEntity.GetStateEntity();

        RenderFramer.Enqueue(entity, () => {
            var handle = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.UniformBuffer, handle);

            ref var state = ref stateEntity.Get<Camera3DState>();
            state = new Camera3DState {
                Handle = new(handle),
                Pointer = GLUtils.InitializeBuffer(BufferTargetARB.UniformBuffer, Camera3DParameters.MemorySize),
                ClearFlags = camera.ClearFlags,
                TargetTextureState = texStateEntity,
                RenderSettingsState = renderSettingsStateEntity,
            };

            UpdateCameraParameters(ref state, camera);
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
            UpdateCameraParameters(ref state, camera);
        });
    }

    public AABB CalculateBoundingBox(
        ref Camera3DState state, in Camera3D camera, in Vector3 direction, float near, float far)
    {
        float fov = camera.FieldOfView;
        float aspect = camera.AspectRatio ?? WindowAspectRatio;
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
}