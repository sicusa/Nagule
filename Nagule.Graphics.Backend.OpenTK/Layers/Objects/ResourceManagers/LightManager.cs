namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Nagule.Graphics;

public class LightManager : ResourceManagerBase<Light, LightData>, ILoadListener
{
    private class InitializeCommand : Command<InitializeCommand>
    {
        public LightManager? Sender;
        public Guid LightId;
        public Light? Resource;

        public override void Execute(IContext context)
        {
            ref var buffer = ref context.RequireAny<LightsBuffer>();
            ref var data = ref context.Require<LightData>(LightId);

            if (!Sender!._lightIndices.TryPop(out var lightIndex)) {
                lightIndex = Sender._maxIndex++;
                if (buffer.Parameters.Length <= lightIndex) {
                    ResizeLightsBuffer(ref buffer, Sender._maxIndex);
                }
            }
            data.Index = lightIndex;
            InitializeLight(context, LightId, Resource!, ref buffer, ref data);
        }
    }

    private class ReinitializeCommand : Command<ReinitializeCommand>
    {
        public Guid LightId;
        public Light? Resource;

        public override void Execute(IContext context)
        {
            ref var buffer = ref context.RequireAny<LightsBuffer>();
            ref var data = ref context.Require<LightData>(LightId);
            InitializeLight(context, LightId, Resource!, ref buffer, ref data);
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand>
    {
        public LightManager? Sender;
        public Guid LightId;

        public unsafe override void Execute(IContext context)
        {
            ref var buffer = ref context.RequireAny<LightsBuffer>();
            if (!context.Contains<LightData>(LightId)) {
                return;
            }
            ref var data = ref context.Require<LightData>(LightId);

            ((LightParameters*)buffer.Pointer + data.Index)->Category = 0f;
            Sender!._lightIndices.Push(data.Index);
        }
    }

    private Stack<ushort> _lightIndices = new();
    private ushort _maxIndex = 0;

    public void OnLoad(IContext context)
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

    protected unsafe override void Initialize(
        IContext context, Guid id, Light resource, ref LightData data, bool updating)
    {
        if (updating) {
            var cmd = ReinitializeCommand.Create();
            cmd.LightId = id;
            cmd.Resource = resource;
            context.SendCommand<RenderTarget>(cmd);
        }
        else {
            var cmd = InitializeCommand.Create();
            cmd.Sender = this;
            cmd.LightId = id;
            cmd.Resource = resource;
            context.SendCommand<RenderTarget>(cmd);
        }
    }

    protected override unsafe void Uninitialize(IContext context, Guid id, Light resource, in LightData data)
    {
        var cmd = UninitializeCommand.Create();
        cmd.Sender = this;
        cmd.LightId = id;
        context.SendCommand<RenderTarget>(cmd);
    }

    private static unsafe void InitializeLight(IContext context, Guid id, Light resource, ref LightsBuffer buffer, ref LightData data)
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