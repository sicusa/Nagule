namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Immutable;

using Aeco;

using Nagule.Graphics;

public class EmbededShaderProgramsLoader : VirtualLayer, ILoadListener
{
    private static string LoadShader(string resourceId)
        => InternalAssets.LoadText("Nagule.Graphics.Backend.OpenTK.Embeded.Shaders." + resourceId);

    public void OnLoad(IContext context)
    {
        var emptyVertShader = LoadShader("nagule.utils.empty.vert.glsl");
        var simpleVertShader = LoadShader("nagule.utils.simple.vert.glsl");
        var whiteFragShader = LoadShader("nagule.utils.white.frag.glsl");
        var quadGeoShader = LoadShader("nagule.utils.quad.geo.glsl");
        var blinnPhongVert = LoadShader("blinn_phong.vert.glsl");

        // load default shader program

        var resource = new ShaderProgramResource()
            .WithShaders(
                KeyValuePair.Create(ShaderType.Vertex, blinnPhongVert),
                KeyValuePair.Create(ShaderType.Fragment, LoadShader("blinn_phong.frag.glsl")));

        ref var program = ref context.Acquire<ShaderProgram>(Graphics.DefaultOpaqueProgramId);
        program.Resource = resource;
        Console.WriteLine("Default shader program loaded: " + Graphics.DefaultOpaqueProgramId);

        // load default transparent shader program

        resource = new ShaderProgramResource()
            .WithShaders(
                KeyValuePair.Create(ShaderType.Vertex, blinnPhongVert),
                KeyValuePair.Create(ShaderType.Fragment, LoadShader("blinn_phong_transparent.frag.glsl")));

        program = ref context.Acquire<ShaderProgram>(Graphics.DefaultTransparentShaderProgramId);
        program.Resource = resource;
        Console.WriteLine("Default transparent shader program loaded: " + Graphics.DefaultTransparentShaderProgramId);

        // load default cutoff shader program

        resource = new ShaderProgramResource()
            .WithShaders(
                KeyValuePair.Create(ShaderType.Vertex, blinnPhongVert),
                KeyValuePair.Create(ShaderType.Fragment, LoadShader("blinn_phong_cutoff.frag.glsl")))
            .WithParameter("Threshold", ShaderParameterType.Float);

        program = ref context.Acquire<ShaderProgram>(Graphics.DefaultCutoffShaderProgramId);
        program.Resource = resource;
        Console.WriteLine("Default cutoff shader program loaded: " + Graphics.DefaultCutoffShaderProgramId);

        // load culling shader program

        resource = ShaderProgramResource.NonMaterial
            .WithShaders(
                KeyValuePair.Create(ShaderType.Vertex, LoadShader("nagule.pipeline.cull.vert.glsl")),
                KeyValuePair.Create(ShaderType.Geometry, LoadShader("nagule.pipeline.cull.geo.glsl")))
            .WithTransformFeedbackVarying("CulledObjectToWorld");

        program = ref context.Acquire<ShaderProgram>(Graphics.CullingShaderProgramId);
        program.Resource = resource;
        Console.WriteLine("Culling shader program loaded: " + Graphics.CullingShaderProgramId);

        // load hierarchical-Z shader program

        resource = ShaderProgramResource.NonMaterial
            .WithShaders(
                KeyValuePair.Create(ShaderType.Vertex, emptyVertShader),
                KeyValuePair.Create(ShaderType.Geometry, quadGeoShader),
                KeyValuePair.Create(ShaderType.Fragment, LoadShader("nagule.pipeline.hiz.frag.glsl")))
            .WithParameters(
                KeyValuePair.Create("LastMip", ShaderParameterType.Texture),
                KeyValuePair.Create("LastMipSize", ShaderParameterType.IntVector2));

        program = ref context.Acquire<ShaderProgram>(Graphics.HierarchicalZShaderProgramId);
        program.Resource = resource;
        Console.WriteLine("Hierarchical-Z shader program loaded: " + Graphics.HierarchicalZShaderProgramId);

        // transparency compose shader program

        resource = ShaderProgramResource.NonMaterial
            .WithShaders(
                KeyValuePair.Create(ShaderType.Vertex, emptyVertShader),
                KeyValuePair.Create(ShaderType.Geometry, quadGeoShader),
                KeyValuePair.Create(ShaderType.Fragment, LoadShader("nagule.pipeline.transparency_compose.frag.glsl")))
            .WithParameters(
                KeyValuePair.Create("AccumTex", ShaderParameterType.Texture),
                KeyValuePair.Create("RevealTex", ShaderParameterType.Texture));

        program = ref context.Acquire<ShaderProgram>(Graphics.TransparencyComposeShaderProgramId);
        program.Resource = resource;
        Console.WriteLine("Transparency compose shader program loaded: " + Graphics.TransparencyComposeShaderProgramId);

        // load post-processing shader program

        resource = ShaderProgramResource.NonMaterial
            .WithShaders(
                KeyValuePair.Create(ShaderType.Vertex, emptyVertShader),
                KeyValuePair.Create(ShaderType.Geometry, quadGeoShader),
                KeyValuePair.Create(ShaderType.Fragment, LoadShader("nagule.pipeline.post.frag.glsl")));

        program = ref context.Acquire<ShaderProgram>(Graphics.PostProcessingShaderProgramId);
        program.Resource = resource;
        Console.WriteLine("Post-processing shader program loaded: " + Graphics.PostProcessingShaderProgramId);

        // load debugging post-processing shader program

        resource = ShaderProgramResource.NonMaterial
            .WithShaders(
                KeyValuePair.Create(ShaderType.Vertex, emptyVertShader),
                KeyValuePair.Create(ShaderType.Geometry, quadGeoShader),
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
                    "ShowClusters"));

        program = ref context.Acquire<ShaderProgram>(Graphics.DebugPostProcessingShaderProgramId);
        program.Resource = resource;
        Console.WriteLine("Post-processing debug shader program loaded: " + Graphics.DebugPostProcessingShaderProgramId);
    }
}