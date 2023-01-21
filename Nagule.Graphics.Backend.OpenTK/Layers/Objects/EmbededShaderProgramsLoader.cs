namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Immutable;

using Aeco;

using Nagule.Graphics;

public class EmbededShaderProgramsLoader : Layer, ILoadListener
{
    private static string LoadShader(string resourceId)
        => InternalAssets.LoadText("Nagule.Graphics.Backend.OpenTK.Embeded.Shaders." + resourceId);

    private readonly string EmptyFragmentShader = "#version 410 core\nvoid main() { }";

    public void OnLoad(IContext context)
    {
        var quadVertShader = LoadShader("nagule.common.quad.vert.glsl");
        var cubemapVertShader = LoadShader("nagule.common.cubemap.vert.glsl");
        var simpleVertShader = LoadShader("nagule.common.simple.vert.glsl");
        var whiteFragShader = LoadShader("nagule.common.white.frag.glsl");
        var blinnPhongVert = LoadShader("blinn_phong.vert.glsl");
        var unlitVert = LoadShader("unlit.vert.glsl");

        context.SetResource(Graphics.DefaultOpaqueShaderProgramId,
            new ShaderProgram()
                .WithShaders(
                    KeyValuePair.Create(ShaderType.Vertex, blinnPhongVert),
                    KeyValuePair.Create(ShaderType.Fragment, LoadShader("blinn_phong.frag.glsl"))));

        context.SetResource(Graphics.DefaultTransparentShaderProgramId,
            new ShaderProgram()
                .WithShaders(
                    KeyValuePair.Create(ShaderType.Vertex, blinnPhongVert),
                    KeyValuePair.Create(ShaderType.Fragment, LoadShader("blinn_phong_transparent.frag.glsl"))));

        context.SetResource(Graphics.DefaultCutoffShaderProgramId,
            new ShaderProgram()
                .WithShaders(
                    KeyValuePair.Create(ShaderType.Vertex, blinnPhongVert),
                    KeyValuePair.Create(ShaderType.Fragment, LoadShader("blinn_phong_cutoff.frag.glsl")))
                .WithParameter("Threshold", ShaderParameterType.Float));
        
        context.SetResource(Graphics.DefaultUnlitShaderProgramId,
            new ShaderProgram()
                .WithShaders(
                    KeyValuePair.Create(ShaderType.Vertex, unlitVert),
                    KeyValuePair.Create(ShaderType.Fragment, LoadShader("unlit.frag.glsl"))));
        
        context.SetResource(Graphics.DefaultUnlitTransparentShaderProgramId,
            new ShaderProgram()
                .WithShaders(
                    KeyValuePair.Create(ShaderType.Vertex, unlitVert),
                    KeyValuePair.Create(ShaderType.Fragment, LoadShader("unlit_transparent.frag.glsl"))));

        context.SetResource(Graphics.DefaultUnlitCutoffShaderProgramId,
            new ShaderProgram()
                .WithShaders(
                    KeyValuePair.Create(ShaderType.Vertex, unlitVert),
                    KeyValuePair.Create(ShaderType.Fragment, LoadShader("unlit_cutoff.frag.glsl")))
                .WithParameter("Threshold", ShaderParameterType.Float));
        
        context.SetResource(Graphics.DefaultDepthShaderProgramId,
            new ShaderProgram()
                .WithShaders(
                    KeyValuePair.Create(ShaderType.Vertex, simpleVertShader),
                    KeyValuePair.Create(ShaderType.Fragment, EmptyFragmentShader)));
        
        context.SetResource(Graphics.SkyboxShaderProgramId,
            new ShaderProgram()
                .WithShaders(
                    KeyValuePair.Create(ShaderType.Vertex, cubemapVertShader),
                    KeyValuePair.Create(ShaderType.Fragment, LoadShader("skybox_cubemap.frag.glsl")))
                .WithParameters(
                    KeyValuePair.Create("SkyboxTex", ShaderParameterType.Texture)));
        
        context.SetResource(Graphics.CullingShaderProgramId,
            ShaderProgram.NonMaterial
                .WithShaders(
                    KeyValuePair.Create(ShaderType.Vertex, LoadShader("nagule.pipeline.cull.vert.glsl")),
                    KeyValuePair.Create(ShaderType.Geometry, LoadShader("nagule.pipeline.cull.geo.glsl")))
                .WithTransformFeedbackVarying("CulledObjectToWorld"));
        
        context.SetResource(Graphics.OccluderCullingShaderProgramId,
            ShaderProgram.NonMaterial
                .WithShaders(
                    KeyValuePair.Create(ShaderType.Vertex, LoadShader("nagule.pipeline.cull_occluders.vert.glsl")),
                    KeyValuePair.Create(ShaderType.Geometry, LoadShader("nagule.pipeline.cull.geo.glsl")))
                .WithTransformFeedbackVarying("CulledObjectToWorld"));
        
        context.SetResource(Graphics.HierarchicalZShaderProgramId,
            ShaderProgram.NonMaterial
                .WithShaders(
                    KeyValuePair.Create(ShaderType.Vertex, quadVertShader),
                    KeyValuePair.Create(ShaderType.Fragment, LoadShader("nagule.pipeline.hiz.frag.glsl")))
                .WithParameters(
                    KeyValuePair.Create("LastMip", ShaderParameterType.Texture)));
        
        context.SetResource(Graphics.TransparencyComposeShaderProgramId,
            ShaderProgram.NonMaterial
                .WithShaders(
                    KeyValuePair.Create(ShaderType.Vertex, quadVertShader),
                    KeyValuePair.Create(ShaderType.Fragment, LoadShader("nagule.pipeline.transparency_compose.frag.glsl")))
                .WithParameters(
                    KeyValuePair.Create("AccumTex", ShaderParameterType.Texture),
                    KeyValuePair.Create("RevealTex", ShaderParameterType.Texture)));
        
        context.SetResource(Graphics.PostProcessingShaderProgramId,
            ShaderProgram.NonMaterial
                .WithShaders(
                    KeyValuePair.Create(ShaderType.Vertex, quadVertShader),
                    KeyValuePair.Create(ShaderType.Fragment, LoadShader("nagule.pipeline.post.frag.glsl"))));
        
        context.SetResource(Graphics.DebugPostProcessingShaderProgramId,
            ShaderProgram.NonMaterial
                .WithShaders(
                    KeyValuePair.Create(ShaderType.Vertex, quadVertShader),
                    KeyValuePair.Create(ShaderType.Fragment, LoadShader("nagule.pipeline.post_debug.frag.glsl")))
                .WithParameters(
                    KeyValuePair.Create("ColorBuffer", ShaderParameterType.Texture),
                    KeyValuePair.Create("TransparencyAccumBuffer", ShaderParameterType.Texture),
                    KeyValuePair.Create("TransparencyRevealBuffer", ShaderParameterType.Texture))
                .WithSubroutine(
                    ShaderType.Fragment,
                    ImmutableArray.Create(
                        "ShowColor",
                        "ShowTransparencyAccum",
                        "ShowTransparencyReveal",
                        "ShowDepth",
                        "ShowClusters")));
    }
}