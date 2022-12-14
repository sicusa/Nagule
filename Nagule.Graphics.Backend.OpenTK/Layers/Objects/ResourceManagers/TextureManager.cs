namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Concurrent;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Nagule.Graphics;

public class TextureManager : ResourceManagerBase<Texture, TextureData>
{
    private class InitializeCommand : Command<InitializeCommand>
    {
        public TextureManager? Sender;
        public Guid TextureId;
        public Texture? Resource;

        public override void Execute(IContext context)
        {
            ref var data = ref context.Require<TextureData>(TextureId);
            data.Handle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2d, data.Handle);

            var image = Resource!.Image ?? Image.Hint;
            GLHelper.TexImage2D(Resource.Type, image.PixelFormat, image.Width, image.Height, image.Bytes.AsSpan());

            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, TextureHelper.Cast(Resource.WrapU));
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, TextureHelper.Cast(Resource.WrapV));
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, TextureHelper.Cast(Resource.MinFilter));
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, TextureHelper.Cast(Resource.MaxFilter));

            Resource.BorderColor.CopyTo(s_tempBorderColor);
            GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureBorderColor, s_tempBorderColor);

            if (Resource.MipmapEnabled) {
                GL.GenerateMipmap(TextureTarget.Texture2d);
            }
            GL.BindTexture(TextureTarget.Texture2d, TextureHandle.Zero);

            if (Resource.Type == TextureType.UI) {
                Sender!._uiTextures.Enqueue((TextureId, data.Handle));
            }

            context.SendResourceValidCommand(TextureId);
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand>
    {
        public TextureData TextureData;

        public override void Execute(IContext context)
        {
            GL.DeleteTexture(TextureData.Handle);
        }
    }

    private ConcurrentQueue<(Guid, TextureHandle)> _uiTextures = new();

    private static float[] s_tempBorderColor = new float[4];

    public override void OnUpdate(IContext context)
    {
        base.OnUpdate(context);

        while (_uiTextures.TryDequeue(out var tuple)) {
            var (id, handle) = tuple;
            context.Acquire<ImGuiTextureId>(id).Value = (IntPtr)(int)handle;
        }
    }

    protected override void Initialize(
        IContext context, Guid id, Texture resource, ref TextureData data, bool updating)
    {
        if (updating) {
            context.SendResourceInvalidCommand(id);
            Uninitialize(context, id, resource, in data);
        }
        var cmd = InitializeCommand.Create();
        cmd.Sender = this;
        cmd.TextureId = id;
        cmd.Resource = resource;
        context.SendCommand<GraphicsResourceTarget>(cmd);
    }

    protected override void Uninitialize(IContext context, Guid id, Texture resource, in TextureData data)
    {
        var cmd = UninitializeCommand.Create();
        cmd.TextureData = data;
        context.SendCommand<GraphicsResourceTarget>(cmd);
    }
}