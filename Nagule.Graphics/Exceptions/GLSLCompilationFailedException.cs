namespace Nagule.Graphics;

[System.Serializable]
public class GLSLCompilationFailedException : System.Exception
{
    public GLSLCompilationFailedException() { }
    public GLSLCompilationFailedException(string message) : base(message) { }
    public GLSLCompilationFailedException(string message, System.Exception inner) : base(message, inner) { }
}