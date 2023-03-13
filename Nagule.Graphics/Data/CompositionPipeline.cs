namespace Nagule.Graphics;

using System.Collections.Immutable;

public record CompositionPipeline
{
    public static CompositionPipeline Default { get; } = new() {
        Passes = ImmutableList.Create<CompositionPass>(
            new CompositionPass.SampleColor(),
            new CompositionPass.ACESToneMapping(),
            new CompositionPass.GammaCorrection(),
            new CompositionPass.BlitToDisplay())
    };

    public static CompositionPipeline SampleColor { get; } = new() {
        Passes = ImmutableList.Create<CompositionPass>(
            new CompositionPass.SampleColor(),
            new CompositionPass.BlitToDisplay())
    };

    public static CompositionPipeline SampleDepth { get; } = new() {
        Passes = ImmutableList.Create<CompositionPass>(
            new CompositionPass.SampleDepth(),
            new CompositionPass.BlitToDisplay())
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