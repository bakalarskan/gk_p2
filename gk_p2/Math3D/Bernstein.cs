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
}