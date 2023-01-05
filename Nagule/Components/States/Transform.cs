namespace Nagule;

using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

[DataContract]
[StructLayout(LayoutKind.Sequential)]
public unsafe struct Transform : IReactiveComponent
{
    [Flags]
    private enum DirtyTags
    {
        None = 0,
        WorldMatrix = 1,
        ViewMatrix = 2,
        TranslationMatrix = 4,
        RotationMatrix = 8,
        ScaleMatrix = 16,
        WorldPosition = 32,
        WorldRotation = 64,
        LocalAngles = 128,
        WorldAngles = 256,
        WorldAxes = 512
    }

    public const int InitialChildrenCapacity = 64;

    public Matrix4x4 World {
        get {
            if ((_dirtyTags & DirtyTags.WorldMatrix) != DirtyTags.None) {
                _world = Parent != null ? Local * Parent->World : Local;
                _dirtyTags &= ~DirtyTags.WorldMatrix;
                _dirtyTags |= DirtyTags.ViewMatrix;
            }
            return _world;
        }
    }

    public Matrix4x4 View {
        get {
            if ((_dirtyTags & DirtyTags.ViewMatrix) != DirtyTags.None) {
                Matrix4x4.Invert(World, out _view);
                _dirtyTags &= ~DirtyTags.ViewMatrix;
            }
            return _view;
        }
    }

    public Matrix4x4 Local {
        get {
            bool modified = false;
            if ((_dirtyTags & DirtyTags.TranslationMatrix) != DirtyTags.None) {
                _translationMat = Matrix4x4.CreateTranslation(_localPosition);
                _dirtyTags &= ~DirtyTags.TranslationMatrix;
                modified = true;
            }
            if ((_dirtyTags & DirtyTags.RotationMatrix) != DirtyTags.None) {
                _rotationMat = Matrix4x4.CreateFromQuaternion(_localRotation);
                _dirtyTags &= ~DirtyTags.RotationMatrix;
                modified = true;
            }
            if ((_dirtyTags & DirtyTags.ScaleMatrix) != DirtyTags.None) {
                _scaleMat = Matrix4x4.CreateScale(_localScale);
                _dirtyTags &= ~DirtyTags.ScaleMatrix;
                modified = true;
            }
            if (modified) {
                _local = _scaleMat * _rotationMat * _translationMat;
            }
            return _local;
        }
    }

    [DataMember(Name = "Position")]
    public Vector3 LocalPosition {
        get => _localPosition;
        set {
            _localPosition = value;
            _dirtyTags |= DirtyTags.TranslationMatrix
                | DirtyTags.WorldPosition
                | DirtyTags.WorldMatrix
                | DirtyTags.ViewMatrix;
            TagChildrenDirty();
        }
    }

    [DataMember(Name = "Rotation")]
    public Quaternion LocalRotation {
        get => _localRotation;
        set {
            _localRotation = value;
            _dirtyTags |= DirtyTags.RotationMatrix
                | DirtyTags.WorldRotation
                | DirtyTags.WorldMatrix
                | DirtyTags.ViewMatrix
                | DirtyTags.LocalAngles
                | DirtyTags.WorldAngles
                | DirtyTags.WorldAxes;
            TagChildrenDirty();
        }
    }

    [DataMember(Name = "Scale")]
    public Vector3 LocalScale {
        get => _localScale;
        set {
            _localScale = value;
            _dirtyTags |= DirtyTags.ScaleMatrix
                | DirtyTags.WorldMatrix
                | DirtyTags.ViewMatrix;
            TagChildrenDirty();
        }
    }

    public Vector3 Position {
        get {
            if ((_dirtyTags & DirtyTags.WorldPosition) != DirtyTags.None) {
                _position = Parent != null
                    ? Vector3.Transform(_localPosition, Parent->World) : _localPosition;
                _dirtyTags &= ~DirtyTags.WorldPosition;
            }
            return _position;
        }
        set {
            LocalPosition = Parent != null
                ? Vector3.Transform(value, Parent->View) : value;
            _position = value;
            _dirtyTags &= ~DirtyTags.WorldPosition;
        }
    }

    public Quaternion Rotation {
        get {
            if ((_dirtyTags & DirtyTags.WorldRotation) != DirtyTags.None) {
                _rotation = Parent != null
                    ? Parent->Rotation * _localRotation : _localRotation;
                _dirtyTags &= ~DirtyTags.WorldRotation;
            }
            return _rotation;
        }
        set {
            LocalRotation = Parent != null
                ? Quaternion.Inverse(Parent->Rotation) * value : value;
            _rotation = value;
            _dirtyTags &= ~DirtyTags.WorldRotation;
        }
    }

    public Vector3 LocalAngles {
        get {
            if ((_dirtyTags & DirtyTags.LocalAngles) != DirtyTags.None) {
                _localAngles = _localRotation.ToEulerAngles();
                _dirtyTags &= ~DirtyTags.LocalAngles;
            }
            return _localAngles;
        }
        set {
            LocalRotation = value.ToQuaternion();
            _localAngles = value;
            _dirtyTags &= ~DirtyTags.LocalAngles;
        }
    }

    public Vector3 Angles {
        get {
            if ((_dirtyTags & DirtyTags.WorldAngles) != DirtyTags.None) {
                _angles = _rotation.ToEulerAngles();
                _dirtyTags &= ~DirtyTags.WorldAngles;
            }
            return _angles;
        }
        set {
            Rotation = value.ToQuaternion();
            _angles = value;
            _dirtyTags &= ~DirtyTags.WorldAngles;
        }
    }

    public Vector3 Right {
        get {
            if ((_dirtyTags & DirtyTags.WorldAxes) != DirtyTags.None) {
                UpdateWorldAxes();
            }
            return _right;
        }
    }

    public Vector3 Up {
        get {
            if ((_dirtyTags & DirtyTags.WorldAxes) != DirtyTags.None) {
                UpdateWorldAxes();
            }
            return _up;
        }
    }

    public Vector3 Forward {
        get {
            if ((_dirtyTags & DirtyTags.WorldAxes) != DirtyTags.None) {
                UpdateWorldAxes();
            }
            return _forward;
        }
    }

    internal Guid Id = Guid.Empty;
    internal Transform* Parent = null;
    internal Transform** Children = null;
    internal GCHandle ChildrenHandle = default;
    internal int ChildrenCapacity = 0;
    internal int ChildrenCount = 0;

    private Matrix4x4 _world = Matrix4x4.Identity;
    private Matrix4x4 _view = Matrix4x4.Identity;
    private Matrix4x4 _local = Matrix4x4.Identity;

    private Vector3 _localPosition = Vector3.Zero;
    private Quaternion _localRotation = Quaternion.Identity;
    private Vector3 _localScale = Vector3.One;

    private Matrix4x4 _translationMat = Matrix4x4.Identity;
    private Matrix4x4 _rotationMat = Matrix4x4.Identity;
    private Matrix4x4 _scaleMat = Matrix4x4.Identity;

    private Vector3 _position = Vector3.Zero;
    private Quaternion _rotation = Quaternion.Identity;

    private Vector3 _localAngles = Vector3.Zero;
    private Vector3 _angles = Vector3.Zero;

    private Vector3 _right = Vector3.UnitX;
    private Vector3 _up = Vector3.UnitY;
    private Vector3 _forward = Vector3.UnitZ;

    private DirtyTags _dirtyTags = DirtyTags.None;

    public Transform() {}

    public void TagDirty()
    {
        _dirtyTags |= DirtyTags.WorldMatrix
            | DirtyTags.ViewMatrix
            | DirtyTags.WorldPosition
            | DirtyTags.WorldRotation
            | DirtyTags.LocalAngles
            | DirtyTags.WorldAngles
            | DirtyTags.WorldAxes;
        TagChildrenDirty();
    }

    public void TagChildrenDirty()
    {
        if (Children == null) {
            return;
        }
        for (int i = 0; i != ChildrenCount; ++i) {
            Children[i]->TagDirty();
        }
    }

    public void UpdateWorldAxes()
    {
        var worldRot = World;
        _right = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitX, worldRot));
        _up = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitY, worldRot));
        _forward = Vector3.Normalize(Vector3.TransformNormal(-Vector3.UnitZ, worldRot));
        _dirtyTags &= ~DirtyTags.WorldAxes;
    }
}