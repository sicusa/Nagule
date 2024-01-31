namespace Nagule;

using System.Numerics;
using System.Runtime.CompilerServices;
using Sia;

public partial struct Transform3D
{
    public sealed class OnChanged : SingletonEvent<OnChanged> {}

    [SiaProperty]
    public Matrix4x4 Local {
        get {
            if ((DirtyTags & TransformDirtyTags.Local) != 0) {
                _local = Matrix4x4.CreateScale(_scale)
                    * Matrix4x4.CreateFromQuaternion(_rotation)
                    * Matrix4x4.CreateTranslation(_position);
                DirtyTags &= ~TransformDirtyTags.Local;
            }
            return _local;
        }
        set {
            _local = value;

            Matrix4x4.Decompose(value,
                out _scale, out _rotation, out _position);

            DirtyTags = TransformDirtyTags.Globals;
        }
    }

    [SiaProperty]
    public Matrix4x4 World {
        get {
            if ((DirtyTags & TransformDirtyTags.World) != 0) {
                _world = Parent != null
                    ? Local * Parent.Value.Get<Transform3D>().World
                    : Local;
                DirtyTags &= ~TransformDirtyTags.World;
            }
            return _world;
        }
        set {
            _world = value;
            _local = Parent != null
                ? Parent.Value.Get<Transform3D>().View * _world : _world;

            Matrix4x4.Decompose(_local,
                out _scale, out _rotation, out _position);

            DirtyTags = TransformDirtyTags.Globals & ~TransformDirtyTags.World;
        }
    }

    public Matrix4x4 View {
        get {
            if ((DirtyTags & TransformDirtyTags.View) != 0) {
                Matrix4x4.Invert(World, out _view);
                DirtyTags &= ~TransformDirtyTags.View;
            }
            return _view;
        }
    }

    [SiaProperty]
    public Vector3 Position {
        readonly get => _position;

        set {
            _position = value;
            DirtyTags = TransformDirtyTags.All;
        }
    }

    [SiaProperty]
    public Quaternion Rotation {
        readonly get => _rotation;

        set {
            _rotation = value;
            DirtyTags = TransformDirtyTags.All;
        }
    }

    [SiaProperty]
    public Vector3 Scale {
        readonly get => _scale;

        set {
            _scale = value;
            DirtyTags = TransformDirtyTags.All;
        }
    }

    [SiaProperty]
    public Vector3 WorldPosition {
        get {
            UpdateWorldComponents();
            return _worldPosition;
        }
        set {
            _worldPosition = value;
            _position = Parent != null
                ? Vector3.Transform(value, Parent.Value.Get<Transform3D>().View) : value;
            DirtyTags = TransformDirtyTags.All;
        }
    }

    [SiaProperty]
    public Quaternion WorldRotation {
        get {
            UpdateWorldComponents();
            return _worldRotation;
        }
        set {
            _worldRotation = value;
            _rotation = Parent != null
                ? Quaternion.Inverse(Parent.Value.Get<Transform3D>().WorldRotation) * value : value;
            DirtyTags = TransformDirtyTags.All;
        }
    }

    public Vector3 WorldScale {
        get {
            UpdateWorldComponents();
            return _worldScale;
        }
    }

    public Vector3 WorldRight => Vector3.Transform(Vector3.UnitX, WorldRotation);
    public Vector3 WorldUp => Vector3.Transform(Vector3.UnitY, WorldRotation);
    public Vector3 WorldForward => Vector3.Transform(-Vector3.UnitZ, WorldRotation);

    public readonly Vector3 Right => Vector3.Transform(Vector3.UnitX, Rotation);
    public readonly Vector3 Up => Vector3.Transform(Vector3.UnitY, Rotation);
    public readonly Vector3 Forward => Vector3.Transform(-Vector3.UnitZ, Rotation);

    public EntityRef? Parent { get; internal set; }

    internal TransformDirtyTags DirtyTags;

    private Matrix4x4 _local;
    private Matrix4x4 _world;
    private Matrix4x4 _view;

    private Vector3 _position;
    private Quaternion _rotation;
    private Vector3 _scale;

    private Vector3 _worldPosition;
    private Quaternion _worldRotation;
    private Vector3 _worldScale;

    public Transform3D() {}

    public Transform3D(Vector3 position)
    {
        _position = position;
        DirtyTags = TransformDirtyTags.All;
    }

    public Transform3D(Vector3 position, Quaternion rotation)
    {
        _position = position;
        _rotation = rotation;
        DirtyTags = TransformDirtyTags.All;
    }
    
    public Transform3D(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        _position = position;
        _rotation = rotation;
        _scale = scale;
        DirtyTags = TransformDirtyTags.All;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void UpdateWorldComponents()
    {
        if ((DirtyTags & TransformDirtyTags.WorldComps) != 0) {
            Matrix4x4.Decompose(World, out _worldScale, out _worldRotation, out _worldPosition);
            DirtyTags &= ~TransformDirtyTags.WorldComps;
        }
    }

}

public static class EntityTransformExtenions
{
    public static void SetPosition(this EntityRef entity, Vector3 position)
        => entity.Modify(new Transform3D.SetPosition(position));
    public static void SetWorldPosition(this EntityRef entity, Vector3 position)
        => entity.Modify(new Transform3D.SetWorldPosition(position));

    public static void SetRotation(this EntityRef entity, Quaternion rotation)
        => entity.Modify(new Transform3D.SetRotation(rotation));
    public static void SetWorldRotation(this EntityRef entity, Quaternion rotation)
        => entity.Modify(new Transform3D.SetWorldRotation(rotation));

    public static void SetScale(this EntityRef entity, Vector3 scale)
        => entity.Modify(new Transform3D.SetScale(scale));
}