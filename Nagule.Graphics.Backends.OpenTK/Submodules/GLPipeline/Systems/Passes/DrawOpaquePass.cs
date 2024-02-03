namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class DrawOpaquePass()
    : DrawPassBase(materialPredicate: MaterialPredicates.IsOpaque)
{
    public bool UseDrawnDepth { get; init; } = true;

    protected override void BeginPass()
    {
        if (UseDrawnDepth) {
            GL.DepthMask(false);
            GL.DepthFunc(DepthFunction.Equal);
        }
    }

    protected override void EndPass()
    {
        if (UseDrawnDepth) {
            GL.DepthMask(true);
            GL.DepthFunc(DepthFunction.Lequal);
        }
    }

    protected override EntityRef GetShaderProgramState(
        Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState)
        => materialState.ColorProgramState;
}