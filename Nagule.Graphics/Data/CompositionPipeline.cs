namespace Nagule.Graphics;

using System.Collections.Immutable;

public record CompositionPipeline
{
    public static CompositionPipeline Default { get; } = new() {
        Passes = ImmutableArray.Create<CompositionPass>(
            new CompositionPass.BlitColor(),
            new CompositionPass.Bloom(),
            new CompositionPass.ACESToneMapping(),
            new CompositionPass.GammaCorrection())
    };

    public static CompositionPipeline BlitColor { get; } = new() {
        Passes = ImmutableArray.Create<CompositionPass>(
            new CompositionPass.BlitColor())
    };

    public static CompositionPipeline BlitDepth { get; } = new() {
        Passes = ImmutableArray.Create<CompositionPass>(
            new CompositionPass.BlitDepth())
    };

    public ImmutableArray<CompositionPass> Passes { get; init; }
        = ImmutableArray<CompositionPass>.Empty;
}