using System.Numerics;
using BezierSurfacesApp.Math3D;

namespace BezierSurfacesApp.Models;

public sealed class LightCurve
{
    // Minimum 2 segmenty 3. stopnia: V0..V3 oraz V3..V6 (V6 = V0) -> 7 punktów
    public const int MinimumControlPoints = 7;

    public List<Vector2> ControlPoints { get; } = new();

    public float Z { get; set; } = 240f;
    public float ParameterT { get; set; } = 0f;
    public float Speed { get; set; } = 0.14f;
    public bool IsAnimating { get; set; } = true;

    public int SegmentCount => Math.Max(1, (ControlPoints.Count - 1) / 3);

    public LightCurve()
    {
        ResetToDefault();
    }

    public void ResetToDefault()
    {
        ControlPoints.Clear();
        // 2 segmenty 3. stopnia tworzące gładką, zamkniętą pętlę (C1 w V0 i V3)
        // Punkty na krzywej (węzły): V0 (=V6) oraz V3
        ControlPoints.Add(new Vector2(220f, 0f));     // V0 (na krzywej)
        ControlPoints.Add(new Vector2(220f, 290f));   // V1
        ControlPoints.Add(new Vector2(-220f, 290f));  // V2
        ControlPoints.Add(new Vector2(-220f, 0f));    // V3 (na krzywej)
        ControlPoints.Add(new Vector2(-220f, -290f)); // V4
        ControlPoints.Add(new Vector2(220f, -290f));  // V5
        ControlPoints.Add(ControlPoints[0]);          // V6 = V0
    }

    public void Update(float deltaTimeSeconds)
    {
        if (!IsAnimating) return;

        ParameterT += Speed * deltaTimeSeconds;
        if (ParameterT >= 1f)
            ParameterT -= MathF.Floor(ParameterT);
    }

    public Vector2 Evaluate2D(float t)
    {
        int segments = SegmentCount;
        float scaled = Math.Clamp(t, 0f, 1f) * segments;
        int segIdx = Math.Min((int)scaled, segments - 1);
        float localT = scaled - segIdx;

        int i0 = segIdx * 3;
        Vector2 p0 = ControlPoints[i0];
        Vector2 p1 = ControlPoints[i0 + 1];
        Vector2 p2 = ControlPoints[i0 + 2];
        Vector2 p3 = ControlPoints[i0 + 3];

        return p0 * Bernstein.EvaluateCubic(0, localT)
             + p1 * Bernstein.EvaluateCubic(1, localT)
             + p2 * Bernstein.EvaluateCubic(2, localT)
             + p3 * Bernstein.EvaluateCubic(3, localT);
    }

    public Vector3 CurrentLightPosition
    {
        get
        {
            Vector2 pos2D = Evaluate2D(ParameterT);
            return new Vector3(pos2D.X, pos2D.Y, Z);
        }
    }

    public void MoveControlPoint(int index, Vector2 newPos)
    {
        if (index < 0 || index >= ControlPoints.Count) return;

        ControlPoints[index] = newPos;
        if (index == 0)
            ControlPoints[^1] = newPos;
        else if (index == ControlPoints.Count - 1)
            ControlPoints[0] = newPos;
    }

    /// <summary>
    /// Dzieli ostatni segment sześcienny w połowie (de Casteljau), dodając nowy węzeł na krzywej (+3 punkty).
    /// </summary>
    public void AddControlPoint()
    {
        int start = ControlPoints.Count - 4;
        Vector2 p0 = ControlPoints[start];
        Vector2 p1 = ControlPoints[start + 1];
        Vector2 p2 = ControlPoints[start + 2];
        Vector2 p3 = ControlPoints[start + 3];

        Vector2 q0 = (p0 + p1) * 0.5f;
        Vector2 q1 = (p1 + p2) * 0.5f;
        Vector2 q2 = (p2 + p3) * 0.5f;
        Vector2 r0 = (q0 + q1) * 0.5f;
        Vector2 r1 = (q1 + q2) * 0.5f;
        Vector2 mid = (r0 + r1) * 0.5f;

        ControlPoints[start + 1] = q0;
        ControlPoints[start + 2] = r0;
        ControlPoints.InsertRange(start + 3, [mid, r1, q2]);
    }

    /// <summary>
    /// Usuwa ostatni dodatkowy segment (-3 punkty), zachowując minimum 7 punktów.
    /// </summary>
    public bool RemoveControlPoint()
    {
        if (ControlPoints.Count <= MinimumControlPoints)
            return false;

        ControlPoints.RemoveRange(ControlPoints.Count - 4, 3);
        return true;
    }

    public int HitTestControlPoint(Vector2 worldPos, float hitRadius = 14f)
    {
        float r2 = hitRadius * hitRadius;
        for (int i = 0; i < ControlPoints.Count - 1; i++)
        {
            if (Vector2.DistanceSquared(ControlPoints[i], worldPos) <= r2)
                return i;
        }
        return -1;
    }
}