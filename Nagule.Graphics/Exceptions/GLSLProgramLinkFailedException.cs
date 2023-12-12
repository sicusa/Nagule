namespace Nagule.Graphics;

[System.Serializable]
public class GLSLProgramLinkFailedException : System.Exception
{
    public GLSLProgramLinkFailedException() { }
    public GLSLProgramLinkFailedException(string message) : base(message) { }
    public GLSLProgramLinkFailedException(string message, System.Exception inner) : base(message, inner) { }
}