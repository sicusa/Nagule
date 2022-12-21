namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Concurrent;

using global::OpenTK.Graphics.OpenGL;

using Nagule.Graphics;

public class MeshUniformBufferUpdator : ReactiveUpdatorBase<Resource<Mesh>>, IRenderListener
{
    private ConcurrentQueue<(bool, Guid)> _commandQueue = new();

    protected override void Update(IContext context, Guid id)
    {
        if (!context.Contains<Resource<Mesh>>(id)) {
            return;
        }
        _commandQueue.Enqueue((true, id));
    }

    protected override void Release(IContext context, Guid id)
    {
        _commandQueue.Enqueue((false, id));
    }

    public unsafe void OnRender(IContext context, float deltaTime)
    {
        while (_commandQueue.TryDequeue(out var command)) {
            var (commandType, id) = command;
            if (commandType) {
                ref var handle = ref context.Acquire<MeshUniformBuffer>(id, out bool exists).Handle;
                if (!exists) {
                    handle = GL.GenBuffer();
                    GL.BindBuffer(BufferTargetARB.UniformBuffer, handle);
                    GL.BufferData(BufferTargetARB.UniformBuffer, 2 * 16, IntPtr.Zero, BufferUsageARB.DynamicDraw);
                    GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Mesh, handle);
                }
                else {
                    GL.BindBuffer(BufferTargetARB.UniformBuffer, handle);
                }

                var mesh = context.UnsafeAcquire<Resource<Mesh>>(id).Value!;
                var boundingBox = mesh.BoundingBox;
                GL.BufferSubData(BufferTargetARB.UniformBuffer, IntPtr.Zero, 12, boundingBox.Min);
                GL.BufferSubData(BufferTargetARB.UniformBuffer, IntPtr.Zero + 16, 12, boundingBox.Max);
            }
            else {
                if (context.Remove<MeshUniformBuffer>(id, out var handle)) {
                    GL.DeleteBuffer(handle.Handle);
                }
            }
        }
    }
}