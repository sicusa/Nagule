namespace Nagule;

using Sia;

[SiaTemplate(nameof(Node))]
[NaAsset<Node>]
public record RNode : RNodeBase<RNode>
{
    public static RNode Empty { get; } = new();
}

public partial record struct Node : INode<RNode>;