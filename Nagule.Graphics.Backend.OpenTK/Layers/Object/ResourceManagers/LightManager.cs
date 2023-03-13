namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Reactive.Disposables;

using Nagule.Graphics;

public class LightManager : ResourceManagerBase<Light>, ILoadListener
{
    private class InitializeLightsBufferCommand : Command<InitializeLightsBufferCommand, RenderTarget>
    {
        public override void Execute(ICommandHost host)
        {
            ref var buffer = ref host.Acquire<LightsBuffer>();
            buffer.Capacity = LightsBuffer.InitialCapacity;
            buffer.Parameters = new LightParameters[buffer.Capacity];

            buffer.Handle = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.TextureBuffer, buffer.Handle);

            buffer.Pointer = GLHelper.InitializeBuffer(BufferTargetARB.TextureBuffer, buffer.Capacity * LightParameters.MemorySize);
            buffer.TexHandle = GL.GenTexture();

            GL.BindTexture(TextureTarget.TextureBuffer, buffer.TexHandle);
            GL.TexBuffer(TextureTarget.TextureBuffer, SizedInternalFormat.R32f, buffer.Handle);

            GL.BindBuffer(BufferTargetARB.TextureBuffer, BufferHandle.Zero);
            GL.BindTexture(TextureTarget.TextureBuffer, TextureHandle.Zero);
        }
    }

    private class InitializeCommand : Command<InitializeCommand, RenderTarget>, IDeferrableCommand
    {
        public uint LightId;
        public Light? Resource;

        public uint LightingEnvironmentId;
        public uint? ShadowCameraId;

        public override uint? Id => LightId;

        public bool ShouldExecute(ICommandHost host)
            => host.Contains<LightingEnvironmentData>(LightingEnvironmentId);

        public unsafe override void Execute(ICommandHost host)
        {
            ref var buffer = ref host.Require<LightsBuffer>();
            ref var data = ref host.Acquire<LightData>(LightId, out bool exists);

            if (!exists) {
                if (!s_lightIndices.TryPop(out var lightIndex)) {
                    lightIndex = s_maxIndex++;
                }
                if (buffer.Parameters.Length <= lightIndex) {
                    ResizeLightsBuffer(ref buffer, lightIndex);
                }
                data.Index = lightIndex;
                data.ShadowMapId = -1;
            }

            data.ShadowCameraId = ShadowCameraId;

            if (ShadowCameraId.HasValue) {
                if (data.ShadowMapId == -1) {
                    ref var lightingEnvData = ref host.Require<LightingEnvironmentData>(LightingEnvironmentId);
                    data.ShadowMapId = Resource!.Type == LightType.Point
                        ? lightingEnvData.ShadowMapCubemapPool.Allocate()
                        : lightingEnvData.ShadowMapTexturePool.Allocate();
                }
            }
            else if (data.ShadowMapId != -1) {
                ref var lightingEnvData = ref host.Require<LightingEnvironmentData>(LightingEnvironmentId);
                if (data.Type == LightType.Point) {
                    lightingEnvData.ShadowMapCubemapPool.Release(data.ShadowMapId);
                }
                else {
                    lightingEnvData.ShadowMapTexturePool.Release(data.ShadowMapId);
                }
                data.ShadowMapId = -1;
            }

            var type = Resource!.Type;
            data.Type = type;

            ref var pars = ref buffer.Parameters[data.Index];
            pars.Type = (float)type;
            pars.ShadowMapId = (float)data.ShadowMapId;
            pars.Color = Resource.Color;

            if (type == LightType.Ambient || type == LightType.Directional) {
                pars.Range = float.PositiveInfinity;
            }
            else {
                pars.Range = Resource.Range;

                switch (type) {
                case LightType.Spot:
                    pars.ConeCutoffsOrAreaSize.X = MathF.Cos(Resource.InnerConeAngle / 180f * MathF.PI);
                    pars.ConeCutoffsOrAreaSize.Y = MathF.Cos(Resource.OuterConeAngle / 180f * MathF.PI);
                    break;
                case LightType.Area:
                    pars.ConeCutoffsOrAreaSize = Resource.AreaSize;
                    break;
                }
            }

            *((LightParameters*)buffer.Pointer + data.Index) = pars;
        }

        private static unsafe void ResizeLightsBuffer(ref LightsBuffer buffer, int maxIndex)
        {
            int requiredCapacity = buffer.Parameters.Length;
            while (requiredCapacity <= maxIndex) requiredCapacity *= 2;

            var prevPars = buffer.Parameters;
            buffer.Parameters = new LightParameters[requiredCapacity];
            Array.Copy(prevPars, buffer.Parameters, buffer.Capacity);

            var newBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.TextureBuffer, newBuffer);
            var pointer = GLHelper.InitializeBuffer(BufferTargetARB.TextureBuffer, buffer.Parameters.Length * LightParameters.MemorySize);

            GL.BindBuffer(BufferTargetARB.CopyReadBuffer, buffer.Handle);
            GL.CopyBufferSubData(CopyBufferSubDataTarget.CopyReadBuffer, CopyBufferSubDataTarget.TextureBuffer,
                IntPtr.Zero, IntPtr.Zero, buffer.Capacity * LightParameters.MemorySize);

            GL.DeleteTexture(buffer.TexHandle);
            GL.DeleteBuffer(buffer.Handle);

            buffer.TexHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.TextureBuffer, buffer.TexHandle);
            GL.TexBuffer(TextureTarget.TextureBuffer, SizedInternalFormat.R32f, newBuffer);

            buffer.Capacity = buffer.Parameters.Length;
            buffer.Handle = newBuffer;
            buffer.Pointer = pointer;

            GL.BindTexture(TextureTarget.TextureBuffer, TextureHandle.Zero);
            GL.BindBuffer(BufferTargetARB.TextureBuffer, BufferHandle.Zero);
            GL.BindBuffer(BufferTargetARB.CopyReadBuffer, BufferHandle.Zero);
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand, RenderTarget>
    {
        public uint LightId;

        public unsafe override void Execute(ICommandHost host)
        {
            if (!host.Remove<LightData>(LightId, out var data)) {
                return;
            }
            ref var buffer = ref host.Require<LightsBuffer>();
            ((LightParameters*)buffer.Pointer + data.Index)->Type = 0f;
            s_lightIndices.Push(data.Index);
        }
    }

    private static Stack<ushort> s_lightIndices = new();
    private static ushort s_maxIndex = 0;

    public void OnLoad(IContext context)
    {
        context.SendCommandBatched(
            InitializeLightsBufferCommand.Create());
    }

    protected unsafe override void Initialize(
        IContext context, uint id, Light resource, Light? prevResource)
    {
        var resLib = context.GetResourceLibrary();

        Light.GetProps(context, id).Set(resource);

        var cmd = InitializeCommand.Create();
        cmd.LightId = id;
        cmd.Resource = resource;

        var envId = resLib.Reference(id, resource.Environment);
        cmd.LightingEnvironmentId = envId;

        if (resource.IsShadowEnabled && resource.Type != LightType.Ambient) {
            ref var envProps = ref LightingEnvironment.GetProps(context, envId);

            cmd.ShadowCameraId = resLib.Reference(id,
                new Camera {
                    ProjectionMode =
                        resource.Type == LightType.Directional
                            ? ProjectionMode.Orthographic : ProjectionMode.Perspective,
                    ClearFlags = ClearFlags.Depth
                });
        }
        else {
            cmd.ShadowCameraId = null;
        }

        context.SendCommandBatched(cmd);
    }

    protected override IDisposable? Subscribe(IContext context, uint id, Light resource)
    {
        ref var props = ref Light.GetProps(context, id);

        unsafe ref LightParameters GetPars(IntPtr ptr, int index)
            => ref ((LightParameters*)ptr)[index];

        return new CompositeDisposable(
            props.Type.SubscribeCommand<LightType, RenderTarget>(
                context, (host, type) => {
                    ref var data = ref host.Require<LightData>(id);
                    ref var buffer = ref host.Require<LightsBuffer>();
                    float convType = (float)type;

                    data.Type = type;
                    buffer.Parameters[data.Index].Type = convType;
                    GetPars(buffer.Pointer, data.Index).Type = convType;
                }),
            
            props.Color.SubscribeCommand<Vector4, RenderTarget>(
                context, (host, color) => {
                    ref var data = ref host.Require<LightData>(id);
                    ref var buffer = ref host.Require<LightsBuffer>();

                    buffer.Parameters[data.Index].Color = color;
                    GetPars(buffer.Pointer, data.Index).Color = color;
                }),

            props.Range.SubscribeCommand<float, RenderTarget>(
                context, (host, range) => {
                    ref var data = ref host.Require<LightData>(id);
                    ref var buffer = ref host.Require<LightsBuffer>();

                    buffer.Parameters[data.Index].Range = range;
                    GetPars(buffer.Pointer, data.Index).Range = range;
                }),
            
            props.InnerConeAngle.SubscribeCommand<float, RenderTarget>(
                context, (host, angle) => {
                    ref var data = ref host.Require<LightData>(id);
                    ref var buffer = ref host.Require<LightsBuffer>();

                    ref var pars = ref buffer.Parameters[data.Index];
                    if (data.Type != LightType.Spot) { return; }

                    pars.ConeCutoffsOrAreaSize.X = angle;
                    GetPars(buffer.Pointer, data.Index).ConeCutoffsOrAreaSize.X = angle;
                }),

            props.OuterConeAngle.SubscribeCommand<float, RenderTarget>(
                context, (host, angle) => {
                    ref var data = ref host.Require<LightData>(id);
                    ref var buffer = ref host.Require<LightsBuffer>();

                    ref var pars = ref buffer.Parameters[data.Index];
                    if (data.Type != LightType.Spot) { return; }

                    pars.ConeCutoffsOrAreaSize.Y = angle;
                    GetPars(buffer.Pointer, data.Index).ConeCutoffsOrAreaSize.Y = angle;
                }),

            props.AreaSize.SubscribeCommand<Vector2, RenderTarget>(
                context, (host, size) => {
                    ref var data = ref host.Require<LightData>(id);
                    ref var buffer = ref host.Require<LightsBuffer>();

                    ref var pars = ref buffer.Parameters[data.Index];
                    if (data.Type != LightType.Area) { return; }

                    pars.ConeCutoffsOrAreaSize = size;
                    GetPars(buffer.Pointer, data.Index).ConeCutoffsOrAreaSize = size;
                })
        );
    }

    protected override unsafe void Uninitialize(IContext context, uint id, Light resource)
    {
        var cmd = UninitializeCommand.Create();
        cmd.LightId = id;
        context.SendCommandBatched(cmd);
    }
}