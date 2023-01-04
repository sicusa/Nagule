namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Concurrent;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Nagule.Graphics;

public class CubemapManager : ResourceManagerBase<Cubemap, CubemapData>, IRenderListener
{
    private ConcurrentQueue<(bool, Guid, Cubemap)> _commandQueue = new();
    private ConcurrentQueue<(Guid, TextureHandle)> _uiTextures = new();
    private float[] _tempBorderColor = new float[4];

    public override void OnUpdate(IContext context)
    {
        base.OnUpdate(context);

        while (_uiTextures.TryDequeue(out var tuple)) {
            var (id, handle) = tuple;
            context.Acquire<ImGuiTextureId>(id).Value = (IntPtr)(int)handle;
        }
    }

    protected override void Initialize(
        IContext context, Guid id, Cubemap resource, ref CubemapData data, bool updating)
    {
        if (updating) {
            Uninitialize(context, id, resource, in data);
        }
        _commandQueue.Enqueue((true, id, resource));
    }

    protected override void Uninitialize(IContext context, Guid id, Cubemap resource, in CubemapData data)
    {
        _commandQueue.Enqueue((false, id, resource));
    }

    public unsafe void OnRender(IContext context)
    {
        while (_commandQueue.TryDequeue(out var command)) {
            var (commandType, id, resource) = command;
            ref var data = ref context.Require<CubemapData>(id);

            if (commandType) {
                data.Handle = GL.GenTexture();
                GL.BindTexture(TextureTarget.TextureCubeMap, data.Handle);

                foreach (var (target, image) in resource.Images) {
                    var textureTarget = TextureHelper.Cast(target);
                    GLHelper.TexImage2D(textureTarget, resource.Type,
                        image.PixelFormat, image.Width, image.Height, image.Bytes.AsSpan());
                }

                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, TextureHelper.Cast(resource.WrapU));
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, TextureHelper.Cast(resource.WrapV));
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapR, TextureHelper.Cast(resource.WrapW));
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, TextureHelper.Cast(resource.MinFilter));
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, TextureHelper.Cast(resource.MaxFilter));

                resource.BorderColor.CopyTo(_tempBorderColor);
                GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureBorderColor, _tempBorderColor);

                if (resource.MipmapEnabled) {
                    GL.GenerateMipmap(TextureTarget.Texture2d);
                }
                GL.BindTexture(TextureTarget.Texture2d, TextureHandle.Zero);

                if (resource.Type == TextureType.UI) {
                    _uiTextures.Enqueue((id, data.Handle));
                }
            }
            else {
                GL.DeleteTexture(data.Handle);
            }
        }
    }
}