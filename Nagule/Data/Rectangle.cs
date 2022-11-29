namespace Nagule;

using System.Numerics;
using System.Runtime.Serialization;

[DataContract]
public struct Rectangle : IEquatable<Rectangle>
{
    [DataMember] public Vector3 Min;
    [DataMember] public Vector3 Max;

    public Rectangle(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    public bool Equals(Rectangle other)
        => Min == other.Min && Max == other.Max;

    public override string ToString()
        => $"[{Min}, {Max}]";
    
    public Vector3 ClosetPoint(Vector3 point)
        => Vector3.Clamp(point, Min, Max);
    
    public float DistanceToPoint(Vector3 point)
        => Vector3.Distance(point, ClosetPoint(point));

    public float DistanceToPointSquared(Vector3 point)
        => Vector3.DistanceSquared(point, ClosetPoint(point));
}

[DataContract]
public struct ExtendedRectangle
{
    [DataMember] public Vector3 Min;
    [DataMember] public Vector3 Max;

    public Vector3 Middle;
    public float Radius;

    public ExtendedRectangle(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
        Middle = (Min + Max) / 2;
        Radius = Vector3.Distance(Middle, Max);
    }

    public void UpdateExtents()
    {
        Middle = (Min + Max) / 2;
        Radius = Vector3.Distance(Middle, Max);
    }

    public bool Equals(Rectangle other)
        => Min == other.Min && Max == other.Max;

    public override string ToString()
        => $"[{Min}, {Max}]";
    
    public Vector3 ClosetPoint(Vector3 point)
        => Vector3.Clamp(point, Min, Max);
    
    public float DistanceToPoint(Vector3 point)
        => Vector3.Distance(point, ClosetPoint(point));

    public float DistanceToPointSquared(Vector3 point)
        => Vector3.DistanceSquared(point, ClosetPoint(point));
}