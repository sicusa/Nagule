namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.CompilerServices;

public class BlitToRenderTexturePassImpl : CompositionPassImplBase, IExecutableCompositionPass
{
    public RenderTexture RenderTexture { get; }

    private uint _renderTextureId;

    public BlitToRenderTexturePassImpl(RenderTexture renderTexture)
    {
        RenderTexture = renderTexture;
    }

    public override void LoadResources(IContext context)
    {
        _renderTextureId = context.GetResourceLibrary().Reference(Id, RenderTexture);
    }

    public void Execute(ICommandHost host, ICompositionPipeline pipeline)
    {
        ref var renderTextureData = ref host.RequireOrNullRef<RenderTextureData>(_renderTextureId);
        if (Unsafe.IsNullRef(ref renderTextureData)) { return; }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, renderTextureData.FramebufferHandle);
        GL.Viewport(0, 0, renderTextureData.Width, renderTextureData.Height);
        pipeline.Blit(host);
    }
}