namespace Nagule.Graphics;

using System.Runtime.CompilerServices;
using Sia;

public record RenderPassChain() : SystemChain<RenderPassBase>()
{
    public new static readonly RenderPassChain Empty = new();

    public new RenderPassChain Add<TSystem>()
        where TSystem : RenderPassBase, new()
        => Unsafe.As<RenderPassChain>(base.Add<TSystem>());
    
    public new RenderPassChain Add<TSystem>(Func<TSystem> creator)
        where TSystem : RenderPassBase
        => Unsafe.As<RenderPassChain>(base.Add(creator));

    public new RenderPassChain Concat(RenderPassChain chain)
        => Unsafe.As<RenderPassChain>(new SystemChain(Entries.AddRange(chain.Entries)));

    public new RenderPassChain Remove<TSystem>()
        where TSystem : RenderPassBase
        => Unsafe.As<RenderPassChain>(base.Remove<TSystem>());

    public new RenderPassChain RemoveAll<TSystem>()
        where TSystem : RenderPassBase
        => Unsafe.As<RenderPassChain>(base.RemoveAll<TSystem>());
}