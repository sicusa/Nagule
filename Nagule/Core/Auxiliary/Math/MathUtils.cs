namespace Nagule;

using System.Numerics;
using System.Runtime.CompilerServices;

public static class MathUtils
{
    public const float TwoPI = MathF.PI * 2;
    public const float DegreeToRadian = TwoPI / 360;
    public const float RadianToDegree = 360 / TwoPI;

    public static float Lerp(float firstFloat, float secondFloat, float by)
        => firstFloat * (1 - by) + secondFloat * by;

    public static Vector3 ToEulerAngles(this Quaternion q)
    {
        var angles = new Vector3();

        float sinrCosp = 2 * (q.W * q.X + q.Y * q.Z);
        float cosrCosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
        angles.X = MathF.Atan2(sinrCosp, cosrCosp);

        float sinp = 2 * (q.W * q.Y - q.Z * q.X);
        angles.Y = MathF.Abs(sinp) >= 1
            ? MathF.CopySign(MathF.PI / 2, sinp)
            : MathF.Asin(sinp);

        float sinyCosp = 2 * (q.W * q.Z + q.X * q.Y);
        float cosyCosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
        angles.Z = MathF.Atan2(sinyCosp, cosyCosp);

        return angles * RadianToDegree;
    }

    // from https://answers.unity.com/questions/467614/what-is-the-source-code-of-quaternionlookrotation.html
    public static Quaternion LookRotation(Vector3 forward, Vector3 up)
    {
        forward = Vector3.Normalize(forward);
        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, up));
        up = Vector3.Cross(right, forward);
        
        var m00 = right.X;
        var m01 = right.Y;
        var m02 = right.Z;
        var m10 = up.X;
        var m11 = up.Y;
        var m12 = up.Z;
        var m20 = forward.X;
        var m21 = forward.Y;
        var m22 = forward.Z;
    
        float num8 = m00 + m11 + m22;
        var quaternion = new Quaternion();
        if (num8 > 0f) {
            var num = (float)Math.Sqrt(num8 + 1f);
            quaternion.W = num * 0.5f;
            num = 0.5f / num;
            quaternion.X = (m12 - m21) * num;
            quaternion.Y = (m20 - m02) * num;
            quaternion.Z = (m01 - m10) * num;
            return quaternion;
        }
        if ((m00 >= m11) && (m00 >= m22)) {
            var num7 = (float)Math.Sqrt(((1f + m00) - m11) - m22);
            var num4 = 0.5f / num7;
            quaternion.X = 0.5f * num7;
            quaternion.Y = (m01 + m10) * num4;
            quaternion.Z = (m02 + m20) * num4;
            quaternion.W = (m12 - m21) * num4;
            return quaternion;
        }
        if (m11 > m22) {
            var num6 = (float)Math.Sqrt(((1f + m11) - m00) - m22);
            var num3 = 0.5f / num6;
            quaternion.X = (m10+ m01) * num3;
            quaternion.Y = 0.5f * num6;
            quaternion.Z = (m21 + m12) * num3;
            quaternion.W = (m20 - m02) * num3;
            return quaternion; 
        }
        var num5 = (float)Math.Sqrt(((1f + m22) - m00) - m11);
        var num2 = 0.5f / num5;
        quaternion.X = (m20 + m02) * num2;
        quaternion.Y = (m21 + m12) * num2;
        quaternion.Z = 0.5f * num5;
        quaternion.W = (m01 - m10) * num2;
        return quaternion;
    }

    public static Quaternion ToQuaternion(this Vector3 v)
    {
        v *= DegreeToRadian;

        float cy = MathF.Cos(v.Z * 0.5f);
        float sy = MathF.Sin(v.Z * 0.5f);
        float cp = MathF.Cos(v.Y * 0.5f);
        float sp = MathF.Sin(v.Y * 0.5f);
        float cr = MathF.Cos(v.X * 0.5f);
        float sr = MathF.Sin(v.X * 0.5f);

        return new Quaternion {
            W = cr * cp * cy + sr * sp * sy,
            X = sr * cp * cy - cr * sp * sy,
            Y = cr * sp * cy + sr * cp * sy,
            Z = cr * cp * sy - sr * sp * cy
        };
    }

    public static float ToRadian(this float f)
        => f * DegreeToRadian;

    public static Vector2 ToRadian(this Vector2 v)
        => v * DegreeToRadian;

    public static Vector3 ToRadian(this Vector3 v)
        => v * DegreeToRadian;

    public static Vector4 ToRadian(this Vector4 v)
        => v * DegreeToRadian;

    public static float ToDegree(this float f)
        => f * RadianToDegree;

    public static Vector2 ToDegree(this Vector2 v)
        => v * RadianToDegree;

    public static Vector3 ToDegree(this Vector3 v)
        => v * RadianToDegree;

    public static Vector4 ToDegree(this Vector4 v)
        => v * RadianToDegree;

    public static int InterlockedXor(ref int location, int value)
    {
        int current = location;
        while (true) {
            int newValue = current ^ value;
            int oldValue = Interlocked.CompareExchange(ref location, newValue, current);
            if (oldValue == current) {
                return oldValue;
            }
            current = oldValue;
        }
    }
}
