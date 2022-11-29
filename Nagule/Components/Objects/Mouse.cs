namespace Nagule;

public struct Mouse : ISingletonComponent
{
    public float X = 0;
    public float Y = 0;
    public bool InWindow = true;

    public Mouse() {}
}