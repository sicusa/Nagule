namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Concurrent;

using Nagule.Graphics;

public class CubemapManager : ResourceManagerBase<Cubemap>
{
    private class InitializeCommand : Command<InitializeCommand, GraphicsResourceTarget>
    {
        public CubemapManager? Sender;
        public uint CubemapId;
        public Cubemap? Resource;
        public CancellationToken Token;

        private float[] _tempBorderColor = new float[4];

        public override void Execute(ICommandHost host)
        {
            var data = new TextureData();

            data.Handle = GL.GenTexture();
            GL.BindTexture(TextureTarget.TextureCubeMap, data.Handle);

            foreach (var (target, image) in Resource!.Images) {
                var textureTarget = TextureHelper.Cast(target);
                GLHelper.TexImage2D(textureTarget, Resource.Type, image);
            }

            GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, TextureHelper.Cast(Resource.WrapU));
            GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, TextureHelper.Cast(Resource.WrapV));
            GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, TextureHelper.Cast(Resource.WrapW));
            GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, TextureHelper.Cast(Resource.MinFilter));
            GL.TexParameteri(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, TextureHelper.Cast(Resource.MaxFilter));

            Resource.BorderColor.CopyTo(_tempBorderColor);
            GL.TexParameterf(TextureTarget.TextureCubeMap, TextureParameterName.TextureBorderColor, _tempBorderColor);

            if (Resource.MipmapEnabled) {
                GL.GenerateMipmap(TextureTarget.TextureCubeMap);
            }
            GL.BindTexture(TextureTarget.TextureCubeMap, TextureHandle.Zero);

            if (Resource.Type == TextureType.UI) {
                Sender!._uiTextures.Enqueue((CubemapId, data.Handle));
            }

            host.SendRenderData(CubemapId, data, Token,
                (id, data) => GL.DeleteTexture(data.Handle));
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand, RenderTarget>
    {
        public uint CubemapId;

        public override void Execute(ICommandHost host)
        {
            if (host.Remove<TextureData>(CubemapId, out var data)) {
                GL.DeleteTexture(data.Handle);
            }
        }
    }

    private ConcurrentQueue<(uint, TextureHandle)> _uiTextures = new();

    public override void OnResourceUpdate(IContext context)
    {
        base.OnResourceUpdate(context);

        while (_uiTextures.TryDequeue(out var tuple)) {
            var (id, handle) = tuple;
            context.Acquire<ImGuiTextureId>(id).Value = (IntPtr)(int)handle;
        }
    }

    protected override void Initialize(
        IContext context, uint id, Cubemap resource, Cubemap? prevResource)
    {
        if (prevResource != null) {
            Uninitialize(context, id, prevResource);
        }
        var cmd = InitializeCommand.Create();
        cmd.Sender = this;
        cmd.CubemapId = id;
        cmd.Resource = resource;
        cmd.Token = context.GetLifetimeToken(id);
        context.SendCommandBatched(cmd);
    }

    protected override void Uninitialize(IContext context, uint id, Cubemap resource)
    {
        var cmd = UninitializeCommand.Create();
        cmd.CubemapId = id;
        context.SendCommandBatched(cmd);
    }
}