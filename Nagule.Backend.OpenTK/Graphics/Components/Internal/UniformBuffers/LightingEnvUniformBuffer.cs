namespace Nagule.Backend.OpenTK.Graphics;

using System.Runtime.InteropServices;

using global::OpenTK.Graphics;

[StructLayout(LayoutKind.Sequential)]
public struct LightingEnvParameters
{
    public const int MaximumGlobalLightCount = 8;
    public const int ClusterCountX = 16;
    public const int ClusterCountY = 9;
    public const int ClusterCountZ = 24;
    public const int ClusterCount = ClusterCountX * ClusterCountY * ClusterCountZ;
    public const int MaximumClusterLightCount = 32;
    public const int MaximumActiveLightCount = ClusterCount * MaximumClusterLightCount;

    public float ClusterDepthSliceMultiplier;
    public float ClusterDepthSliceSubstractor;

    public int GlobalLightCount;
    public int[] GlobalLightIndeces;
}

public struct LightingEnvUniformBuffer : IPooledComponent
{
    public BufferHandle Handle;
    public IntPtr Pointer;

    public BufferHandle ClustersHandle;
    public TextureHandle ClustersTexHandle;
    public IntPtr ClustersPointer;

    public BufferHandle ClusterLightCountsHandle;
    public TextureHandle ClusterLightCountsTexHandle;
    public IntPtr ClusterLightCountsPointer;

    public int[] Clusters;
    public int[] ClusterLightCounts;
    public ExtendedRectangle[] ClusterBoundingBoxes;

    public LightingEnvParameters Parameters;
    public int LastActiveLocalLightCount;
}