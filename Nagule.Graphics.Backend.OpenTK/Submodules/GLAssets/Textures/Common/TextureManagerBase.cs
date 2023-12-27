namespace Nagule.Graphics.Backend.OpenTK;

using System.Numerics;
using System.Runtime.CompilerServices;
using Sia;

public abstract class TextureManagerBase<TTexture, TTextureTemplate, TTextureState>
    : GraphicsAssetManagerBase<TTexture, TTextureTemplate, Tuple<TTextureState, TextureHandle>>
    where TTexture : struct, IAsset<TTextureTemplate>, IConstructable<TTexture, TTextureTemplate>
    where TTextureTemplate : IAsset
    where TTextureState : struct, ITextureState
{
    public delegate void StateHandler(ref TTextureState state);

    protected delegate void CommandHandler<TCommand>(in TTextureState state, in TCommand cmd)
        where TCommand : ICommand<TTexture>;
    
    protected abstract TextureTarget TextureTarget { get; }

    protected override void UnloadAsset(EntityRef entity, ref TTexture asset, EntityRef stateEntity)
    {
        RenderFrame.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<TTextureState>();
            GL.DeleteTexture(state.Handle.Handle);
            return true;
        });
    }

    protected void RegisterParameterListener<TCommand>(CommandHandler<TCommand> handler)
        where TCommand : ICommand<TTexture>
    {
        Listen((EntityRef entity, in TCommand cmd) => {
            var cmdCopy = cmd;
            RenderFrame.Enqueue(entity, () => {
                ref var state = ref entity.GetState<TTextureState>();
                GL.BindTexture(TextureTarget, state.Handle.Handle);
                handler(state, cmdCopy);
                GL.BindTexture(TextureTarget, 0);
                return true;
            });
        });
    }

    protected void RegisterCommonListeners<
        TSetMinFilterCommand, TSetMagFilterCommand, TSetBorderColorCommand, TSetMipmapEnabledCommand>(
        Func<TSetMinFilterCommand, TextureMinFilter> minFilterGetter,
        Func<TSetMagFilterCommand, TextureMagFilter> magFilterGetter,
        Func<TSetBorderColorCommand, Vector4> borderColorGetter,
        Func<TSetMipmapEnabledCommand, bool> mipmapEnabledGetter)
        where TSetMinFilterCommand : ICommand<TTexture>
        where TSetMagFilterCommand : ICommand<TTexture>
        where TSetBorderColorCommand : ICommand<TTexture>
        where TSetMipmapEnabledCommand : ICommand<TTexture>
    {
        RegisterParameterListener((in TTextureState state, in TSetMinFilterCommand cmd) =>
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureMinFilter, TextureUtils.Cast(minFilterGetter(cmd))));

        RegisterParameterListener((in TTextureState state, in TSetMagFilterCommand cmd) =>
            GL.TexParameteri(TextureTarget, TextureParameterName.TextureMagFilter, TextureUtils.Cast(magFilterGetter(cmd))));
        
        unsafe {
            RegisterParameterListener((in TTextureState state, in TSetBorderColorCommand cmd) => {
                var borderColor = borderColorGetter(cmd);
                GL.TexParameterf(TextureTarget, TextureParameterName.TextureBorderColor,
                    new ReadOnlySpan<float>(Unsafe.AsPointer(ref borderColor), 4));
            });
        }

        Listen((EntityRef entity, in TSetMipmapEnabledCommand cmd) => {
            var enabled = mipmapEnabledGetter(cmd);
            RenderFrame.Enqueue(entity, () => {
                ref var state = ref entity.GetState<TTextureState>();
                state.MipmapEnabled = enabled;
                if (enabled) {
                    var handle = state.Handle.Handle;
                    GL.BindTexture(TextureTarget, handle);
                    GL.GenerateMipmap(TextureTarget);
                    GL.BindTexture(TextureTarget, 0);
                }
                return true;
            });
        });
    }

    public void RegenerateTexture(EntityRef entity, Action action)
    {
        RenderFrame.Enqueue(entity, () => {
            ref var state = ref entity.GetState<TTextureState>();
            GL.BindTexture(TextureTarget, state.Handle.Handle);
            action();
            if (state.MipmapEnabled) {
                GL.GenerateMipmap(TextureTarget);
            }
            GL.BindTexture(TextureTarget, 0);
            return true;
        });
    }

    public void RegenerateTexture(EntityRef entity, StateHandler handler)
    {
        RenderFrame.Enqueue(entity, () => {
            ref var state = ref entity.GetState<TTextureState>();
            GL.BindTexture(TextureTarget, state.Handle.Handle);
            handler(ref state);
            if (state.MipmapEnabled) {
                GL.GenerateMipmap(TextureTarget);
            }
            GL.BindTexture(TextureTarget, 0);
            return true;
        });
    }

    protected void SetCommonParameters(TextureMinFilter minFilter, TextureMagFilter magFilter, Vector4 borderColor, bool mipmapEnabled)
    {
        GL.TexParameteri(TextureTarget, TextureParameterName.TextureMinFilter, TextureUtils.Cast(minFilter));
        GL.TexParameteri(TextureTarget, TextureParameterName.TextureMagFilter, TextureUtils.Cast(magFilter));

        unsafe {
            GL.TexParameterf(TextureTarget, TextureParameterName.TextureBorderColor,
                new ReadOnlySpan<float>(Unsafe.AsPointer(ref borderColor), 4));
        }
        if (mipmapEnabled) {
            GL.GenerateMipmap(TextureTarget);
        }
        GL.BindTexture(TextureTarget, 0);
    }
}