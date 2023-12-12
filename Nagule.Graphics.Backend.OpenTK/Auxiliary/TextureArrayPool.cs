namespace Nagule.Graphics.Backend.OpenTK;

public sealed class TextureArrayPool : IDisposable
{
    public GLInternalFormat InternalFormat { get; }
    public GLPixelFormat PixelFormat { get; }
    public GLPixelType PixelType { get; }

    public int Width { get; }
    public int Height { get; }
    public int Capacity { get; }
    public int MipLevelCount { get; }

    public int Count { get; private set; }

    public GLTextureWrapMode WrapS {
        get => _wrapS;
        set {
            if (_wrapS == value) {
                return;
            }
            _wrapS = value;
            GL.BindTexture(TextureTarget.Texture2dArray, ArrayTextureHandle);
            GL.TexParameteri(TextureTarget.Texture2dArray, TextureParameterName.TextureWrapS, (int)_wrapS);
            GL.BindTexture(TextureTarget.Texture2dArray, 0);
        }
    }

    public GLTextureWrapMode WrapT {
        get => _wrapT;
        set {
            if (_wrapT == value) {
                return;
            }
            _wrapT = value;
            GL.BindTexture(TextureTarget.Texture2dArray, ArrayTextureHandle);
            GL.TexParameteri(TextureTarget.Texture2dArray, TextureParameterName.TextureWrapT, (int)_wrapT);
            GL.BindTexture(TextureTarget.Texture2dArray, 0);
        }
    }

    public GLTextureMinFilter MinFilter {
        get => _minFilter;
        set {
            if (_minFilter == value) {
                return;
            }
            _minFilter = value;
            GL.BindTexture(TextureTarget.Texture2dArray, ArrayTextureHandle);
            GL.TexParameteri(TextureTarget.Texture2dArray, TextureParameterName.TextureMinFilter, (int)_minFilter);
            GL.BindTexture(TextureTarget.Texture2dArray, 0);
        }
    }

    public GLTextureMagFilter MagFilter {
        get => _magFilter;
        set {
            if (_magFilter == value) {
                return;
            }
            _magFilter = value;
            GL.BindTexture(TextureTarget.Texture2dArray, ArrayTextureHandle);
            GL.TexParameteri(TextureTarget.Texture2dArray, TextureParameterName.TextureMagFilter, (int)_magFilter);
            GL.BindTexture(TextureTarget.Texture2dArray, 0);
        }
    }

    public int ArrayTextureHandle { get; private set; }

    private GLTextureWrapMode _wrapS = GLTextureWrapMode.ClampToEdge;
    private GLTextureWrapMode _wrapT = GLTextureWrapMode.ClampToEdge;
    private GLTextureMinFilter _minFilter = GLTextureMinFilter.Nearest;
    private GLTextureMagFilter _magFilter = GLTextureMagFilter.Nearest;

    private int _idAcc;
    public HashSet<int> _allocatedIds = [];
    public Stack<int> _reservedIds = new();

    public unsafe TextureArrayPool(
        GLInternalFormat internalFormat, GLPixelFormat pixelFormat, GLPixelType pixelType,
        int width, int height, int capacity = 256, int mipLevelCount = 1)
    {
        InternalFormat = internalFormat;
        PixelFormat = pixelFormat;
        PixelType = pixelType;

        Width = width;
        Height = height;
        Capacity = capacity;
        MipLevelCount = Math.Max(mipLevelCount, 1);

        ArrayTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2dArray, ArrayTextureHandle);
        GL.TexImage3D(TextureTarget.Texture2dArray, MipLevelCount, InternalFormat, Width, Height, Capacity, 0, PixelFormat, PixelType, (void*)0);

        GL.TexParameteri(TextureTarget.Texture2dArray, TextureParameterName.TextureWrapS, (int)_wrapS);
        GL.TexParameteri(TextureTarget.Texture2dArray, TextureParameterName.TextureWrapT, (int)_wrapT);
        GL.TexParameteri(TextureTarget.Texture2dArray, TextureParameterName.TextureMinFilter, (int)_minFilter);
        GL.TexParameteri(TextureTarget.Texture2dArray, TextureParameterName.TextureMagFilter, (int)_magFilter);

        if (MipLevelCount != 1) {
            GL.GenerateMipmap(TextureTarget.Texture2dArray);
        }

        GL.BindTexture(TextureTarget.Texture2dArray, 0);
    }

    public unsafe int Allocate()
    {
        if (Count == Capacity) {
            throw new InvalidOperationException("Insufficient capacity");
        }
        ++Count;

        if (_reservedIds.TryPop(out int id)) {
            return id;
        }

        id = _idAcc;
        while (_allocatedIds.Contains(id)) {
            id = ++_idAcc;
        }
        _allocatedIds.Add(id);

        GL.BindTexture(TextureTarget.Texture2dArray, ArrayTextureHandle);
        GL.TexSubImage3D(TextureTarget.Texture2dArray, 0, 0, 0, id, Width, Height, 1, PixelFormat, PixelType, (void*)0);
        GL.BindTexture(TextureTarget.Texture2dArray, 0);

        return id;
    }

    public void Release(int id)
    {
        if (!_allocatedIds.Remove(id)) {
            throw new InvalidOperationException("Invalid texture id");
        }
        _reservedIds.Push(id);
    }

    public void Dispose()
    {
        if (ArrayTextureHandle == 0) {
            return;
        }
        GL.DeleteTexture(ArrayTextureHandle);
        ArrayTextureHandle = 0;
    }
}