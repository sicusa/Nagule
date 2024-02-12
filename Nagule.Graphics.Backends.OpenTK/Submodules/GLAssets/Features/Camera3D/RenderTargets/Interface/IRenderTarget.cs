namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public interface IRenderTarget
{
    (int, int) ViewportSize { get; }

    void OnInitialize(World world, EntityRef cameraEntity);
    void OnUninitailize(World world, EntityRef cameraEntity);
    void Blit(ProgramHandle program, TextureHandle texture);
}