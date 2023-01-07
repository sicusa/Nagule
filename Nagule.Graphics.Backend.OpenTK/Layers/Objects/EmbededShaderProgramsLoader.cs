namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Immutable;

using Aeco;

using Nagule.Graphics;

public class EmbededShaderProgramsLoader : VirtualLayer, ILoadListener
{
    private static string LoadShader(string resourceId)
        => InternalAssets.LoadText("Nagule.Graphics.Backend.OpenTK.Embeded.Shaders." + resourceId);

    private readonly string EmptyFragmentShader = "#version 410 core\nvoid main() { }";

    public void OnLoad(IContext context)
    {
        var emptyVertShader = LoadShader("nagule.utils.empty.vert.glsl");
        var simpleVertShader = LoadShader("nagule.utils.simple.vert.glsl");
        var whiteFragShader = LoadShader("nagule.utils.white.frag.glsl");
        var quadGeoShader = LoadShader("nagule.utils.quad.geo.glsl");
        var blinnPhongVert = LoadShader("blinn_phong.vert.glsl");
        var unlitVert = LoadShader("unlit.vert.glsl");

        // load default opaque shader program

        var resource = new ShaderProgram()
            .WithShaders(
                KeyValuePair.Create(ShaderType.Vertex, blinnPhongVert),
                KeyValuePair.Create(ShaderType.Fragment, LoadShader("blinn_phong.frag.glsl")));

        ref var program = ref context.Acquire<Resource<ShaderProgram>>(Graphics.DefaultOpaqueShaderProgramId);
        program.Value = resource;

        // load default transparent shader program

        resource = new ShaderProgram()
            .WithShaders(
                KeyValuePair.Create(ShaderType.Vertex, blinnPhongVert),
                KeyValuePair.Create(ShaderType.Fragment, LoadShader("blinn_phong_transparent.frag.glsl")));

        program = ref context.Acquire<Resource<ShaderProgram>>(Graphics.DefaultTransparentShaderProgramId);
        program.Value = resource;

        // load default cutoff shader program

        resource = new ShaderProgram()
            .WithShaders(
                KeyValuePair.Create(ShaderType.Vertex, blinnPhongVert),
                KeyValuePair.Create(ShaderType.Fragment, LoadShader("blinn_phong_cutoff.frag.glsl")))
            .WithParameter("Threshold", ShaderParameterType.Float);

        program = ref context.Acquire<Resource<ShaderProgram>>(Graphics.DefaultCutoffShaderProgramId);
        program.Value = resource;

        // load default unlit shader program

        resource = new ShaderProgram()
            .WithShaders(
                KeyValuePair.Create(ShaderType.Vertex, unlitVert),
                KeyValuePair.Create(ShaderType.Fragment, LoadShader("unlit.frag.glsl")));

        program = ref context.Acquire<Resource<ShaderProgram>>(Graphics.DefaultUnlitShaderProgramId);
        program.Value = resource;

        // load default unlit transparent shader program

        resource = new ShaderProgram()
            .WithShaders(
                KeyValuePair.Create(ShaderType.Vertex, unlitVert),
                KeyValuePair.Create(ShaderType.Fragment, LoadShader("unlit_transparent.frag.glsl")));

        program = ref context.Acquire<Resource<ShaderProgram>>(Graphics.DefaultUnlitTransparentShaderProgramId);
        program.Value = resource;

        // load default unlit cutoff shader program

        resource = new ShaderProgram()
            .WithShaders(
                KeyValuePair.Create(ShaderType.Vertex, unlitVert),
                KeyValuePair.Create(ShaderType.Fragment, LoadShader("unlit_cutoff.frag.glsl")))
            .WithParameter("Threshold", ShaderParameterType.Float);

        program = ref context.Acquire<Resource<ShaderProgram>>(Graphics.DefaultUnlitCutoffShaderProgramId);
        program.Value = resource;

        // load default depth shader program

        resource = new ShaderProgram()
            .WithShaders(
                KeyValuePair.Create(ShaderType.Vertex, simpleVertShader),
                KeyValuePair.Create(ShaderType.Fragment, EmptyFragmentShader));

        program = ref context.Acquire<Resource<ShaderProgram>>(Graphics.DefaultDepthShaderProgramId);
        program.Value = resource;

        // load culling shader program

        resource = ShaderProgram.NonMaterial
            .WithShaders(
                KeyValuePair.Create(ShaderType.Vertex, LoadShader("nagule.pipeline.cull.vert.glsl")),
                KeyValuePair.Create(ShaderType.Geometry, LoadShader("nagule.pipeline.cull.geo.glsl")))
            .WithTransformFeedbackVarying("CulledObjectToWorld");

        program = ref context.Acquire<Resource<ShaderProgram>>(Graphics.CullingShaderProgramId);
        program.Value = resource;

        // load occluder culling shader program

        resource = ShaderProgram.NonMaterial
            .WithShaders(
                KeyValuePair.Create(ShaderType.Vertex, LoadShader("nagule.pipeline.cull_occluders.vert.glsl")),
                KeyValuePair.Create(ShaderType.Geometry, LoadShader("nagule.pipeline.cull.geo.glsl")))
            .WithTransformFeedbackVarying("CulledObjectToWorld");

        program = ref context.Acquire<Resource<ShaderProgram>>(Graphics.OccluderCullingShaderProgramId);
        program.Value = resource;

        // load hierarchical-Z shader program

        resource = ShaderProgram.NonMaterial
            .WithShaders(
                KeyValuePair.Create(ShaderType.Vertex, emptyVertShader),
                KeyValuePair.Create(ShaderType.Geometry, quadGeoShader),
                KeyValuePair.Create(ShaderType.Fragment, LoadShader("nagule.pipeline.hiz.frag.glsl")))
            .WithParameters(
                KeyValuePair.Create("LastMip", ShaderParameterType.Texture));

        program = ref context.Acquire<Resource<ShaderProgram>>(Graphics.HierarchicalZShaderProgramId);
        program.Value = resource;

        // transparency compose shader program

        resource = ShaderProgram.NonMaterial
            .WithShaders(
                KeyValuePair.Create(ShaderType.Vertex, emptyVertShader),
                KeyValuePair.Create(ShaderType.Geometry, quadGeoShader),
                KeyValuePair.Create(ShaderType.Fragment, LoadShader("nagule.pipeline.transparency_compose.frag.glsl")))
            .WithParameters(
                KeyValuePair.Create("AccumTex", ShaderParameterType.Texture),
                KeyValuePair.Create("RevealTex", ShaderParameterType.Texture));

        program = ref context.Acquire<Resource<ShaderProgram>>(Graphics.TransparencyComposeShaderProgramId);
        program.Value = resource;

        // load post-processing shader program

        resource = ShaderProgram.NonMaterial
            .WithShaders(
                KeyValuePair.Create(ShaderType.Vertex, emptyVertShader),
                KeyValuePair.Create(ShaderType.Geometry, quadGeoShader),
                KeyValuePair.Create(ShaderType.Fragment, LoadShader("nagule.pipeline.post.frag.glsl")));

        program = ref context.Acquire<Resource<ShaderProgram>>(Graphics.PostProcessingShaderProgramId);
        program.Value = resource;

        // load debugging post-processing shader program

        resource = ShaderProgram.NonMaterial
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

        program = ref context.Acquire<Resource<ShaderProgram>>(Graphics.DebugPostProcessingShaderProgramId);
        program.Value = resource;
    }
}