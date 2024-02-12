namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public interface IGLGraphicsUpdator : IAddon
{
    void WaitSync();
}