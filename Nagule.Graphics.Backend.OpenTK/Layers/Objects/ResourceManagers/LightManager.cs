namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Concurrent;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Nagule.Graphics;

public class LightManager : ResourceManagerBase<Light>, ILoadListener
{
    private class InitializeLightBufferCommand : Command<InitializeLightBufferCommand, RenderTarget>
    {
        public override void Execute(ICommandContext context)
        {
            ref var buffer = ref context.AcquireAny<LightsBuffer>();
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

    private class InitializeCommand : Command<InitializeCommand, RenderTarget>
    {
        public Guid LightId;
        public Light? Resource;

        public override Guid? Id => LightId;

        public override void Execute(ICommandContext context)
        {
            ref var buffer = ref context.RequireAny<LightsBuffer>();
            ref var data = ref context.Acquire<LightData>(LightId, out bool exists);

            if (!exists) {
                if (!s_lightIndices.TryPop(out var lightIndex)) {
                    lightIndex = s_maxIndex++;
                }
                if (buffer.Parameters.Length <= lightIndex) {
                    ResizeLightsBuffer(ref buffer, lightIndex + 1);
                }
                data.Index = lightIndex;
            }

            UpdateLightParameters(ref buffer, ref data, Resource!);
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand, RenderTarget>
    {
        public Guid LightId;

        public unsafe override void Execute(ICommandContext context)
        {
            if (!context.Remove<LightData>(LightId, out var data)) {
                return;
            }
            ref var buffer = ref context.RequireAny<LightsBuffer>();
            ((LightParameters*)buffer.Pointer + data.Index)->Category = 0f;
            s_lightIndices.Push(data.Index);
        }
    }

    private static Stack<ushort> s_lightIndices = new();
    private static ushort s_maxIndex = 0;

    public void OnLoad(IContext context)
    {
        context.SendCommand(
            InitializeLightBufferCommand.Create());
    }

    protected unsafe override void Initialize(
        IContext context, Guid id, Light resource, Light? prevResource)
    {
        var cmd = InitializeCommand.Create();
        cmd.LightId = id;
        cmd.Resource = resource;
        context.SendCommandBatched(cmd);
    }

    protected override unsafe void Uninitialize(IContext context, Guid id, Light resource)
    {
        var cmd = UninitializeCommand.Create();
        cmd.LightId = id;
        context.SendCommandBatched(cmd);
    }

    private static unsafe void UpdateLightParameters(ref LightsBuffer buffer, ref LightData data, Light resource)
    {
        ref var pars = ref buffer.Parameters[data.Index];
        var type = resource.Type;

        pars.Color = resource.Color;

        if (type == LightType.Ambient) {
            data.Category = LightCategory.Ambient;
        }
        else if (type == LightType.Directional) {
            data.Category = LightCategory.Directional;
        }
        else {
            float c = resource.AttenuationConstant;
            float l = resource.AttenuationLinear;
            float q = resource.AttenuationQuadratic;

            data.Range = (-l + MathF.Sqrt(l * l - 4 * q * (c - 255 * resource.Color.W))) / (2 * q);

            pars.AttenuationConstant = c;
            pars.AttenuationLinear = l;
            pars.AttenuationQuadratic = q;

            switch (type) {
            case LightType.Point:
                data.Category = LightCategory.Point;
                break;
            case LightType.Spot:
                data.Category = LightCategory.Spot;
                pars.ConeCutoffsOrAreaSize.X = MathF.Cos(resource.InnerConeAngle / 180f * MathF.PI);
                pars.ConeCutoffsOrAreaSize.Y = MathF.Cos(resource.OuterConeAngle / 180f * MathF.PI);
                break;
            case LightType.Area:
                data.Category = LightCategory.Area;
                pars.ConeCutoffsOrAreaSize = resource.AreaSize;
                break;
            }
        }

        pars.Category = (float)data.Category;
        *((LightParameters*)buffer.Pointer + data.Index) = pars;
    }

    private static unsafe void ResizeLightsBuffer(ref LightsBuffer buffer, int maxIndex)
    {
        int requiredCapacity = buffer.Parameters.Length;
        while (requiredCapacity < maxIndex) requiredCapacity *= 2;

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