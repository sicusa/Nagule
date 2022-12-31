namespace Nagule.Graphics;

using System.Runtime.Serialization;

[DataContract]
public struct Camera : IReactiveComponent
{
    [DataMember] public CameraMode Mode = CameraMode.Perspective;
    [DataMember] public float FieldOfView = 60f;
    [DataMember] public float NearPlaneDistance = 0.01f;
    [DataMember] public float FarPlaneDistance = 200f;

    public Camera() {}
}