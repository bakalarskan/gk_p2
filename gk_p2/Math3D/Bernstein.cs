using System.Numerics;

namespace BezierSurfacesApp.Math3D;

public static class Bernstein
{
    public static float EvaluateCubic(int i, float t)
    {
        float u = 1f - t;
        return i switch
        {
            0 => u * u * u,
            1 => 3f * t * u * u,
            2 => 3f * t * t * u,
            3 => t * t * t,
            _ => 0f
        };
    }

    public static float EvaluateQuadratic(int i, float t)
    {
        float u = 1f - t;
        return i switch
        {
            0 => u * u,
            1 => 2f * t * u,
            2 => t * t,
            _ => 0f
        };
    }

    /// <summary>
    /// Wyznacza punkt na krzywej Béziera dowolnego stopnia algorytmem de Casteljau.
    /// </summary>
    public static Vector2 EvaluateDeCasteljau(IReadOnlyList<Vector2> controlPoints, float t)
    {
        int n = controlPoints.Count;
        if (n == 0) return Vector2.Zero;
        if (n == 1) return controlPoints[0];

        Span<Vector2> buffer = n <= 32 ? stackalloc Vector2[n] : new Vector2[n];
        for (int i = 0; i < n; i++)
            buffer[i] = controlPoints[i];

        for (int r = 1; r < n; r++)
        {
            for (int i = 0; i < n - r; i++)
            {
                buffer[i] = Vector2.Lerp(buffer[i], buffer[i + 1], t);
            }
        }

        return buffer[0];
    }
}