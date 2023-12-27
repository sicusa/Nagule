namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Immutable;
using Sia;

public class PostProcessingEffectPass(ImmutableList<PostProcessingEffect> effects) : RenderPassSystemBase
{
    public ImmutableList<PostProcessingEffect> Effects { get; } = effects;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
    }
}