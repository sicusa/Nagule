namespace Nagule.Prelude;

using System.Numerics;

public struct FirstPersonControllerState()
{
    public bool Active = true;
    public bool Moving;
    public Vector2 Position;
    public Vector3 SmoothDir;
}