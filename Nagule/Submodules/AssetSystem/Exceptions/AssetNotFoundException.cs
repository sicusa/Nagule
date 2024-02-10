namespace Nagule;

[Serializable]
public class AssetNotFoundException : Exception
{
    public AssetNotFoundException() { }
    public AssetNotFoundException(string message) : base(message) { }
    public AssetNotFoundException(string message, Exception inner) : base(message, inner) { }
}