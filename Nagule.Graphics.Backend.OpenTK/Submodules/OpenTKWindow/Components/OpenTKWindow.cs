namespace Nagule.Graphics.Backend.OpenTK;

public record struct OpenTKWindow
{
    public OpenTKNativeWindow Native { get; internal set; }
}