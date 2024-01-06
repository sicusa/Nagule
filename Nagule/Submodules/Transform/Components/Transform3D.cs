namespace Nagule;

using System.Numerics;
using System.Runtime.CompilerServices;
using Sia;

public struct Transform3D
{
    public sealed class OnChanged : SingletonEvent<OnChanged> {}

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
    }

    public readonly record struct SetLocal(Matrix4x4 Value)
        : ICommand<Transform3D>, IReconstructableCommand<SetLocal>
    {
        public static SetLocal ReconstructFromCurrentState(in EntityRef entity)
            => new(entity.Get<Transform3D>().Local);

        public void Execute(World world, in EntityRef target)
            => Execute(world, target, ref target.Get<Transform3D>());

        public void Execute(World world, in EntityRef target, ref Transform3D transform)
        {
            transform._local = Value;
            Matrix4x4.Decompose(Value, out transform._scale, out transform._rotation, out transform._position);

            transform.DirtyTags = TransformDirtyTags.Globals;
            transform.TagChildrenDirty(world, TransformDirtyTags.Globals);

            world.Send(target, OnChanged.Instance);
        }
    }

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
    }

    public readonly record struct SetWorld(Matrix4x4 Value)
        : ICommand<Transform3D>, IReconstructableCommand<SetLocal>
    {
        public static SetLocal ReconstructFromCurrentState(in EntityRef entity)
            => new(entity.Get<Transform3D>().Local);

        public void Execute(World world, in EntityRef target)
            => Execute(world, target, ref target.Get<Transform3D>());

        public void Execute(World world, in EntityRef target, ref Transform3D transform)
        {
            ref var worldMat = ref transform._world;
            ref var localMat = ref transform._local;

            worldMat = Value;

            var parent = transform.Parent;
            localMat = parent != null
                ? parent.Value.Get<Transform3D>().View * worldMat : worldMat;

            Matrix4x4.Decompose(localMat,
                out transform._scale, out transform._rotation, out transform._position);

            transform.DirtyTags = TransformDirtyTags.Globals & ~TransformDirtyTags.World;
            transform.TagChildrenDirty(world, TransformDirtyTags.Globals);

            world.Send(target, OnChanged.Instance);
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

    public readonly Vector3 Position => _position;

    public readonly record struct SetPosition(Vector3 Value)
        : ICommand<Transform3D>, IReconstructableCommand<SetPosition>
    {
        public static SetPosition ReconstructFromCurrentState(in EntityRef entity)
            => new(entity.Get<Transform3D>().Position);

        public void Execute(World world, in EntityRef target)
            => Execute(world, target, ref target.Get<Transform3D>());

        public void Execute(World world, in EntityRef target, ref Transform3D transform)
        {
            transform._position = Value;
            transform.DirtyTags = TransformDirtyTags.All;
            transform.TagChildrenDirty(world, TransformDirtyTags.Globals);

            world.Send(target, OnChanged.Instance);
        }
    }

    public readonly Quaternion Rotation => _rotation;

    public readonly record struct SetRotation(Quaternion Value)
        : ICommand<Transform3D>, IReconstructableCommand<SetRotation>
    {
        public static SetRotation ReconstructFromCurrentState(in EntityRef entity)
            => new(entity.Get<Transform3D>().Rotation);

        public void Execute(World world, in EntityRef target)
            => Execute(world, target, ref target.Get<Transform3D>());

        public void Execute(World world, in EntityRef target, ref Transform3D transform)
        {
            transform._rotation = Value;
            transform.DirtyTags = TransformDirtyTags.All;
            transform.TagChildrenDirty(world, TransformDirtyTags.Globals);

            world.Send(target, OnChanged.Instance);
        }
    }

    public readonly Vector3 Scale => _scale;

    public readonly record struct SetScale(Vector3 Value)
        : ICommand<Transform3D>, IReconstructableCommand<SetScale>
    {
        public static SetScale ReconstructFromCurrentState(in EntityRef entity)
            => new(entity.Get<Transform3D>().Scale);

        public void Execute(World world, in EntityRef target)
            => Execute(world, target, ref target.Get<Transform3D>());

        public void Execute(World world, in EntityRef target, ref Transform3D transform)
        {
            transform._scale = Value;
            transform.DirtyTags = TransformDirtyTags.All;
            transform.TagChildrenDirty(world, TransformDirtyTags.Globals);

            world.Send(target, OnChanged.Instance);
        }
    }

    public Vector3 WorldPosition {
        get {
            UpdateWorldComponents();
            return _worldPosition;
        }
    }

    public readonly record struct SetWorldPosition(Vector3 Value)
        : ICommand<Transform3D>, IReconstructableCommand<SetWorldPosition>
    {
        public static SetWorldPosition ReconstructFromCurrentState(in EntityRef entity)
            => new(entity.Get<Transform3D>().WorldPosition);

        public void Execute(World world, in EntityRef target)
            => Execute(world, target, ref target.Get<Transform3D>());

        public void Execute(World world, in EntityRef target, ref Transform3D transform)
        {
            transform._worldPosition = Value;

            var parent = transform.Parent;
            transform._position = parent != null
                ? Vector3.Transform(Value, parent.Value.Get<Transform3D>().View) : Value;

            transform.DirtyTags = TransformDirtyTags.All;
            transform.TagChildrenDirty(world, TransformDirtyTags.Globals);

            world.Send(target, OnChanged.Instance);
        }
    }

    public Quaternion WorldRotation {
        get {
            UpdateWorldComponents();
            return _worldRotation;
        }
    }

    public readonly record struct SetWorldRotation(Quaternion Value)
        : ICommand<Transform3D>, IReconstructableCommand<SetWorldRotation>
    {
        public static SetWorldRotation ReconstructFromCurrentState(in EntityRef entity)
            => new(entity.Get<Transform3D>().WorldRotation);

        public void Execute(World world, in EntityRef target)
            => Execute(world, target, ref target.Get<Transform3D>());

        public void Execute(World world, in EntityRef target, ref Transform3D transform)
        {
            transform._worldRotation = Value;

            var parent = transform.Parent;
            transform._rotation = parent != null
                ? Quaternion.Inverse(parent.Value.Get<Transform3D>().WorldRotation) * Value : Value;

            transform.DirtyTags = TransformDirtyTags.All;
            transform.TagChildrenDirty(world, TransformDirtyTags.Globals);

            world.Send(target, OnChanged.Instance);
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

    public readonly record struct SetParent(EntityRef? Value)
        : ICommand<Transform3D>, IReconstructableCommand<SetParent>
    {
        public static SetParent ReconstructFromCurrentState(in EntityRef entity)
            => new(entity.Get<Transform3D>().Parent);

        public void Execute(World world, in EntityRef target)
            => Execute(world, target, ref target.Get<Transform3D>());

        public void Execute(World world, in EntityRef target, ref Transform3D component)
        {
            component.Parent = Value;
            component.DirtyTags |= TransformDirtyTags.Globals;
            component.TagChildrenDirty(world, TransformDirtyTags.Globals);

            target.Modify(new Node<Transform3D>.SetParent(Value));

            if (Value is EntityRef parent) {
                ref var parentTrans = ref parent.Get<Transform3D>();
                if (parentTrans.Children == null) {
                    ref var parentNode = ref parent.Get<Node<Transform3D>>();
                    parentTrans.Children = parentNode.Children;
                }
            }

            target.Send(OnChanged.Instance);
        }
    }

    public IReadOnlySet<EntityRef>? Children { get; internal set; }

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

    internal readonly void TagChildrenDirty(World world, TransformDirtyTags tags)
    {
        if (Children == null) {
            return;
        }
        foreach (var child in Children) {
            ref var childTrans = ref child.Get<Transform3D>();
            if ((childTrans.DirtyTags & tags) != tags) {
                childTrans.DirtyTags |= tags;
                childTrans.TagChildrenDirty(world, tags);
            }
            world.Send(child, OnChanged.Instance);
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