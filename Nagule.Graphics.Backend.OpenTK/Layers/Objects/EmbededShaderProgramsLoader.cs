namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Immutable;

using Aeco;

using Nagule.Graphics;

public class EmbededShaderProgramsLoader : Layer, ILoadListener
{
    private static string LoadShader(string resourceId)
        => EmbededAssets.LoadText("Nagule.Graphics.Backend.OpenTK.Embeded.Shaders." + resourceId);

    public void OnLoad(IContext context)
    {
        var quadVertShader = GraphicsHelper.LoadEmbededShader("nagule.common.quad.vert.glsl");

        GraphicsHelper.LoadEmbededShaderPrograms(context);

        context.SetResource(Graphics.CullingShaderProgramId,
            new GLSLProgram { Name = "pipeline.cull" }
                .WithShaders(
                    new(ShaderType.Vertex, LoadShader("pipeline.cull.vert.glsl")),
                    new(ShaderType.Geometry, LoadShader("pipeline.cull.geo.glsl")))
                .WithFeedback("CulledObjectToWorld"));
        
        context.SetResource(Graphics.OccluderCullingShaderProgramId,
            new GLSLProgram { Name = "pipeline.cull_occluders" }
                .WithShaders(
                    new(ShaderType.Vertex, LoadShader("pipeline.cull_occluders.vert.glsl")),
                    new(ShaderType.Geometry, LoadShader("pipeline.cull.geo.glsl")))
                .WithFeedback("CulledObjectToWorld"));
        
        context.SetResource(Graphics.HierarchicalZShaderProgramId,
            new GLSLProgram { Name = "pipeline.hiz" }
                .WithShaders(
                    new(ShaderType.Vertex, quadVertShader),
                    new(ShaderType.Fragment, LoadShader("pipeline.hiz.frag.glsl")))
                .WithParameter("LastMip", ShaderParameterType.Texture));
        
        context.SetResource(Graphics.TransparencyComposeShaderProgramId,
            new GLSLProgram { Name = "pipeline.transparency_compose" }
                .WithShaders(
                    new(ShaderType.Vertex, quadVertShader),
                    new(ShaderType.Fragment, LoadShader("pipeline.transparency_compose.frag.glsl")))
                .WithParameters(
                    new("AccumTex", ShaderParameterType.Texture),
                    new("RevealTex", ShaderParameterType.Texture)));
        
        context.SetResource(Graphics.PostProcessingShaderProgramId,
            new GLSLProgram { Name = "pipeline.post" }
                .WithShaders(
                    new(ShaderType.Vertex, quadVertShader),
                    new(ShaderType.Fragment, LoadShader("pipeline.post.frag.glsl"))));
        
        context.SetResource(Graphics.DebugPostProcessingShaderProgramId,
            new GLSLProgram { Name = "pipeline.post_debug" }
                .WithShaders(
                    new(ShaderType.Vertex, quadVertShader),
                    new(ShaderType.Fragment, LoadShader("pipeline.post_debug.frag.glsl")))
                .WithParameters(
                    new("ColorBuffer", ShaderParameterType.Texture),
                    new("TransparencyAccumBuffer", ShaderParameterType.Texture),
                    new("TransparencyRevealBuffer", ShaderParameterType.Texture))
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