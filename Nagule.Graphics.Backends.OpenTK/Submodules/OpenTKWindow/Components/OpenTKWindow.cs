namespace Nagule.Graphics.Backends.OpenTK;

public record struct OpenTKWindow
{
    public OpenTKNativeWindow Native { get; internal set; }
}