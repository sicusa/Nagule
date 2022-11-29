namespace Nagule.Backend.OpenTK.Graphics;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LightingEnvParameters
{
    public const int MaximumGlobalLightCount = 64;
    public const int ClusterCountX = 16;
    public const int ClusterCountY = 9;
    public const int ClusterCountZ = 24;
    public const int ClusterCount = ClusterCountX * ClusterCountY * ClusterCountZ;
    public const int MaximumClusterLightCount = 64;
    public const int MaximumActiveLightCount = ClusterCount * MaximumClusterLightCount;

    public float ClusterDepthSliceMultiplier;
    public float ClusterDepthSliceSubstractor;

    public int GlobalLightCount;
    public int[] GlobalLightIndeces;
}

public struct LightingEnvUniformBuffer : IPooledComponent
{
    public int Handle;
    public IntPtr Pointer;

    public int ClustersHandle;
    public int ClustersTexHandle;
    public IntPtr ClustersPointer;

    public int ClusterLightCountsHandle;
    public int ClusterLightCountsTexHandle;
    public IntPtr ClusterLightCountsPointer;

    public int[] Clusters;
    public int[] ClusterLightCounts;
    public ExtendedRectangle[] ClusterBoundingBoxes;

    public LightingEnvParameters Parameters;
    public int LastActiveLocalLightCount;
}