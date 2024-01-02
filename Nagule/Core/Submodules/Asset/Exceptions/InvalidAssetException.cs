namespace Nagule;

[Serializable]
public class InvalidAssetException : Exception
{
    public InvalidAssetException() { }
    public InvalidAssetException(string message) : base(message) { }
    public InvalidAssetException(string message, Exception inner) : base(message, inner) { }
}