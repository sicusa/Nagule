namespace Nagule.Graphics.Backends.OpenTK;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PipelineUniform
{
    public int ViewportWidth;
    public int ViewportHeight;
    public int Frame;
    public float Time;
    public Vector3 SunLightDirection;

    public static readonly int MemorySize = Unsafe.SizeOf<PipelineUniform>();
}
