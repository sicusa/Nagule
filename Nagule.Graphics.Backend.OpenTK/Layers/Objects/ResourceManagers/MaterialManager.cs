namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Concurrent;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Aeco;

using Nagule.Graphics;

using ShaderType = Nagule.Graphics.ShaderType;

public class MaterialManager : ResourceManagerBase<Material, MaterialData, MaterialResource>, IRenderListener
{
    private ConcurrentQueue<(bool, Guid)> _commandQueue = new();

    private readonly string EmptyFragmentShader = "#version 410 core\nvoid main() { }";

    protected override void Initialize(
        IContext context, Guid id, ref Material material, ref MaterialData data, bool updating)
    {
        if (updating) {
            Uninitialize(context, id, in material, in data);
        }

        var materialRes = material.Resource;
        if (materialRes.Name != "") {
            context.Acquire<Name>(id).Value = materialRes.Name;
        }

        ShaderProgramResource programResource;

        if (material.ShaderProgram != null) {
            programResource = material.ShaderProgram;
            data.ShaderProgramId =
                ResourceLibrary<ShaderProgramResource>.Reference<ShaderProgram>(context, programResource, id);
        }
        else {
            data.ShaderProgramId =
                material.Resource.RenderMode switch {
                    RenderMode.Opaque => Graphics.DefaultOpaqueProgramId,
                    RenderMode.Additive => Graphics.DefaultOpaqueProgramId,
                    RenderMode.Multiplicative => Graphics.DefaultOpaqueProgramId,
                    RenderMode.Transparent => Graphics.DefaultTransparentShaderProgramId,
                    RenderMode.Cutoff => Graphics.DefaultCutoffShaderProgramId,
                    _ => throw new NotSupportedException("Material mode not supported")
                };
            programResource = context.Inspect<ShaderProgram>(data.ShaderProgramId).Resource;
        }

        data.DepthShaderProgramId =
            ResourceLibrary<ShaderProgramResource>.Reference<ShaderProgram>(
                context, programResource.WithShader(ShaderType.Fragment, EmptyFragmentShader), id);

        var textureReferences = new EnumArray<TextureType, Guid?>();
        foreach (var (type, texRes) in material.Resource.Textures) {
            textureReferences[(int)type] =
                ResourceLibrary<TextureResource>.Reference<Texture>(context, texRes, id);
        }
        data.Textures = textureReferences;
        data.IsTwoSided = materialRes.IsTwoSided;

        var pars = context.Acquire<MaterialSettings>(id, out var settingsExists).Parameters;
        if (settingsExists) { pars.Clear(); }

        if (materialRes.CustomParameters.Count != 0) {
            foreach (var (name, value) in materialRes.CustomParameters) {
                pars.Add(name, value);
            }
        }

        _commandQueue.Enqueue((true, id));
    }

    protected override void Uninitialize(IContext context, Guid id, in Material material, in MaterialData data)
    {
        ResourceLibrary<ShaderProgramResource>.Unreference(context, data.ShaderProgramId, id);
        var textures = data.Textures;
        if (textures != null) {
            for (int i = 0; i != (int)TextureType.Unknown; ++i) {
                var texId = textures[i];
                if (texId != null) {
                    ResourceLibrary<TextureResource>.Unreference(context, texId.Value, id);
                }
            }
        }
        _commandQueue.Enqueue((false, id));
    }

    public unsafe void OnRender(IContext context, float deltaTime)
    {
        while (_commandQueue.TryDequeue(out var command)) {
            var (commandType, id) = command;
            ref var data = ref context.Require<MaterialData>(id);

            if (commandType) {
                var resource = context.Inspect<Material>(id).Resource;
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