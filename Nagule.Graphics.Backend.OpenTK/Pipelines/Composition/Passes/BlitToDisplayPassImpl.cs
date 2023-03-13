namespace Nagule.Graphics.Backend.OpenTK;

public class BlitToDisplayPassImpl : CompositionPassImplBase, IExecutableCompositionPass
{
    public void Execute(ICommandHost host, ICompositionPipeline pipeline)
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferHandle.Zero);
        GL.Viewport(0, 0, pipeline.ViewportWidth, pipeline.ViewportHeight);
        pipeline.Blit(host);
    }
}