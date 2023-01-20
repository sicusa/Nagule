namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics.OpenGL;

using Aeco;

using Nagule.Graphics;

using ShaderType = Nagule.Graphics.ShaderType;

public class MaterialManager : ResourceManagerBase<Material>
{
    private class InitializeCommand : Command<InitializeCommand, RenderTarget>
    {
        public Guid MaterialId;
        public Material? Resource;
        public Guid ShaderProgramId;
        public Guid DepthShaderProgramId;
        public EnumArray<TextureType, Guid?>? Textures;

        public override Guid? Id => MaterialId;

        public unsafe override void Execute(ICommandContext context)
        {
            ref var data = ref context.Acquire<MaterialData>(MaterialId, out bool exists);

            if (!exists) {
                data.Handle = GL.GenBuffer();
                GL.BindBuffer(BufferTargetARB.UniformBuffer, data.Handle);
                data.Pointer = GLHelper.InitializeBuffer(
                    BufferTargetARB.UniformBuffer, MaterialParameters.MemorySize);
            }

            data.IsTwoSided = Resource!.IsTwoSided;
            data.ShaderProgramId = ShaderProgramId;
            data.DepthShaderProgramId = DepthShaderProgramId;
            data.Textures = Textures!;

            var pars = context.Acquire<MaterialSettings>(MaterialId, out var settingsExists).Parameters;
            if (settingsExists) { pars.Clear(); }

            if (Resource.CustomParameters.Count != 0) {
                foreach (var (name, value) in Resource.CustomParameters) {
                    pars.Add(name, value);
                }
            }

            *((MaterialParameters*)data.Pointer) = Resource!.Parameters;
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand, RenderTarget>
    {
        public Guid MaterialId;

        public override void Execute(ICommandContext context)
        {
            if (context.Remove<MaterialData>(MaterialId, out var data)) {
                GL.DeleteBuffer(data.Handle);
            }
        }
    }

    private readonly string EmptyFragmentShader = "#version 410 core\nvoid main() { }";

    protected override void Initialize(
        IContext context, Guid id, Material resource, Material? prevResource)
    {
        if (prevResource != null) {
            UnreferenceDependencies(context, id);
        }

        var cmd = InitializeCommand.Create();
        cmd.MaterialId = id;
        cmd.Resource = resource;

        ShaderProgram programResource;

        if (resource.ShaderProgram != null) {
            programResource = resource.ShaderProgram;
            cmd.ShaderProgramId =
                ResourceLibrary<ShaderProgram>.Reference(context, id, programResource);
        }
        else {
            cmd.ShaderProgramId =
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
            programResource = context.Inspect<Resource<ShaderProgram>>(cmd.ShaderProgramId).Value;
        }

        cmd.DepthShaderProgramId =
            ResourceLibrary<ShaderProgram>.Reference(
                context, id, programResource.WithShader(ShaderType.Fragment, EmptyFragmentShader));

        var textures = new EnumArray<TextureType, Guid?>();
        foreach (var (type, texture) in resource.Textures) {
            textures[(int)type] = ResourceLibrary<Texture>.Reference(context, id, texture);
        }
        cmd.Textures = textures;

        context.SendCommandBatched(cmd);
    }

    protected override void Uninitialize(IContext context, Guid id, Material resource)
    {
        UnreferenceDependencies(context, id);

        var cmd = UninitializeCommand.Create();
        cmd.MaterialId = id;
        context.SendCommandBatched(cmd);
    }

    private void UnreferenceDependencies(IContext context, Guid id)
    {
        ResourceLibrary<ShaderProgram>.UnreferenceAll(context, id);
        ResourceLibrary<Texture>.UnreferenceAll(context, id);
    }
}