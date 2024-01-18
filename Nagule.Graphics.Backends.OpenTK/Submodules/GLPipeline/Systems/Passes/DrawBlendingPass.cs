namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class DrawBlendingPass()
    : DrawPassBase(materialPredicate: MaterialPredicates.IsBlending)
{
    protected override bool BeforeDraw(Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState, in GLSLProgramState programState)
    {
        if (DrawnGroupCount == 0) {
            GL.Enable(EnableCap.Blend);
        }
        return true;
    }

    protected override void EndPass()
    {
        if (DrawnGroupCount == 0) {
            return;
        }
        GL.Disable(EnableCap.Blend);
    }

    protected override EntityRef GetShaderProgramState(
        Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState)
        => materialState.ColorProgramState;
}