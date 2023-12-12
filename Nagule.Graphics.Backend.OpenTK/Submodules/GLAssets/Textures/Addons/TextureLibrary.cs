namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class TextureLibrary : IAddon
{
    public IReadOnlyEntityStore<TextureHandle> Handles => HandlesRaw;

    internal readonly EntityStore<TextureHandle> HandlesRaw = new();
}