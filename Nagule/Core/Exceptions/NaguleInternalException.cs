namespace Sia;

[Serializable]
public class NaguleInternalException : Exception
{
    public NaguleInternalException() { }
    public NaguleInternalException(string message) : base(message) { }
    public NaguleInternalException(string message, Exception inner) : base(message, inner) { }
}