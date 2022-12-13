namespace Nagule.Graphics.Backend.OpenTK.Graphics;

using System.Collections.Concurrent;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Nagule.Graphics;

public class LightManager : ResourceManagerBase<Light, LightData, LightResourceBase>, ILoadListener, IRenderListener
{
    private enum CommandType
    {
        Initialize,
        Reinitialize,
        Uninitialize
    }

    private Stack<int> _lightIndeces = new();
    private int _maxIndex = 0;
    private ConcurrentQueue<(CommandType, Guid)> _commandQueue = new();

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
        IContext context, Guid id, ref Light light, ref LightData data, bool updating)
        => _commandQueue.Enqueue(
            (updating ? CommandType.Reinitialize : CommandType.Initialize, id));

    protected override unsafe void Uninitialize(IContext context, Guid id, in Light light, in LightData data)
        => _commandQueue.Enqueue((CommandType.Uninitialize, id));

    public unsafe void OnRender(IContext context, float deltaTime)
    {
        ref var buffer = ref context.RequireAny<LightsBuffer>();

        while (_commandQueue.TryDequeue(out var command)) {
            var (commandType, id) = command;
            ref var data = ref context.Require<LightData>(id);

            switch (commandType) {
            case CommandType.Initialize:
                if (!_lightIndeces.TryPop(out var lightIndex)) {
                    lightIndex = _maxIndex++;
                    if (buffer.Parameters.Length <= lightIndex) {
                        ResizeLightsBuffer(ref buffer);
                    }
                }
                data.Index = lightIndex;
                InitializeLight(context, id, ref buffer, ref data);
                break;
            case CommandType.Reinitialize:
                InitializeLight(context, id, ref buffer, ref data);
                break;
            case CommandType.Uninitialize:
                ((LightParameters*)buffer.Pointer + data.Index)->Category = 0f;
                _lightIndeces.Push(data.Index);
                break;
            }
        }
    }

    private unsafe void InitializeLight(IContext context, Guid id, ref LightsBuffer buffer, ref LightData data)
    {
        var resource = context.Inspect<Light>(id).Resource;
        ref var pars = ref buffer.Parameters[data.Index];

        pars.Color = resource.Color;

        if (resource is AmbientLightResource) {
            data.Category = LightCategory.Ambient;
        }
        else if (resource is DirectionalLightResource) {
            data.Category = LightCategory.Directional;
        }
        else if (resource is AttenuateLightResourceBase attLight) {
            float c = attLight.AttenuationConstant;
            float l = attLight.AttenuationLinear;
            float q = attLight.AttenuationQuadratic;

            data.Range = (-l + MathF.Sqrt(l * l - 4 * q * (c - 255 * attLight.Color.W))) / (2 * q);

            pars.AttenuationConstant = c;
            pars.AttenuationLinear = l;
            pars.AttenuationQuadratic = q;

            switch (attLight) {
            case PointLightResource:
                data.Category = LightCategory.Point;
                break;
            case SpotLightResource spotLight:
                data.Category = LightCategory.Spot;
                pars.ConeCutoffsOrAreaSize.X = MathF.Cos(spotLight.InnerConeAngle / 180f * MathF.PI);
                pars.ConeCutoffsOrAreaSize.Y = MathF.Cos(spotLight.OuterConeAngle / 180f * MathF.PI);
                break;
            case AreaLightResource areaLight:
                data.Category = LightCategory.Area;
                pars.ConeCutoffsOrAreaSize = areaLight.AreaSize;
                break;
            }
        }

        pars.Category = (float)data.Category;
        *((LightParameters*)buffer.Pointer + data.Index) = pars;
    }

    private unsafe void ResizeLightsBuffer(ref LightsBuffer buffer)
    {
        int requiredCapacity = buffer.Parameters.Length;
        while (requiredCapacity < _maxIndex) requiredCapacity *= 2;

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