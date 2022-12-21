namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Concurrent;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Aeco;

using Nagule.Graphics;

using ShaderType = Nagule.Graphics.ShaderType;

public class MaterialManager : ResourceManagerBase<Material, MaterialData>, IRenderListener
{
    private ConcurrentQueue<(bool, Guid, Material)> _commandQueue = new();

    private readonly string EmptyFragmentShader = "#version 410 core\nvoid main() { }";

    protected override void Initialize(
        IContext context, Guid id, Material resource, ref MaterialData data, bool updating)
    {
        if (updating) {
            Uninitialize(context, id, resource, in data);
        }

        if (resource.Name != "") {
            context.Acquire<Name>(id).Value = resource.Name;
        }

        ShaderProgram programResource;

        if (resource.ShaderProgram != null) {
            programResource = resource.ShaderProgram;
            data.ShaderProgramId =
                ResourceLibrary<ShaderProgram>.Reference(context, programResource, id);
        }
        else {
            data.ShaderProgramId =
                resource.RenderMode switch {
                    RenderMode.Opaque => Graphics.DefaultOpaqueProgramId,
                    RenderMode.Additive => Graphics.DefaultOpaqueProgramId,
                    RenderMode.Multiplicative => Graphics.DefaultOpaqueProgramId,
                    RenderMode.Transparent => Graphics.DefaultTransparentShaderProgramId,
                    RenderMode.Cutoff => Graphics.DefaultCutoffShaderProgramId,
                    _ => throw new NotSupportedException("Material mode not supported")
                };
            programResource = context.Inspect<Resource<ShaderProgram>>(data.ShaderProgramId).Value!;
        }

        data.DepthShaderProgramId =
            ResourceLibrary<ShaderProgram>.Reference(
                context, programResource.WithShader(ShaderType.Fragment, EmptyFragmentShader), id);

        var textureReferences = new EnumArray<TextureType, Guid?>();
        foreach (var (type, texRes) in resource.Textures) {
            textureReferences[(int)type] =
                ResourceLibrary<Texture>.Reference(context, texRes, id);
        }
        data.Textures = textureReferences;
        data.IsTwoSided = resource.IsTwoSided;

        var pars = context.Acquire<MaterialSettings>(id, out var settingsExists).Parameters;
        if (settingsExists) { pars.Clear(); }

        if (resource.CustomParameters.Count != 0) {
            foreach (var (name, value) in resource.CustomParameters) {
                pars.Add(name, value);
            }
        }

        _commandQueue.Enqueue((true, id, resource));
    }

    protected override void Uninitialize(IContext context, Guid id, Material resource, in MaterialData data)
    {
        ResourceLibrary<ShaderProgram>.Unreference(context, data.ShaderProgramId, id);
        ResourceLibrary<ShaderProgram>.Unreference(context, data.DepthShaderProgramId, id);

        var textures = data.Textures;
        if (textures != null) {
            for (int i = 0; i != (int)TextureType.Unknown; ++i) {
                var texId = textures[i];
                if (texId != null) {
                    ResourceLibrary<Texture>.Unreference(context, texId.Value, id);
                }
            }
        }
        _commandQueue.Enqueue((false, id, resource));
    }

    public unsafe void OnRender(IContext context, float deltaTime)
    {
        while (_commandQueue.TryDequeue(out var command)) {
            var (commandType, id, resource) = command;
            ref var data = ref context.Require<MaterialData>(id);

            if (commandType) {
                data.Handle = GL.GenBuffer();
                GL.BindBuffer(BufferTargetARB.UniformBuffer, data.Handle);
                data.Pointer = GLHelper.InitializeBuffer(BufferTargetARB.UniformBuffer, MaterialParameters.MemorySize);
                *((MaterialParameters*)data.Pointer) = resource.Parameters;
            }
            else {
                GL.DeleteBuffer(data.Handle);
            }
        }
    }
}