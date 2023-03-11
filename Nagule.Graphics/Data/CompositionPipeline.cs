namespace Nagule.Graphics;

using System.Collections.Immutable;

public record CompositionPipeline
{
    public static CompositionPipeline Default { get; } = new() {
        Passes = ImmutableList.Create<CompositionPass>(
            new CompositionPass.BlitColor(),
            new CompositionPass.ACESToneMapping(),
            new CompositionPass.GammaCorrection())
    };

    public static CompositionPipeline BlitColor { get; } = new() {
        Passes = ImmutableList.Create<CompositionPass>(
            new CompositionPass.BlitColor())
    };

    public static CompositionPipeline BlitDepth { get; } = new() {
        Passes = ImmutableList.Create<CompositionPass>(
            new CompositionPass.BlitDepth())
    };

    public ImmutableList<CompositionPass> Passes { get; init; }
        = ImmutableList<CompositionPass>.Empty;

    public CompositionPipeline WithPass(CompositionPass pass)
        => this with { Passes = Passes.Add(pass) };
    public CompositionPipeline WithPasses(params CompositionPass[] passes)
        => this with { Passes = Passes.AddRange(passes) };
    public CompositionPipeline WithPasses(IEnumerable<CompositionPass> passes)
        => this with { Passes = Passes.AddRange(passes) };
}