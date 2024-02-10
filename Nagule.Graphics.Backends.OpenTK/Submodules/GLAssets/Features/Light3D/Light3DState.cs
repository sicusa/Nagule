namespace Nagule.Graphics.Backends.OpenTK;

public struct Light3DState : IAssetState
{
    public readonly bool Loaded => Type != LightType.None;

    public bool IsEnabled;
    public LightType Type;
    public int Index;
}