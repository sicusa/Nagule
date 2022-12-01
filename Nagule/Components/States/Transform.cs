namespace Nagule;

using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

[DataContract]
[StructLayout(LayoutKind.Sequential)]
public unsafe struct Transform : IReactiveComponent
{
    public const int InitialChildrenCapacity = 64;

    public Matrix4x4 World {
        get {
            if (_worldDirty) {
                _world = Parent != null ? Local * Parent->World : Local;
                _worldDirty = false;
                _viewDirty = true;
            }
            return _world;
        }
    }

    public Matrix4x4 View {
        get {
            if (_viewDirty) {
                Matrix4x4.Invert(World, out _view);
                _viewDirty = false;
            }
            return _view;
        }
    }

    public Matrix4x4 Local {
        get {
            if (_translationMatDirty) {
                _translationMat = Matrix4x4.CreateTranslation(_localPosition);
                _translationMatDirty = false;
                _local = _scaleMat * _rotationMat * _translationMat;
            }
            if (_rotationMatDirty) {
                _rotationMat = Matrix4x4.CreateFromQuaternion(_localRotation);
                _rotationMatDirty = false;
                _local = _scaleMat * _rotationMat * _translationMat;
            }
            if (_scaleMatDirty) {
                _scaleMat = Matrix4x4.CreateScale(_localScale);
                _scaleMatDirty = false;
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
            _translationMatDirty = true;
            _positionDirty = true;
            _worldDirty = true;
            _viewDirty = true;
            TagChildrenDirty();
        }
    }

    [DataMember(Name = "Rotation")]
    public Quaternion LocalRotation {
        get => _localRotation;
        set {
            _localRotation = value;
            _rotationMatDirty = true;
            _rotationDirty = true;
            _worldDirty = true;
            _viewDirty = true;
            _axesDirty = true;
            TagChildrenDirty();
        }
    }

    [DataMember(Name = "Scale")]
    public Vector3 LocalScale {
        get => _localScale;
        set {
            _localScale = value;
            _scaleMatDirty = true;
            _worldDirty = true;
            _viewDirty = true;
            TagChildrenDirty();
        }
    }

    public Vector3 Position {
        get {
            if (_positionDirty) {
                _position = Parent != null
                    ? Vector3.Transform(_localPosition, Parent->World) : _localPosition;
                _positionDirty = false;
            }
            return _position;
        }
        set {
            LocalPosition = Parent != null
                ? Vector3.Transform(value, Parent->View) : value;
            _position = value;
            _positionDirty = false;
        }
    }

    public Quaternion Rotation {
        get {
            if (_rotationDirty) {
                _rotation = Parent != null
                    ? Parent->Rotation * _localRotation : _localRotation;
                _rotationDirty = false;
            }
            return _rotation;
        }
        set {
            LocalRotation = Parent != null
                ? Quaternion.Inverse(Parent->Rotation) * value : value;
            _rotation = value;
            _rotationDirty = false;
        }
    }

    public Vector3 Right {
        get {
            if (_axesDirty) { UpdateWorldAxes(); }
            return _right;
        }
    }

    public Vector3 Up {
        get {
            if (_axesDirty) { UpdateWorldAxes(); }
            return _up;
        }
    }

    public Vector3 Forward {
        get {
            if (_axesDirty) { UpdateWorldAxes(); }
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
    private Vector3 _localScale = Vector3.Zero;

    private Matrix4x4 _translationMat = Matrix4x4.Identity;
    private Matrix4x4 _rotationMat = Matrix4x4.Identity;
    private Matrix4x4 _scaleMat = Matrix4x4.Identity;

    private Vector3 _position = Vector3.Zero;
    private Quaternion _rotation = Quaternion.Identity;

    private Vector3 _right = Vector3.UnitX;
    private Vector3 _up = Vector3.UnitY;
    private Vector3 _forward = Vector3.UnitZ;

    private bool _worldDirty = false;
    private bool _viewDirty = false;
    private bool _translationMatDirty = false;
    private bool _rotationMatDirty = false;
    private bool _scaleMatDirty = false;
    private bool _positionDirty = false;
    private bool _rotationDirty = false;
    private bool _axesDirty = false;

    public Transform() {}

    public void TagDirty()
    {
        _worldDirty = true;
        _viewDirty = true;
        _positionDirty = true;
        _rotationDirty = true;
        _axesDirty = true;
        TagChildrenDirty();
    }

    public void TagChildrenDirty()
    {
        if (Children == null) {
            return;
        }
        for (int i = 0; i != ChildrenCount; ++i) {
            var child = *(Children + i);
            child->_worldDirty = true;
            child->_viewDirty = true;
            child->_positionDirty = true;
            child->_rotationDirty = true;
            child->_axesDirty = true;
            child->TagChildrenDirty();
        }
    }

    public void UpdateWorldAxes()
    {
        var worldRot = World;
        _right = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitX, worldRot));
        _up = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitY, worldRot));
        _forward = Vector3.Normalize(Vector3.TransformNormal(-Vector3.UnitZ, worldRot));
        _axesDirty = true;
    }
}