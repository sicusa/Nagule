namespace Nagule.Graphics;

using System.Numerics;
using System.Runtime.Serialization;

[DataContract]
public struct GraphicsSpecification : ISingletonComponent
{
    // Window
    [DataMember] public int Width = 800;
    [DataMember] public int Height = 600;
    [DataMember] public string Title = "Nagule Engine";
    [DataMember] public bool IsResizable = true;
    [DataMember] public bool IsFullscreen = false;
    [DataMember] public bool HasBorder = true;
    [DataMember] public (int, int)? MaximumSize = null;
    [DataMember] public (int, int)? MinimumSize = null;
    [DataMember] public (int, int)? Location = null;

    // Rendering
    [DataMember] public int Framerate = 60;
    [DataMember] public Vector4 ClearColor = Vector4.Zero;
    [DataMember] public VSyncMode VSyncMode = VSyncMode.Off;

    // Debug
    [DataMember] public bool IsDebugEnabled = false;
    
    public GraphicsSpecification() {}
}