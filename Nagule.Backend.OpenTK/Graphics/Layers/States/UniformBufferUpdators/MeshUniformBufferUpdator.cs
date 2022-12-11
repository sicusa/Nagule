namespace Nagule.Backend.OpenTK.Graphics;

using global::OpenTK.Graphics.OpenGL;

using Nagule.Graphics;

public class MeshUniformBufferUpdator : ReactiveObjectUpdatorBase<Mesh>
{
    protected override void UpdateObject(IContext context, Guid id)
    {
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

        ref var mesh = ref context.UnsafeAcquire<Mesh>(id);
        ref var boundingBox = ref mesh.Resource.BoudingBox;
        GL.BufferSubData(BufferTargetARB.UniformBuffer, IntPtr.Zero, 12, boundingBox.Min);
        GL.BufferSubData(BufferTargetARB.UniformBuffer, IntPtr.Zero + 16, 12, boundingBox.Max);
    }

    protected override void ReleaseObject(IContext context, Guid id)
    {
        if (context.Remove<MeshUniformBuffer>(id, out var handle)) {
            GL.DeleteBuffer(handle.Handle);
        }
    }
}