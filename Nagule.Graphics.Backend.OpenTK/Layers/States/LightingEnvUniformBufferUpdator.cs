namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Runtime.InteropServices;

using CommunityToolkit.HighPerformance.Helpers;

using global::OpenTK.Graphics.OpenGL;

using Aeco;
using Aeco.Reactive;  

using Nagule.Graphics;

public class LightingEnvUniformBufferUpdator : Layer, IEngineUpdateListener
{
    private class CullLightsCommand : Command<CullLightsCommand, RenderTarget>
    {
        public LightingEnvUniformBufferUpdator? Sender;
        public Guid CameraId;
        public bool CameraDirty;
        public Matrix4x4 CameraView;
        public Camera? Resource;

        public override Guid? Id => CameraId;

        public override void Execute(ICommandHost host)
        {
            ref var buffer = ref host.Acquire<LightingEnvUniformBuffer>(CameraId, out bool exists);
            ref readonly var cameraData = ref host.Inspect<CameraData>(CameraId);

            if (!exists) {
                InitializeLightingEnv(host, ref buffer, Resource!, in cameraData);
                UpdateClusterBoundingBoxes(host, ref buffer, Resource!, in cameraData);
                UpdateClusterParameters(ref buffer, Resource!);
            }
            else if (CameraDirty) {
                UpdateClusterBoundingBoxes(host, ref buffer, Resource!, in cameraData);
                UpdateClusterParameters(ref buffer, Resource!);
            }
            Sender!.CullLights(host, ref buffer, in cameraData, in CameraView);
        }
    }

    public struct LightCuller : IInAction<Guid>
    {
        public ushort[] Clusters;
        public ushort[] LightCounts;

        private readonly ICommandHost _context;
        private readonly LightingEnvParameters _parameters;
        private readonly LightParameters[] _lightParsArray;

        private readonly Matrix4x4 _cameraView;
        private readonly float _nearPlaneDistance;
        private readonly float _farPlaneDistance;
        private readonly Matrix4x4 _projectionMat;

        private readonly ExtendedRectangle[] _boundingBoxes;

        private static object[] s_locks;

        public static int LocalLightCount;
        public static int GlobalLightCount;

        static LightCuller()
        {
            s_locks = new object[LightingEnvParameters.ClusterCount];
            foreach (ref var l in s_locks.AsSpan()) {
                l = new();
            }
        }

        public LightCuller(ICommandHost host, in LightingEnvUniformBuffer buffer, in CameraData cameraData, in Matrix4x4 cameraView)
        {
            LocalLightCount = 0;
            GlobalLightCount = 0;

            Clusters = buffer.Clusters;
            LightCounts = buffer.ClusterLightCounts;
            Array.Clear(LightCounts);

            _context = host;
            _parameters = buffer.Parameters;
            _lightParsArray = host.InspectAny<LightsBuffer>().Parameters;

            _cameraView = cameraView;
            _nearPlaneDistance = cameraData.NearPlaneDistance;
            _farPlaneDistance = cameraData.FarPlaneDistance;
            _projectionMat = cameraData.Projection;

            _boundingBoxes = buffer.ClusterBoundingBoxes;
        }

        public void Invoke(in Guid lightId)
        {
            const int countX = LightingEnvParameters.ClusterCountX;
            const int countY = LightingEnvParameters.ClusterCountY;
            const int maxGlobalLightCount = LightingEnvParameters.MaximumGlobalLightCount;
            const ushort maxClusterLightCount = LightingEnvParameters.MaximumClusterLightCount;

            if (!_context.TryGet<LightData>(lightId, out var lightData)) {
                return;
            }

            var lightIndex = lightData.Index;
            ref var lightPars = ref _lightParsArray[lightData.Index];

            float range = lightData.Range;
            if (range == float.PositiveInfinity) {
                var count = Interlocked.Increment(ref GlobalLightCount) - 1;
                if (count < maxGlobalLightCount) {
                    _parameters.GlobalLightIndices[count * 4] = lightIndex;
                }
                else {
                    GlobalLightCount = maxGlobalLightCount;
                }
                return;
            }

            var viewPos = Vector4.Transform(lightPars.Position, _cameraView);

            // culled by depth
            if (viewPos.Z < -_farPlaneDistance - range || viewPos.Z > -_nearPlaneDistance + range) {
                return;
            }

            var rangeVec = new Vector4(range, range, 0, 0);
            var bottomLeft = TransformScreen(viewPos - rangeVec, in _projectionMat);
            var topRight = TransformScreen(viewPos + rangeVec, in _projectionMat);

            var screenMin = Vector2.Min(bottomLeft, topRight);
            var screenMax = Vector2.Max(bottomLeft, topRight);

            // out of screen
            if (screenMin.X > 1 || screenMax.X < -1 || screenMin.Y > 1 || screenMax.Y < -1) {
                return;
            }

            Interlocked.Increment(ref LocalLightCount);

            int minX = (int)(Math.Clamp((screenMin.X + 1) / 2, 0, 1) * countX);
            int maxX = (int)MathF.Ceiling(Math.Clamp((screenMax.X + 1) / 2, 0, 1) * countX);
            int minY = (int)(Math.Clamp((screenMin.Y + 1) / 2, 0, 1) * countY);
            int maxY = (int)MathF.Ceiling(Math.Clamp((screenMax.Y + 1) / 2, 0, 1) * countY);
            int minZ = CalculateClusterDepthSlice(Math.Max(0, -viewPos.Z - range), in _parameters);
            int maxZ = CalculateClusterDepthSlice(-viewPos.Z + range, in _parameters);

            var centerPoint = new Vector3(viewPos.X, viewPos.Y, viewPos.Z);
            float rangeSq = range * range;

            var category = lightData.Category;
            if (category == LightCategory.Spot) {
                var spotViewPos = Vector3.Transform(lightPars.Position, _cameraView);
                var spotViewDir = Vector3.Normalize(Vector3.TransformNormal(lightPars.Direction, _cameraView));

                for (int z = minZ; z <= maxZ; ++z) {
                    for (int y = minY; y < maxY; ++y) {
                        for (int x = minX; x < maxX; ++x) {
                            int index = x + countX * y + (countX * countY) * z;
                            if (!IntersectConeWithSphere(
                                    spotViewPos, spotViewDir, range, lightPars.ConeCutoffsOrAreaSize.Y,
                                    _boundingBoxes[index].Middle, _boundingBoxes[index].Radius)) {
                                continue;
                            }
                            lock (s_locks[index]) {
                                var lightCount = LightCounts[index];
                                if (lightCount >= maxClusterLightCount) {
                                    continue;
                                }
                                Clusters[index * maxClusterLightCount + lightCount] = lightIndex;
                                LightCounts[index] = ++lightCount;
                            }
                        }
                    }
                }
            }
            else {
                for (int z = minZ; z <= maxZ; ++z) {
                    for (int y = minY; y < maxY; ++y) {
                        for (int x = minX; x < maxX; ++x) {
                            int index = x + countX * y + (countX * countY) * z;
                            if (rangeSq < _boundingBoxes[index].DistanceToPointSquared(centerPoint)) {
                                continue;
                            }
                            lock (s_locks[index]) {
                                var lightCount = LightCounts[index];
                                if (lightCount >= maxClusterLightCount) {
                                    continue;
                                }
                                Clusters[index * maxClusterLightCount + lightCount] = lightIndex;
                                LightCounts[index] = ++lightCount;
                            }
                        }
                    }
                }
            }
        }
    }

    private Group<Resource<Camera>> _cameraGroup = new();
    private Group<LightData> _lightGroup = new();
    private ParallelQuery<Guid> _lightIdsParallel;

    private LightCuller _culler = default;
    private Action<Guid> _invokeCuller;

    private readonly Vector3 TwoVec = new Vector3(2);
    private readonly Vector3 ClusterCounts = new Vector3(
        LightingEnvParameters.ClusterCountX,
        LightingEnvParameters.ClusterCountY,
        LightingEnvParameters.ClusterCountZ);
    
    public LightingEnvUniformBufferUpdator()
    {
        _lightIdsParallel = _lightGroup.AsParallel();
        _invokeCuller = id => _culler.Invoke(id);
    }

    public void OnEngineUpdate(IContext context)
    {
        foreach (var id in _cameraGroup.Query(context)) {
            var cmd = CullLightsCommand.Create();
            cmd.Sender = this;
            cmd.CameraId = id;
            cmd.CameraDirty = context.Contains<Modified<Resource<Camera>>>(id);
            cmd.CameraView = context.Inspect<Transform>(id).View;
            cmd.Resource = context.Inspect<Resource<Camera>>(id).Value;
            context.SendCommandBatched(cmd);
        }
    }
    
    private static void InitializeLightingEnv(ICommandHost host, ref LightingEnvUniformBuffer buffer, Camera camera, in CameraData cameraData)
    {
        buffer.Parameters.GlobalLightIndices = new int[4 * LightingEnvParameters.MaximumGlobalLightCount];

        buffer.Clusters = new ushort[LightingEnvParameters.MaximumActiveLightCount];
        buffer.ClusterLightCounts = new ushort[LightingEnvParameters.ClusterCount];
        buffer.ClusterBoundingBoxes = new ExtendedRectangle[LightingEnvParameters.ClusterCount];
        UpdateClusterBoundingBoxes(host, ref buffer, camera, in cameraData);

        buffer.Handle = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.UniformBuffer, buffer.Handle);
        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.LightingEnv, buffer.Handle);

        buffer.Pointer = GLHelper.InitializeBuffer(BufferTargetARB.UniformBuffer, 16 + 4 * LightingEnvParameters.MaximumGlobalLightCount);
        UpdateClusterParameters(ref buffer, camera);
        
        // initialize texture buffer of clusters

        buffer.ClustersHandle = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.TextureBuffer, buffer.ClustersHandle);

        buffer.ClustersTexHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.TextureBuffer, buffer.ClustersTexHandle);
        GL.TexBuffer(TextureTarget.TextureBuffer, SizedInternalFormat.R16ui, buffer.ClustersHandle);

        // initialize texture buffer of cluster light counts

        buffer.ClusterLightCountsHandle = GL.GenBuffer();
        GL.BindBuffer(BufferTargetARB.TextureBuffer, buffer.ClusterLightCountsHandle);

        buffer.ClusterLightCountsTexHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.TextureBuffer, buffer.ClusterLightCountsTexHandle);
        GL.TexBuffer(TextureTarget.TextureBuffer, SizedInternalFormat.R16ui, buffer.ClusterLightCountsHandle);
    }

    private static void UpdateClusterBoundingBoxes(
        ICommandHost host, ref LightingEnvUniformBuffer buffer, Camera camera, in CameraData cameraData)
    {
        const int countX = LightingEnvParameters.ClusterCountX;
        const int countY = LightingEnvParameters.ClusterCountY;
        const int countZ = LightingEnvParameters.ClusterCountZ;

        const float floatCountX = (float)countX;
        const float floatCountY = (float)countY;
        const float floatCountZ = (float)countZ;

        Matrix4x4.Invert(cameraData.Projection, out var invProj);
    
        Vector3 ScreenToView(Vector3 texCoord)
        {
            var clip = new Vector4(texCoord.X * 2 - 1, texCoord.Y * 2 - 1, texCoord.Z, 1);
            var view = Vector4.Transform(clip, invProj);
            view = view / view.W;
            return new Vector3(view.X, view.Y, view.Z);
        }

        ref var boundingBoxes = ref buffer.ClusterBoundingBoxes;
        float planeRatio = camera.FarPlaneDistance / camera.NearPlaneDistance;

        for (int x = 0; x < countX; ++x) {
            for (int y = 0; y < countY; ++y) {
                var minPoint = ScreenToView(new Vector3(x / floatCountX, y / floatCountY, -1));
                var maxPoint = ScreenToView(new Vector3((x + 1) / floatCountX, (y + 1) / floatCountY, -1));

                minPoint /= minPoint.Z;
                maxPoint /= maxPoint.Z;

                for (int z = 0; z < countZ; ++z) {
                    float tileNear = -camera.NearPlaneDistance * MathF.Pow(planeRatio, z / floatCountZ);
                    float tileFar = -camera.NearPlaneDistance * MathF.Pow(planeRatio, (z + 1) / floatCountZ);

                    var minPointNear = minPoint * tileNear;
                    var minPointFar = minPoint * tileFar;
                    var maxPointNear = maxPoint * tileNear;
                    var maxPointFar = maxPoint * tileFar;

                    ref var rect = ref boundingBoxes[x + countX * y + (countX * countY) * z];
                    rect.Min = Vector3.Min(Vector3.Min(minPointNear, minPointFar), Vector3.Min(maxPointNear, maxPointFar));
                    rect.Max = Vector3.Max(Vector3.Max(minPointNear, minPointFar), Vector3.Max(maxPointNear, maxPointFar));
                    rect.UpdateExtents();
                }
            }
        }
    }

    private static unsafe void UpdateClusterParameters(ref LightingEnvUniformBuffer buffer, Camera camera)
    {
        float factor = LightingEnvParameters.ClusterCountZ /
            MathF.Log2(camera.FarPlaneDistance / camera.NearPlaneDistance);
        float subtractor = MathF.Log2(camera.NearPlaneDistance) * factor;

        float* envPtr = (float*)buffer.Pointer;
        *envPtr = factor;
        *(envPtr + 1) = subtractor;

        buffer.Parameters.ClusterDepthSliceMultiplier = factor;
        buffer.Parameters.ClusterDepthSliceSubstractor = subtractor;
    }

    private unsafe void CullLights(
        ICommandHost host, ref LightingEnvUniformBuffer buffer,
        in CameraData cameraData, in Matrix4x4 cameraView)
    {
        _lightGroup.Refresh(host);

        _culler = new LightCuller(host, in buffer, in cameraData, in cameraView);
        _lightIdsParallel.ForAll(_invokeCuller);

        int globalLightCount = LightCuller.GlobalLightCount;
        ref var pars = ref buffer.Parameters;
        buffer.Parameters.GlobalLightCount = globalLightCount;

        *((int*)(buffer.Pointer + 8)) = globalLightCount;
        Marshal.Copy(pars.GlobalLightIndices, 0, buffer.Pointer + 16, 4 * globalLightCount);

        if (LightCuller.LocalLightCount != 0 || buffer.LastActiveLocalLightCount != 0) {
            GL.BindBuffer(BufferTargetARB.TextureBuffer, buffer.ClustersHandle);
            GL.BufferData(BufferTargetARB.TextureBuffer, _culler.Clusters, BufferUsageARB.StreamDraw);

            GL.BindBuffer(BufferTargetARB.TextureBuffer, buffer.ClusterLightCountsHandle);
            GL.BufferData(BufferTargetARB.TextureBuffer, _culler.LightCounts, BufferUsageARB.StreamDraw);
        }
        buffer.LastActiveLocalLightCount = LightCuller.LocalLightCount;
    }
    
    private static int CalculateClusterDepthSlice(float z, in LightingEnvParameters pars)
        => Math.Clamp((int)(MathF.Log2(z) * pars.ClusterDepthSliceMultiplier - pars.ClusterDepthSliceSubstractor), 0,
            LightingEnvParameters.ClusterCountZ - 1);

    private static Vector2 TransformScreen(Vector4 vec, in Matrix4x4 mat)
    {
        var res = Vector4.Transform(vec, mat);
        return new Vector2(res.X, res.Y) / res.W;
    }

    public static bool IntersectConeWithPlane(Vector3 origin, Vector3 forward, float size, float angle, Vector3 plane, float planeOffset)
    {
        var v1 = Vector3.Cross(plane, forward);
        var v2 = Vector3.Cross(v1, forward);
        var capRimPoint = origin + size * MathF.Cos(angle) * forward + size * MathF.Sin(angle) * v2;
        return Vector3.Dot(capRimPoint, plane) + planeOffset >= 0.0f || Vector3.Dot(origin, plane) + planeOffset >= 0.0f;
    }

    public static bool IntersectConeWithSphere(Vector3 origin, Vector3 forward, float size, float angle, Vector3 sphereOrigin, float sphereRadius)
    {
        Vector3 v = sphereOrigin - origin;
        float vlenSq = Vector3.Dot(v, v);
        float v1len = Vector3.Dot(v, forward);
        float distanceClosestPoint = MathF.Cos(angle) * MathF.Sqrt(vlenSq - v1len * v1len) - v1len * MathF.Sin(angle);
    
        bool angleCull = distanceClosestPoint > sphereRadius;
        bool frontCull = v1len > sphereRadius + size;
        bool backCull = v1len < -sphereRadius;
        return !(angleCull || frontCull || backCull);
    }
}