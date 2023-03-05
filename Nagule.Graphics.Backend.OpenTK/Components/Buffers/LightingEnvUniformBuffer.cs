namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct LightingEnvParameters
{
    public const int MaximumGlobalLightCount = 8;
    public const int ClusterCountX = 16;
    public const int ClusterCountY = 9;
    public const int ClusterCountZ = 24;
    public const int ClusterCount = ClusterCountX * ClusterCountY * ClusterCountZ;
    public const int MaximumClusterLightCount = 1024;
    public const int MaximumActiveLightCount = ClusterCount * MaximumClusterLightCount;

    public float ClusterDepthSliceMultiplier;
    public float ClusterDepthSliceSubstractor;

    public int GlobalLightCount;
    public int[] GlobalLightIndices;
}

public struct LightingEnvUniformBuffer : IHashComponent
{
    public BufferHandle Handle;
    public IntPtr Pointer;

    public BufferHandle ClustersHandle;
    public TextureHandle ClustersTexHandle;

    public BufferHandle ClusterLightCountsHandle;
    public TextureHandle ClusterLightCountsTexHandle;

    public ushort[] Clusters;
    public ushort[] ClusterLightCounts;
    public ExtendedRectangle[] ClusterBoundingBoxes;

    public LightingEnvParameters Parameters;
    public int LastActiveLocalLightCount;
}