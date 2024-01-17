namespace Nagule;

using System.Numerics;

public record struct AABB(Vector3 Min, Vector3 Max)
{
    public override readonly string ToString()
        => $"[{Min}, {Max}]";
    
    public readonly Vector3 ClosetPoint(Vector3 point)
        => Vector3.Clamp(point, Min, Max);
    
    public readonly float DistanceToPoint(Vector3 point)
        => Vector3.Distance(point, ClosetPoint(point));

    public readonly float DistanceToPointSquared(Vector3 point)
        => Vector3.DistanceSquared(point, ClosetPoint(point));
}

public record struct ExtendedAABB
{
    public Vector3 Min;
    public Vector3 Max;

    public Vector3 Middle { get; private set;}
    public float Radius { get; private set; }

    public ExtendedAABB(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
        Middle = (Min + Max) / 2;
        Radius = Vector3.Distance(Middle, Max);
    }

    public void UpdateExtents()
    {
        Middle = (Min + Max) / 2;
        Radius = Vector3.Distance(Middle, Min);
    }

    public readonly bool Equals(AABB other)
        => Min == other.Min && Max == other.Max;

    public override readonly string ToString()
        => $"[{Min}, {Max}]";
    
    public readonly Vector3 ClosetPoint(Vector3 point)
        => Vector3.Clamp(point, Min, Max);
    
    public readonly float DistanceToPoint(Vector3 point)
        => Vector3.Distance(point, ClosetPoint(point));

    public readonly float DistanceToPointSquared(Vector3 point)
        => Vector3.DistanceSquared(point, ClosetPoint(point));
}