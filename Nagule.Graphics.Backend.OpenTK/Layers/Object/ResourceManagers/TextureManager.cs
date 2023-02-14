namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Concurrent;

using Nagule.Graphics;

public class TextureManager : ResourceManagerBase<Texture>
{
    private class InitializeCommand : Command<InitializeCommand, GraphicsResourceTarget>
    {
        public TextureManager? Sender;
        public Guid TextureId;
        public Texture? Resource;
        public CancellationToken Token = default;

        private float[] _tempBorderColor = new float[4];

        public override void Execute(ICommandHost host)
        {
            var data = new TextureData();

            data.Handle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2d, data.Handle);

            var image = Resource!.Image;
            if (image != null) {
                GLHelper.TexImage2D(Resource.Type, image);
            }

            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, TextureHelper.Cast(Resource.WrapU));
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, TextureHelper.Cast(Resource.WrapV));
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, TextureHelper.Cast(Resource.MinFilter));
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, TextureHelper.Cast(Resource.MaxFilter));

            Resource.BorderColor.CopyTo(_tempBorderColor);
            GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureBorderColor, _tempBorderColor);

            if (Resource.MipmapEnabled) {
                GL.GenerateMipmap(TextureTarget.Texture2d);
            }
            GL.BindTexture(TextureTarget.Texture2d, TextureHandle.Zero);

            if (Resource.Type == TextureType.UI) {
                Sender!._uiTextures.Enqueue((TextureId, data.Handle));
            }

            host.SendRenderData(TextureId, data, Token,
                (id, data) => GL.DeleteTexture(data.Handle));
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand, RenderTarget>
    {
        public Guid TextureId;

        public override void Execute(ICommandHost host)
        {
            if (host.Remove<TextureData>(TextureId, out var data)) {
                GL.DeleteTexture(data.Handle);
            }
        }
    }

    private ConcurrentQueue<(Guid, TextureHandle)> _uiTextures = new();

    public override void OnResourceUpdate(IContext context)
    {
        base.OnResourceUpdate(context);

        while (_uiTextures.TryDequeue(out var tuple)) {
            var (id, handle) = tuple;
            context.Acquire<ImGuiTextureId>(id).Value = (IntPtr)(int)handle;
        }
    }

    protected override void Initialize(
        IContext context, Guid id, Texture resource, Texture? prevResource)
    {
        if (prevResource != null) {
            Uninitialize(context, id, prevResource);
        }
        var cmd = InitializeCommand.Create();
        cmd.Sender = this;
        cmd.TextureId = id;
        cmd.Resource = resource;
        cmd.Token = context.GetLifetimeToken(id);
        context.SendCommandBatched(cmd);
    }

    protected override void Uninitialize(IContext context, Guid id, Texture resource)
    {
        var cmd = UninitializeCommand.Create();
        cmd.TextureId = id;
        context.SendCommandBatched(cmd);
    }
}