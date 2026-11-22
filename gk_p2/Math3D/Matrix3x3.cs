using System.Numerics;

namespace BezierSurfacesApp.Math3D;

public readonly struct Matrix3x3
{
    public readonly float M11, M12, M13;
    public readonly float M21, M22, M23;
    public readonly float M31, M32, M33;

    public Matrix3x3(
        float m11, float m12, float m13,
        float m21, float m22, float m23,
        float m31, float m32, float m33)
    {
        M11 = m11; M12 = m12; M13 = m13;
        M21 = m21; M22 = m22; M23 = m23;
        M31 = m31; M32 = m32; M33 = m33;
    }

    public static Matrix3x3 Identity => new(
        1, 0, 0,
        0, 1, 0,
        0, 0, 1);

    public static Matrix3x3 CreateRotationX(float theta)
    {
        float c = MathF.Cos(theta);
        float s = MathF.Sin(theta);
        return new Matrix3x3(
            1, 0, 0,
            0, c, -s,
            0, s, c);
    }

    public static Matrix3x3 CreateRotationY(float theta)
    {
        float c = MathF.Cos(theta);
        float s = MathF.Sin(theta);
        return new Matrix3x3(
             c, 0, s,
             0, 1, 0,
            -s, 0, c);
    }

    public static Matrix3x3 CreateRotationZ(float theta)
    {
        float c = MathF.Cos(theta);
        float s = MathF.Sin(theta);
        return new Matrix3x3(
            c, -s, 0,
            s, c, 0,
            0, 0, 1);
    }

    /// <summary>
    /// Macierz obrotu wokół dowolnej osi (wzór Rodriguesa).
    /// </summary>
    public static Matrix3x3 CreateFromAxisAngle(Vector3 axis, float angle)
    {
        float len = axis.Length();
        if (len < 1e-6f || MathF.Abs(angle) < 1e-6f)
            return Identity;

        Vector3 k = axis / len;
        float c = MathF.Cos(angle);
        float s = MathF.Sin(angle);
        float t = 1f - c;

        float x = k.X, y = k.Y, z = k.Z;

        return new Matrix3x3(
            t * x * x + c, t * x * y - s * z, t * x * z + s * y,
            t * x * y + s * z, t * y * y + c, t * y * z - s * x,
            t * x * z - s * y, t * y * z + s * x, t * z * z + c);
    }

    /// <summary>
    /// Składanie macierzy: R_nowa = A * B (dla wektorów kolumnowych).
    /// </summary>
    public static Matrix3x3 operator *(Matrix3x3 a, Matrix3x3 b)
    {
        return new Matrix3x3(
            a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31,
            a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32,
            a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33,

            a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31,
            a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32,
            a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33,

            a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31,
            a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32,
            a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33);
    }

    /// <summary>
    /// Przekształcenie wektora kolumnowego: P' = R * P.
    /// </summary>
    public Vector3 Transform(Vector3 v)
    {
        return new Vector3(
            M11 * v.X + M12 * v.Y + M13 * v.Z,
            M21 * v.X + M22 * v.Y + M23 * v.Z,
            M31 * v.X + M32 * v.Y + M33 * v.Z);
    }

    /// <summary>
    /// Ortonormalizacja Grama-Schmidta kolumn macierzy obrotu (zapobiega dryfowi numerycznemu).
    /// </summary>
    public Matrix3x3 Orthonormalize()
    {
        Vector3 c1 = new(M11, M21, M31);
        Vector3 c2 = new(M12, M22, M32);

        c1 = Vector3.Normalize(c1);
        c2 = Vector3.Normalize(c2 - Vector3.Dot(c1, c2) * c1);
        Vector3 c3 = Vector3.Cross(c1, c2);

        return new Matrix3x3(
            c1.X, c2.X, c3.X,
            c1.Y, c2.Y, c3.Y,
            c1.Z, c2.Z, c3.Z);
    }
}