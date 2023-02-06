namespace Nagule.Graphics;

public static class GraphicsHelper
{
    public static string LoadEmbededShader(string resourceId)
        => EmbededAssets.LoadText("Nagule.Graphics.Embeded.Shaders." + resourceId);

    public static readonly string EmptyFragmentShader = "#version 410 core\nvoid main() { }";

    public static void LoadEmbededShaderPrograms(IContext context)
    {
        var panoramaVertShader = LoadEmbededShader("nagule.common.panorama.vert.glsl");
        var simpleVertShader = LoadEmbededShader("nagule.common.simple.vert.glsl");

        context.SetResource(Graphics.DefaultShaderProgramId,
            new GLSLProgram { Name = "nagule.blinn_phong" }
                .WithShaders(
                    new(ShaderType.Vertex, LoadEmbededShader("blinn_phong.vert.glsl")),
                    new(ShaderType.Fragment, LoadEmbededShader("blinn_phong.frag.glsl")))
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
                    new(ShaderType.Fragment, LoadEmbededShader("skybox_cubemap.frag.glsl")))
                .WithParameter(new(MaterialKeys.SkyboxTex)));
        
    }
}