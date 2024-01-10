namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class DrawOpaquePass()
    : DrawPassBase(materialPredicate: MaterialPredicates.IsOpaque)
{
    public bool DepthMask { get; init; } = true;

    protected override void BeginPass()
    {
        if (!DepthMask) {
            GL.DepthMask(false);
        }
    }

    protected override void EndPass()
    {
        if (!DepthMask) {
            GL.DepthMask(true);
        }
    }

    protected override EntityRef GetShaderProgramState(
        Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState)
        => materialState.ColorProgramState;
}