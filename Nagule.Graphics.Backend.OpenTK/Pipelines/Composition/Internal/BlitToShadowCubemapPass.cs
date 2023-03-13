namespace Nagule.Graphics.Backend.OpenTK;

public record BlitToShadowCubemapPass : CompositionPass
{
    public required CubemapArrayPool ArrayPool { get; init; }
    public required int Index { get; init; }

    private class PassImpl : CompositionPassImplBase
    {
        private BlitToShadowCubemapPass _pass;

        public PassImpl(BlitToShadowCubemapPass pass) { _pass = pass; }

        public override void Initialize(ICommandHost host, ICompositionPipeline pipeline)
        {
            
        }
    }

    public ICompositionPass CreateImpl() => new PassImpl(this);
}