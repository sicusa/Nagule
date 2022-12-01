namespace Nagule.Backend.OpenTK.Graphics;

using global::OpenTK.Graphics.OpenGL4;

using Aeco;

using Nagule.Graphics;

public class MaterialManager : ResourceManagerBase<Material, MaterialData, MaterialResource>
{
    protected unsafe override void Initialize(
        IContext context, Guid id, ref Material material, ref MaterialData data, bool updating)
    {
        if (updating) {
            Uninitialize(context, id, in material, in data);
        }

        if (material.Resource.Name != null) {
            context.Acquire<Name>(id).Value = material.Resource.Name;
        }

        data.Handle = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.UniformBuffer, data.Handle);
        data.Pointer = GLHelper.InitializeBuffer(BufferTarget.UniformBuffer, MaterialParameters.MemorySize);

        data.ShaderProgramId =
            material.ShaderProgram != null
                ? ResourceLibrary<ShaderProgramResource>.Reference<ShaderProgram>(context, material.ShaderProgram, id)
                : (material.Resource.IsTransparent
                    ? Graphics.DefaultTransparentShaderProgramId
                    : Graphics.DefaultOpaqueProgramId);

        var resource = material.Resource;
        var textureReferences = new EnumArray<TextureType, Guid?>();

        foreach (var (type, texRes) in resource.Textures) {
            textureReferences[(int)type] =
                ResourceLibrary<TextureResource>.Reference<Texture>(context, texRes, id);
        }
        data.Textures = textureReferences;
        *((MaterialParameters*)data.Pointer) = resource.Parameters;
    }

    protected override void Uninitialize(IContext context, Guid id, in Material material, in MaterialData data)
    {
        GL.DeleteBuffer(data.Handle);
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
    }
}