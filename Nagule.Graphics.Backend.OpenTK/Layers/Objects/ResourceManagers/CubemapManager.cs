namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Concurrent;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Nagule.Graphics;

public class CubemapManager : ResourceManagerBase<Cubemap, CubemapData>
{
    private class InitializeCommand : Command<InitializeCommand>
    {
        public CubemapManager? Sender;
        public Guid CubemapId;
        public Cubemap? Resource;

        private static float[] s_tempBorderColor = new float[4];

        public override void Execute(IContext context)
        {
            ref var data = ref context.Require<CubemapData>(CubemapId);
            data.Handle = GL.GenTexture();
            GL.BindTexture(TextureTarget.TextureCubeMap, data.Handle);

            foreach (var (target, image) in Resource!.Images) {
                var textureTarget = TextureHelper.Cast(target);
                GLHelper.TexImage2D(textureTarget, Resource.Type,
                    image.PixelFormat, image.Width, image.Height, image.Bytes.AsSpan());
            }

            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, TextureHelper.Cast(Resource.WrapU));
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, TextureHelper.Cast(Resource.WrapV));
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapR, TextureHelper.Cast(Resource.WrapW));
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, TextureHelper.Cast(Resource.MinFilter));
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, TextureHelper.Cast(Resource.MaxFilter));

            Resource.BorderColor.CopyTo(s_tempBorderColor);
            GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureBorderColor, s_tempBorderColor);

            if (Resource.MipmapEnabled) {
                GL.GenerateMipmap(TextureTarget.Texture2d);
            }
            GL.BindTexture(TextureTarget.Texture2d, TextureHandle.Zero);

            if (Resource.Type == TextureType.UI) {
                Sender!._uiTextures.Enqueue((CubemapId, data.Handle));
            }
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand>
    {
        public Guid CubemapId;

        public override void Execute(IContext context)
        {
            ref var data = ref context.Require<CubemapData>(CubemapId);
            GL.DeleteTexture(data.Handle);
        }
    }

    private ConcurrentQueue<(Guid, TextureHandle)> _uiTextures = new();

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

        var cmd = InitializeCommand.Create();
        cmd.Sender = this;
        cmd.CubemapId = id;
        cmd.Resource = resource;
        context.SendCommand<RenderTarget>(cmd);
    }

    protected override void Uninitialize(IContext context, Guid id, Cubemap resource, in CubemapData data)
    {
        var cmd = UninitializeCommand.Create();
        cmd.CubemapId = id;
        context.SendCommand<RenderTarget>(cmd);
    }
}