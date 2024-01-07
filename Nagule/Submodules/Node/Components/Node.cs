namespace Nagule;

using Sia;

[SiaTemplate(nameof(Node))]
[NaAsset]
public record RNode : RNodeBase<RNode>
{
    public static RNode Empty { get; } = new();
}

public partial record struct Node : INode<RNode>;