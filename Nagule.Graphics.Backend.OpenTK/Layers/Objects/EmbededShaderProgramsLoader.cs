namespace Nagule.Graphics.Backend.OpenTK;

using Aeco;

using Nagule.Graphics;

public class EmbededShaderProgramsLoader : Layer, ILoadListener
{
    public void OnLoad(IContext context)
    {
        GraphicsHelper.LoadEmbededShaderPrograms(context);
    }
}