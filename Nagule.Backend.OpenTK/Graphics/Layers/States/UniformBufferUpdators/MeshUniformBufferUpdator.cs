namespace Nagule.Backend.OpenTK.Graphics;

using global::OpenTK.Graphics.OpenGL4;

using Nagule.Graphics;

public class MeshUniformBufferUpdator : ReactiveObjectUpdatorBase<Mesh>
{
    protected override void UpdateObject(IContext context, Guid id)
    {
        ref var handle = ref context.Acquire<MeshUniformBuffer>(id, out bool exists).Handle;
        if (!exists) {
            handle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.UniformBuffer, handle);
            GL.BufferData(BufferTarget.UniformBuffer, 2 * 16, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, (int)UniformBlockBinding.Mesh, handle);
        }
        else {
            GL.BindBuffer(BufferTarget.UniformBuffer, handle);
        }

        ref var mesh = ref context.UnsafeAcquire<Mesh>(id);
        ref var boundingBox = ref mesh.Resource.BoudingBox;
        GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, 12, ref boundingBox.Min);
        GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero + 16, 12, ref boundingBox.Max);
    }

    protected override void ReleaseObject(IContext context, Guid id)
    {
        if (context.Remove<MeshUniformBuffer>(id, out var handle)) {
            GL.DeleteBuffer(handle.Handle);
        }
    }
}