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
        Id = s_brightnessTexId
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
            new("Threshold", ShaderParameterType.Float));
    
    public BloomPass(
        float threshold, float intensity, float radius,
        Texture? dirtTexture, float dirtIntensity)
    {
        Properties = new MaterialProperty[] {
            new("Bloom_BrightnessTex", s_brightnessTexRes),
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
                new("Threshold", threshold));
    }

    public override void LoadResources(IContext context)
    {
        _brightnessMatId = ResourceLibrary.Reference(context, Id, _brightnessMat);
    }

    public override void Execute(ICommandHost host, ICompositionPipeline pipeline, IRenderPipeline renderPipeline)
    {
        ref var brightnessTexData = ref host.RequireOrNullRef<TextureData>(s_brightnessTexId);
        if (Unsafe.IsNullRef(ref brightnessTexData)) { return; }

        ref var matData = ref host.RequireOrNullRef<MaterialData>(_brightnessMatId);
        if (Unsafe.IsNullRef(ref matData)) { return; }

        if (_framebuffer == FramebufferHandle.Zero) {
            _framebuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, brightnessTexData.Handle, 0);
        }
        else {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer);
        }

        GLHelper.ApplyMaterial(host, Id, in matData);
        GLHelper.DrawQuad();

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferHandle.Zero);
    }
}