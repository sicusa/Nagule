namespace Nagule.Graphics;

[System.Serializable]
public class GLSLCompilationFailedException : Exception
{
    public GLSLCompilationFailedException() { }
    public GLSLCompilationFailedException(string message) : base(message) { }
    public GLSLCompilationFailedException(string message, Exception inner) : base(message, inner) { }
}