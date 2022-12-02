namespace Nagule.Graphics;

using System.Numerics;

using Aeco;
using Aeco.Reactive;

public class CameraMatricesUpdator : VirtualLayer, IEngineUpdateListener, IWindowResizeListener
{
    private Query<Modified<Camera>, Camera> _q = new();
    private int _width;
    private int _height;

    public void OnWindowResize(IContext context, int width, int height)
    {
        _width = width;
        _height = height;

        foreach (var id in context.Query<Camera>()) {
            UpdateCamera(context, id);
        }
    }

    public void OnEngineUpdate(IContext context, float deltaTime)
    {
        foreach (var id in _q.Query(context)) {
            UpdateCamera(context, id);
        }
    }

    private void UpdateCamera(IContext context, Guid cameraId)
    {
        ref readonly var camera = ref context.Inspect<Camera>(cameraId);
        ref var matrices = ref context.Acquire<CameraMatrices>(cameraId);
        float aspectRatio = (float)_width / (float)_height;

        matrices.Projection = Matrix4x4.CreatePerspectiveFieldOfView(
            camera.FieldOfView / 180 * MathF.PI,
            aspectRatio, camera.NearPlaneDistance, camera.FarPlaneDistance);
    }
}