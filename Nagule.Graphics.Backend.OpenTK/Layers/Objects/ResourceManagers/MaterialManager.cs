namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics.OpenGL;

using Aeco;

using Nagule.Graphics;

using ShaderType = Nagule.Graphics.ShaderType;

public class MaterialManager : ResourceManagerBase<Material, MaterialData>
{
    private class InitializeCommand : Command<InitializeCommand>
    {
        public Guid MaterialId;
        public Material? Resource;

        public unsafe override void Execute(IContext context)
        {
            ref var data = ref context.Require<MaterialData>(MaterialId);

            data.Handle = GL.GenBuffer();
            GL.BindBuffer(BufferTargetARB.UniformBuffer, data.Handle);

            data.Pointer = GLHelper.InitializeBuffer(BufferTargetARB.UniformBuffer, MaterialParameters.MemorySize);
            *((MaterialParameters*)data.Pointer) = Resource!.Parameters;

            context.SendResourceValidCommand(MaterialId);
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand>
    {
        public MaterialData MaterialData;

        public override void Execute(IContext context)
        {
            GL.DeleteBuffer(MaterialData.Handle);
        }
    }

    private readonly string EmptyFragmentShader = "#version 410 core\nvoid main() { }";

    protected override void Initialize(
        IContext context, Guid id, Material resource, ref MaterialData data, bool updating)
    {
        if (updating) {
            context.SendResourceInvalidCommand(id);
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
                    RenderMode.Opaque => Graphics.DefaultOpaqueShaderProgramId,
                    RenderMode.Transparent => Graphics.DefaultTransparentShaderProgramId,
                    RenderMode.Cutoff => Graphics.DefaultCutoffShaderProgramId,
                    RenderMode.Additive => Graphics.DefaultOpaqueShaderProgramId,
                    RenderMode.Multiplicative => Graphics.DefaultOpaqueShaderProgramId,

                    RenderMode.Unlit => Graphics.DefaultUnlitShaderProgramId,
                    RenderMode.UnlitTransparent => Graphics.DefaultUnlitTransparentShaderProgramId,
                    RenderMode.UnlitCutoff => Graphics.DefaultUnlitCutoffShaderProgramId,
                    RenderMode.UnlitAdditive => Graphics.DefaultUnlitShaderProgramId,
                    RenderMode.UnlitMultiplicative => Graphics.DefaultUnlitShaderProgramId,

                    _ => throw new NotSupportedException("Material render mode not supported")
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

        var cmd = InitializeCommand.Create();
        cmd.MaterialId = id;
        cmd.Resource = resource;
        context.SendCommand<GraphicsResourceTarget>(cmd);
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
        var cmd = UninitializeCommand.Create();
        cmd.MaterialData = data;
        context.SendCommand<GraphicsResourceTarget>(cmd);
    }
}