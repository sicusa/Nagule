namespace Nagule.Backend.OpenTK.Graphics;

using System.Numerics;
using System.Runtime.InteropServices;

using global::OpenTK.Graphics.OpenGL4;

using Aeco;
using Aeco.Reactive;  

using Nagule.Graphics;

public class LightingEnvUniformBufferUpdator : VirtualLayer, IEngineUpdateListener
{
    private Group<Light> _lightIds = new();
    private ParallelQuery<Guid> _lightIdsParallel;

    private readonly Vector3 TwoVec = new Vector3(2);
    private readonly Vector3 ClusterCounts = new Vector3(
        LightingEnvParameters.ClusterCountX,
        LightingEnvParameters.ClusterCountY,
        LightingEnvParameters.ClusterCountZ);

    public LightingEnvUniformBufferUpdator()
    {
        _lightIdsParallel = _lightIds.AsParallel();
    }

    public void OnEngineUpdate(IContext context, float deltaTime)
    {
        _lightIds.Query(context);

        foreach (var id in context.Query<Camera>()) {
            ref readonly var camera = ref context.Inspect<Camera>(id);
            ref readonly var cameraMat = ref context.Inspect<CameraMatrices>(id);
            ref var buffer = ref context.Acquire<LightingEnvUniformBuffer>(id);

            if (context.Contains<Created<Camera>>(id)) {
                InitializeLightingEnv(context, ref buffer, in camera, in cameraMat);
            }
            else if (context.Contains<Modified<Camera>>(id)) {
                UpdateClusterBoundingBoxes(context, ref buffer, in camera, in cameraMat);
                UpdateClusterParameters(ref buffer, in camera);
            }
            if (_lightIds.Count != 0) {
                ref readonly var cameraTransformMat = ref context.Inspect<Transform>(id);
                CullLights(context, ref buffer, in camera, in cameraMat, in cameraTransformMat);
            }
        }
    }
    
    private void InitializeLightingEnv(IContext context, ref LightingEnvUniformBuffer buffer, in Camera camera, in CameraMatrices cameraMat)
    {
        buffer.Parameters.GlobalLightIndeces = new int[4 * LightingEnvParameters.MaximumGlobalLightCount];

        buffer.Clusters = new int[LightingEnvParameters.MaximumActiveLightCount];
        buffer.ClusterLightCounts = new int[LightingEnvParameters.ClusterCount];
        buffer.ClusterBoundingBoxes = new ExtendedRectangle[LightingEnvParameters.ClusterCount];
        UpdateClusterBoundingBoxes(context, ref buffer, in camera, in cameraMat);

        buffer.Handle = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.UniformBuffer, buffer.Handle);
        GL.BindBufferBase(BufferRangeTarget.UniformBuffer, (int)UniformBlockBinding.LightingEnv, buffer.Handle);

        buffer.Pointer = GLHelper.InitializeBuffer(BufferTarget.UniformBuffer, 16 + 4 * LightingEnvParameters.MaximumGlobalLightCount);
        UpdateClusterParameters(ref buffer, in camera);
        
        // initialize texture buffer of clusters

        buffer.ClustersHandle = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.TextureBuffer, buffer.ClustersHandle);

        buffer.ClustersPointer = GLHelper.InitializeBuffer(BufferTarget.TextureBuffer, 4 * LightingEnvParameters.MaximumActiveLightCount);
        buffer.ClustersTexHandle = GL.GenTexture();

        GL.BindTexture(TextureTarget.TextureBuffer, buffer.ClustersTexHandle);
        GL.TexBuffer(TextureBufferTarget.TextureBuffer, SizedInternalFormat.R32i, buffer.ClustersHandle);

        // initialize texture buffer of cluster light counts

        buffer.ClusterLightCountsHandle = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.TextureBuffer, buffer.ClusterLightCountsHandle);

        buffer.ClusterLightCountsPointer = GLHelper.InitializeBuffer(BufferTarget.TextureBuffer, 4 * LightingEnvParameters.ClusterCount);
        buffer.ClusterLightCountsTexHandle = GL.GenTexture();

        GL.BindTexture(TextureTarget.TextureBuffer, buffer.ClusterLightCountsTexHandle);
        GL.TexBuffer(TextureBufferTarget.TextureBuffer, SizedInternalFormat.R32i, buffer.ClusterLightCountsHandle);
    }

    private void UpdateClusterBoundingBoxes(
        IContext context, ref LightingEnvUniformBuffer buffer, in Camera camera, in CameraMatrices cameraMat)
    {
        const int countX = LightingEnvParameters.ClusterCountX;
        const int countY = LightingEnvParameters.ClusterCountY;
        const int countZ = LightingEnvParameters.ClusterCountZ;

        const float floatCountX = (float)countX;
        const float floatCountY = (float)countY;
        const float floatCountZ = (float)countZ;

        Matrix4x4.Invert(cameraMat.Projection, out var invProj);
    
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

    private unsafe void UpdateClusterParameters(ref LightingEnvUniformBuffer buffer, in Camera camera)
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
        IContext context, ref LightingEnvUniformBuffer buffer,
        in Camera camera, in CameraMatrices cameraMat, in Transform cameraTransformMat)
    {
        const int countX = LightingEnvParameters.ClusterCountX;
        const int countY = LightingEnvParameters.ClusterCountY;
        const int maxClusterLightCount = LightingEnvParameters.MaximumClusterLightCount;
        const int maxGlobalLightCount = LightingEnvParameters.MaximumGlobalLightCount;

        var lightPars = context.InspectAny<LightsBuffer>().Parameters;

        var nearPlaneDistance = camera.NearPlaneDistance;
        var farPlaneDistance = camera.FarPlaneDistance;
        var projectionMat = cameraMat.Projection;

        var viewMat = cameraTransformMat.View;
        var boundingBoxes = buffer.ClusterBoundingBoxes;
        var pars = buffer.Parameters;

        var clusters = buffer.Clusters;
        var lightCounts = buffer.ClusterLightCounts;
        Array.Clear(lightCounts);

        pars.GlobalLightCount = 0;
        int localLightCount = 0;

        _lightIdsParallel.ForAll(lightId => {
            var lightRes = context.Inspect<Light>(lightId).Resource;

            ref readonly var lightData = ref context.Inspect<LightData>(lightId);
            var lightIndex = lightData.Index;

            float range = lightData.Range;
            if (range == float.PositiveInfinity) {
                var count = Interlocked.Increment(ref pars.GlobalLightCount) - 1;
                if (count < maxGlobalLightCount) {
                    pars.GlobalLightIndeces[count * 4] = lightIndex;
                }
                else {
                    pars.GlobalLightCount = maxGlobalLightCount;
                }
                return;
            }

            var worldPos = new Vector4(context.Inspect<Transform>(lightId).Position, 1);
            var viewPos = Vector4.Transform(worldPos, viewMat);

            // culled by depth
            if (viewPos.Z < -farPlaneDistance - range || viewPos.Z > -nearPlaneDistance + range) {
                return;
            }

            var rangeVec = new Vector4(range, range, 0, 0);
            var bottomLeft = TransformScreen(viewPos - rangeVec, in projectionMat);
            var topRight = TransformScreen(viewPos + rangeVec, in projectionMat);

            var screenMin = Vector2.Min(bottomLeft, topRight);
            var screenMax = Vector2.Max(bottomLeft, topRight);

            // out of screen
            if (screenMin.X > 1 || screenMax.X < -1 || screenMin.Y > 1 || screenMax.Y < -1) {
                return;
            }

            Interlocked.Increment(ref localLightCount);

            int minX = (int)(Math.Clamp((screenMin.X + 1) / 2, 0, 1) * countX);
            int maxX = (int)MathF.Ceiling(Math.Clamp((screenMax.X + 1) / 2, 0, 1) * countX);
            int minY = (int)(Math.Clamp((screenMin.Y + 1) / 2, 0, 1) * countY);
            int maxY = (int)MathF.Ceiling(Math.Clamp((screenMax.Y + 1) / 2, 0, 1) * countY);
            int minZ = CalculateClusterDepthSlice(Math.Max(0, -viewPos.Z - range), in pars);
            int maxZ = CalculateClusterDepthSlice(-viewPos.Z + range, in pars);

            var centerPoint = new Vector3(viewPos.X, viewPos.Y, viewPos.Z);
            float rangeSq = range * range;

            var category = lightData.Category;
            if (category == LightCategory.Spot) {
                ref var spotPars = ref lightPars[lightData.Index];
                var spotViewPos = Vector3.Transform(spotPars.Position, viewMat);
                var spotViewDir = Vector3.Normalize(Vector3.TransformNormal(spotPars.Direction, viewMat));

                for (int z = minZ; z <= maxZ; ++z) {
                    for (int y = minY; y < maxY; ++y) {
                        for (int x = minX; x < maxX; ++x) {
                            int index = x + countX * y + (countX * countY) * z;
                            if (!IntersectConeWithSphere(
                                    spotViewPos, spotViewDir, range, spotPars.ConeCutoffsOrAreaSize.Y,
                                    boundingBoxes[index].Middle, boundingBoxes[index].Radius)) {
                                continue;
                            }
                            var lightCount = Interlocked.Increment(ref lightCounts[index]) - 1;
                            if (lightCount >= maxClusterLightCount) {
                                lightCounts[index] = maxClusterLightCount;
                                continue;
                            }
                            clusters[index * maxClusterLightCount + lightCount] = lightIndex;
                        }
                    }
                }
            }
            else {
                for (int z = minZ; z <= maxZ; ++z) {
                    for (int y = minY; y < maxY; ++y) {
                        for (int x = minX; x < maxX; ++x) {
                            int index = x + countX * y + (countX * countY) * z;
                            if (rangeSq < boundingBoxes[index].DistanceToPointSquared(centerPoint)) {
                                continue;
                            }
                            var lightCount = Interlocked.Increment(ref lightCounts[index]) - 1;
                            if (lightCount >= maxClusterLightCount) {
                                lightCounts[index] = maxClusterLightCount;
                                continue;
                            }
                            clusters[index * maxClusterLightCount + lightCount] = lightIndex;
                        }
                    }
                }
            }
        });

        *((int*)(buffer.Pointer + 8)) = pars.GlobalLightCount;
        Marshal.Copy(pars.GlobalLightIndeces, 0, buffer.Pointer + 16, 4 * pars.GlobalLightCount);

        if (localLightCount != 0 || buffer.LastActiveLocalLightCount != 0) {
            Marshal.Copy(clusters, 0, buffer.ClustersPointer, LightingEnvParameters.MaximumActiveLightCount);
            Marshal.Copy(lightCounts, 0, buffer.ClusterLightCountsPointer, LightingEnvParameters.ClusterCount);
        }
        buffer.LastActiveLocalLightCount = localLightCount;
    }
    
    private static int CalculateClusterDepthSlice(float z, in LightingEnvParameters pars)
        => (int)Math.Clamp(MathF.Log2(z) * pars.ClusterDepthSliceMultiplier - pars.ClusterDepthSliceSubstractor,
                0, LightingEnvParameters.ClusterCountZ - 1);

    private static Vector2 TransformScreen(Vector4 vec, in Matrix4x4 mat)
    {
        var res = Vector4.Transform(vec, mat);
        return new Vector2(res.X, res.Y) / res.W;
    }

    public bool IntersectConeWithPlane(Vector3 origin, Vector3 forward, float size, float angle, Vector3 plane, float planeOffset)
    {
        var v1 = Vector3.Cross(plane, forward);
        var v2 = Vector3.Cross(v1, forward);
        var capRimPoint = origin + size * MathF.Cos(angle) * forward + size * MathF.Sin(angle) * v2;
        return Vector3.Dot(capRimPoint, plane) + planeOffset >= 0.0f || Vector3.Dot(origin, plane) + planeOffset >= 0.0f;
    }

    public bool IntersectConeWithSphere(Vector3 origin, Vector3 forward, float size, float angle, Vector3 sphereOrigin, float sphereRadius)
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