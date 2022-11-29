namespace Nagule;

using System.Numerics;

public interface IMouseWheelListener
{
    void OnMouseWheel(IContext context, float offsetX, float offsetY);
}