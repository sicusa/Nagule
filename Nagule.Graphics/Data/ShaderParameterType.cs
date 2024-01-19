namespace Nagule.Graphics;

public enum ShaderParameterType
{
    Unit,
    Texture1D,
    Texture2D,
    Texture3D,
    Cubemap,
    ArrayTexture1D,
    ArrayTexture2D,
    Tileset2D,

    Int,
    UInt,
    Bool,
    Float,
    Double,

    Vector2,
    Vector3,
    Vector4,

    DoubleVector2,
    DoubleVector3,
    DoubleVector4,

    IntVector2,
    IntVector3,
    IntVector4,

    UIntVector2,
    UIntVector3,
    UIntVector4,

    BoolVector2,
    BoolVector3,
    BoolVector4,

    Matrix4x4,
    Matrix4x3,
    Matrix3x3,
    Matrix3x2,
    Matrix2x2,

    DoubleMatrix4x4,
    DoubleMatrix4x3,
    DoubleMatrix3x3,
    DoubleMatrix3x2,
    DoubleMatrix2x2
}