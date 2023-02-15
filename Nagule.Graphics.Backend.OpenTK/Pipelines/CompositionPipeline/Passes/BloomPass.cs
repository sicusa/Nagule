namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.CompilerServices;

public class BloomPass : CompositionPassBase
{
    public override IEnumerable<MaterialProperty> Properties { get; }

    public override string? EntryPoint { get; } = "Bloom";
    public override string? Source { get; }
        = GraphicsHelper.LoadEmbededShader("nagule.pipeline.bloom.comp.glsl");
    
    private Material _brightnessMat;
    private Guid _brightnessMatId;

    private FramebufferHandle _framebuffer;

    private static Guid s_brightnessTexId = Guid.NewGuid();
    private static RenderTexture s_brightnessTexRes = new RenderTexture {
        Id = s_brightnessTexId,
        WrapU = TextureWrapMode.ClampToEdge,
        WrapV = TextureWrapMode.ClampToEdge
    };
    
    private static GLSLProgram s_brightnessProgram =
        new GLSLProgram {}
        .WithShaders(
            new(ShaderType.Vertex,
                GraphicsHelper.LoadEmbededShader("nagule.common.quad.vert.glsl")),
            new(ShaderType.Fragment,
                GraphicsHelper.LoadEmbededShader("nagule.pipeline.brightness.frag.glsl")))
        .WithParameters(
            new("ColorTex", ShaderParameterType.Texture2D),
            new("Radius", ShaderParameterType.Float));
    
    public BloomPass(
        float threshold, float intensity, float radius,
        Texture? dirtTexture, float dirtIntensity)
    {
        Properties = new MaterialProperty[] {
            new("Bloom_BrightnessTex", s_brightnessTexRes),
            new("Bloom_Threshold", threshold),
            new("Bloom_Intensity", intensity),
            new("Bloom_Radius", radius),
            new("Bloom_DirtTexture", dirtTexture),
            new("Bloom_DirtIntensity", dirtIntensity)
        };

        _brightnessMat =
            new Material {
                ShaderProgram = s_brightnessProgram
            }
            .WithProperty(
                new("Radius", radius));
    }

    public override void LoadResources(IContext context)
    {
        _brightnessMatId = ResourceLibrary.Reference(context, Id, _brightnessMat);
    }

    public override void Initialize(ICommandHost host, ICompositionPipeline pipeline)
    {
        _framebuffer = GL.GenFramebuffer();
    }

    public override void Uninitialize(ICommandHost host, ICompositionPipeline pipeline)
    {
        GL.DeleteFramebuffer(_framebuffer);
        _framebuffer = FramebufferHandle.Zero;
    }

    public override void Execute(ICommandHost host, ICompositionPipeline pipeline, IRenderPipeline renderPipeline)
    {
        ref var brightnessTexData = ref host.RequireOrNullRef<RenderTextureData>(s_brightnessTexId);
        if (Unsafe.IsNullRef(ref brightnessTexData)) { return; }

        ref var matData = ref host.RequireOrNullRef<MaterialData>(_brightnessMatId);
        if (Unsafe.IsNullRef(ref matData)) { return; }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, brightnessTexData.TextureHandle, 0);
        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Pipeline, renderPipeline.UniformBufferHandle);

        GL.Clear(ClearBufferMask.ColorBufferBit);
        GLHelper.ApplyInternalMaterial(host, Id, in matData);
        GLHelper.DrawQuad();

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferHandle.Zero);
    }
}