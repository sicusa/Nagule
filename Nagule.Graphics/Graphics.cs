namespace Nagule.Graphics;

using Aeco;

public static class Graphics
{
    public static uint RootId { get; } = IdFactory.New();
    public static uint DefaultMaterialId { get; } = IdFactory.New();
}