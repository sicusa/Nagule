namespace Nagule.Graphics.Backend.OpenTK;

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

        // load default shader program

        var resource = new ShaderProgramResource();
        var blinnPhongVert = LoadShader("blinn_phong.vert.glsl");

        resource.Shaders[ShaderType.Vertex] = blinnPhongVert;
        resource.Shaders[ShaderType.Fragment] = LoadShader("blinn_phong.frag.glsl");

        ref var program = ref context.Acquire<ShaderProgram>(Graphics.DefaultOpaqueProgramId);
        program.Resource = resource;
        Console.WriteLine("Default shader program loaded: " + Graphics.DefaultOpaqueProgramId);

        // load default transparent shader program

        resource = new ShaderProgramResource();

        resource.Shaders[ShaderType.Vertex] = blinnPhongVert;
        resource.Shaders[ShaderType.Fragment] = LoadShader("blinn_phong_transparent.frag.glsl");

        program = ref context.Acquire<ShaderProgram>(Graphics.DefaultTransparentShaderProgramId);
        program.Resource = resource;
        Console.WriteLine("Default transparent shader program loaded: " + Graphics.DefaultTransparentShaderProgramId);

        // load default cutoff shader program

        resource = new ShaderProgramResource {
            CustomParameters = new[] {
                ("Threshold", ShaderParameterType.Float),
            }
        };

        resource.Shaders[ShaderType.Vertex] = blinnPhongVert;
        resource.Shaders[ShaderType.Fragment] = LoadShader("blinn_phong_cutoff.frag.glsl");

        program = ref context.Acquire<ShaderProgram>(Graphics.DefaultCutoffShaderProgramId);
        program.Resource = resource;
        Console.WriteLine("Default cutoff shader program loaded: " + Graphics.DefaultCutoffShaderProgramId);

        // load culling shader program

        resource = new ShaderProgramResource {
            IsMaterialTexturesEnabled = false
        };

        resource.Shaders[ShaderType.Vertex] = LoadShader("nagule.pipeline.cull.vert.glsl");
        resource.Shaders[ShaderType.Geometry] = LoadShader("nagule.pipeline.cull.geo.glsl");
        resource.TransformFeedbackVaryings = new string[] { "CulledObjectToWorld" };

        program = ref context.Acquire<ShaderProgram>(Graphics.CullingShaderProgramId);
        program.Resource = resource;
        Console.WriteLine("Culling shader program loaded: " + Graphics.CullingShaderProgramId);

        // load hierarchical-Z shader program

        resource = new ShaderProgramResource {
            IsMaterialTexturesEnabled = false,
            CustomParameters = new[] {
                ("LastMip", ShaderParameterType.Texture),
                ("LastMipSize", ShaderParameterType.IntVector2)
            }
        };

        resource.Shaders[ShaderType.Vertex] = emptyVertShader;
        resource.Shaders[ShaderType.Geometry] = quadGeoShader;
        resource.Shaders[ShaderType.Fragment] = LoadShader("nagule.pipeline.hiz.frag.glsl");

        program = ref context.Acquire<ShaderProgram>(Graphics.HierarchicalZShaderProgramId);
        program.Resource = resource;
        Console.WriteLine("Hierarchical-Z shader program loaded: " + Graphics.HierarchicalZShaderProgramId);

        // transparency compose shader program

        resource = new ShaderProgramResource {
            IsMaterialTexturesEnabled = false,
            CustomParameters = new[] {
                ("AccumTex", ShaderParameterType.Texture),
                ("RevealTex", ShaderParameterType.Texture)
            }
        };

        resource.Shaders[ShaderType.Vertex] = emptyVertShader;
        resource.Shaders[ShaderType.Geometry] = quadGeoShader;
        resource.Shaders[ShaderType.Fragment] = LoadShader("nagule.pipeline.transparency_compose.frag.glsl");

        program = ref context.Acquire<ShaderProgram>(Graphics.TransparencyComposeShaderProgramId);
        program.Resource = resource;
        Console.WriteLine("Transparency compose shader program loaded: " + Graphics.TransparencyComposeShaderProgramId);

        // load post-processing shader program

        resource = new ShaderProgramResource {
            IsMaterialTexturesEnabled = false
        };

        resource.Shaders[ShaderType.Vertex] = emptyVertShader;
        resource.Shaders[ShaderType.Geometry] = quadGeoShader;
        resource.Shaders[ShaderType.Fragment] = LoadShader("nagule.pipeline.post.frag.glsl");

        program = ref context.Acquire<ShaderProgram>(Graphics.PostProcessingShaderProgramId);
        program.Resource = resource;
        Console.WriteLine("Post-processing shader program loaded: " + Graphics.PostProcessingShaderProgramId);

        // load debugging post-processing shader program

        resource = new ShaderProgramResource {
            IsMaterialTexturesEnabled = false,
            CustomParameters = new[] {
                ("ColorBuffer", ShaderParameterType.Texture),
                ("TransparencyAccumBuffer", ShaderParameterType.Texture),
                ("TransparencyRevealBuffer", ShaderParameterType.Texture)
            },
            Subroutines = new() {
                [ShaderType.Fragment] = new[] {
                    "ShowColor",
                    "ShowTransparencyAccum",
                    "ShowTransparencyReveal",
                    "ShowDepth",
                    "ShowClusters"
                }
            }
        };

        resource.Shaders[ShaderType.Vertex] = emptyVertShader;
        resource.Shaders[ShaderType.Geometry] = quadGeoShader;
        resource.Shaders[ShaderType.Fragment] = LoadShader("nagule.pipeline.post_debug.frag.glsl");

        program = ref context.Acquire<ShaderProgram>(Graphics.DebugPostProcessingShaderProgramId);
        program.Resource = resource;
        Console.WriteLine("Post-processing debug shader program loaded: " + Graphics.DebugPostProcessingShaderProgramId);
    }
}