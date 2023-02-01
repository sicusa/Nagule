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
        var panoramaVertShader = LoadShader("nagule.common.panorama.vert.glsl");
        var simpleVertShader = LoadShader("nagule.common.simple.vert.glsl");

        context.SetResource(Graphics.DefaultShaderProgramId,
            new GLSLProgram { Name = "nagule.blinn_phong" }
                .WithShaders(
                    new(ShaderType.Vertex, LoadShader("blinn_phong.vert.glsl")),
                    new(ShaderType.Fragment, LoadShader("blinn_phong.frag.glsl")))
                .WithParameters(
                    new(MaterialKeys.Tiling),
                    new(MaterialKeys.Offset),
                    new(MaterialKeys.Diffuse),
                    new(MaterialKeys.DiffuseTex),
                    new(MaterialKeys.Specular),
                    new(MaterialKeys.SpecularTex),
                    new(MaterialKeys.RoughnessTex),
                    new(MaterialKeys.Ambient),
                    new(MaterialKeys.AmbientTex),
                    new(MaterialKeys.AmbientOcclusionTex),
                    new(MaterialKeys.AmbientOcclusionMultiplier),
                    new(MaterialKeys.Emission),
                    new(MaterialKeys.EmissionTex),
                    new(MaterialKeys.Shininess),
                    new(MaterialKeys.Reflectivity),
                    new(MaterialKeys.ReflectionTex),
                    new(MaterialKeys.OpacityTex),
                    new(MaterialKeys.Threshold),
                    new(MaterialKeys.NormalTex),
                    new(MaterialKeys.HeightTex),
                    new(MaterialKeys.ParallaxScale),
                    new(MaterialKeys.EnableParallaxEdgeClip),
                    new(MaterialKeys.EnableParallaxShadow)));

        context.SetResource(Graphics.DefaultDepthShaderProgramId,
            new GLSLProgram { Name = "nagule.depth" }
                .WithShaders(
                    new(ShaderType.Vertex, simpleVertShader),
                    new(ShaderType.Fragment, EmptyFragmentShader)));
        
        context.SetResource(Graphics.SkyboxShaderProgramId,
            new GLSLProgram { Name = "nagule.skybox_cubemap" }
                .WithShaders(
                    new(ShaderType.Vertex, panoramaVertShader),
                    new(ShaderType.Fragment, LoadShader("skybox_cubemap.frag.glsl")))
                .WithParameter(new(MaterialKeys.SkyboxTex)));
        
        context.SetResource(Graphics.BlitColorShaderProgramId,
            new GLSLProgram { Name = "nagule.common.blit_color" }
                .WithShaders(
                    new(ShaderType.Vertex, quadVertShader),
                    new(ShaderType.Fragment, LoadShader("nagule.common.blit_color.frag.glsl")))
                .WithParameter("ColorBuffer", ShaderParameterType.Texture));

        context.SetResource(Graphics.BlitDepthShaderProgramId,
            new GLSLProgram { Name = "nagule.common.blit_depth" }
                .WithShaders(
                    new(ShaderType.Vertex, quadVertShader),
                    new(ShaderType.Fragment, LoadShader("nagule.common.blit_depth.frag.glsl")))
                .WithParameter("DepthBuffer", ShaderParameterType.Texture));
        
        context.SetResource(Graphics.CullingShaderProgramId,
            new GLSLProgram { Name = "nagule.pipeline.cull" }
                .WithShaders(
                    new(ShaderType.Vertex, LoadShader("nagule.pipeline.cull.vert.glsl")),
                    new(ShaderType.Geometry, LoadShader("nagule.pipeline.cull.geo.glsl")))
                .WithFeedback("CulledObjectToWorld"));
        
        context.SetResource(Graphics.OccluderCullingShaderProgramId,
            new GLSLProgram { Name = "nagule.pipeline.cull_occluders" }
                .WithShaders(
                    new(ShaderType.Vertex, LoadShader("nagule.pipeline.cull_occluders.vert.glsl")),
                    new(ShaderType.Geometry, LoadShader("nagule.pipeline.cull.geo.glsl")))
                .WithFeedback("CulledObjectToWorld"));
        
        context.SetResource(Graphics.HierarchicalZShaderProgramId,
            new GLSLProgram { Name = "nagule.pipeline.hiz" }
                .WithShaders(
                    new(ShaderType.Vertex, quadVertShader),
                    new(ShaderType.Fragment, LoadShader("nagule.pipeline.hiz.frag.glsl")))
                .WithParameter("LastMip", ShaderParameterType.Texture));
        
        context.SetResource(Graphics.TransparencyComposeShaderProgramId,
            new GLSLProgram { Name = "nagule.pipeline.transparency_compose" }
                .WithShaders(
                    new(ShaderType.Vertex, quadVertShader),
                    new(ShaderType.Fragment, LoadShader("nagule.pipeline.transparency_compose.frag.glsl")))
                .WithParameters(
                    new("AccumTex", ShaderParameterType.Texture),
                    new("RevealTex", ShaderParameterType.Texture)));
        
        context.SetResource(Graphics.PostProcessingShaderProgramId,
            new GLSLProgram { Name = "nagule.pipeline.post" }
                .WithShaders(
                    new(ShaderType.Vertex, quadVertShader),
                    new(ShaderType.Fragment, LoadShader("nagule.pipeline.post.frag.glsl"))));
        
        context.SetResource(Graphics.DebugPostProcessingShaderProgramId,
            new GLSLProgram { Name = "nagule.pipeline.post_debug" }
                .WithShaders(
                    new(ShaderType.Vertex, quadVertShader),
                    new(ShaderType.Fragment, LoadShader("nagule.pipeline.post_debug.frag.glsl")))
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