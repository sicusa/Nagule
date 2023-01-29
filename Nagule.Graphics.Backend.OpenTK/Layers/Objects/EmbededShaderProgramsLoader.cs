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
                    new(MaterialKeys.Diffuse, ShaderParameterType.Vector4),
                    new(MaterialKeys.Specular, ShaderParameterType.Vector4),
                    new(MaterialKeys.Ambient, ShaderParameterType.Vector4),
                    new(MaterialKeys.Emission, ShaderParameterType.Vector4),
                    new(MaterialKeys.Shininess, ShaderParameterType.Float),
                    new(MaterialKeys.Reflectivity, ShaderParameterType.Float),
                    new(MaterialKeys.Tiling, ShaderParameterType.Vector2),
                    new(MaterialKeys.Offset, ShaderParameterType.Vector2))
                .WithTextureSlots(
                    MaterialKeys.DiffuseTex,
                    MaterialKeys.SpecularTex,
                    MaterialKeys.AmbientTex,
                    MaterialKeys.EmissionTex,
                    MaterialKeys.HeightTex,
                    MaterialKeys.NormalTex,
                    MaterialKeys.OpacityTex,
                    MaterialKeys.DisplacementTex,
                    MaterialKeys.LightmapTex,
                    MaterialKeys.ReflectionTex,
                    MaterialKeys.AmbientOcclusionTex));

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
                .WithTextureSlot(
                    MaterialKeys.SkyboxTex));
        
        context.SetResource(Graphics.BlitColorShaderProgramId,
            new GLSLProgram { Name = "nagule.common.blit_color" }
                .WithShaders(
                    new(ShaderType.Vertex, quadVertShader),
                    new(ShaderType.Fragment, LoadShader("nagule.common.blit_color.frag.glsl")))
                .WithTextureSlot("ColorBuffer"));

        context.SetResource(Graphics.BlitDepthShaderProgramId,
            new GLSLProgram { Name = "nagule.common.blit_depth" }
                .WithShaders(
                    new(ShaderType.Vertex, quadVertShader),
                    new(ShaderType.Fragment, LoadShader("nagule.common.blit_depth.frag.glsl")))
                .WithTextureSlot("DepthBuffer"));
        
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
                .WithTextureSlot("LastMip"));
        
        context.SetResource(Graphics.TransparencyComposeShaderProgramId,
            new GLSLProgram { Name = "nagule.pipeline.transparency_compose" }
                .WithShaders(
                    new(ShaderType.Vertex, quadVertShader),
                    new(ShaderType.Fragment, LoadShader("nagule.pipeline.transparency_compose.frag.glsl")))
                .WithTextureSlots("AccumTex", "RevealTex"));
        
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
                .WithTextureSlots(
                    "ColorBuffer",
                    "TransparencyAccumBuffer",
                    "TransparencyRevealBuffer")
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