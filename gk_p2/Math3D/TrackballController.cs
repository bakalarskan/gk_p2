using System.Numerics;

namespace BezierSurfacesApp.Math3D;

public sealed class TrackballController
{
    private Point _lastMousePosition;
    private bool _isDragging;
    private bool _isRollMode;

    public float Sensitivity { get; set; } = 1.35f;

    public bool IsDragging => _isDragging;

    public void BeginDrag(Point mousePos, bool rollMode)
    {
        _lastMousePosition = mousePos;
        _isDragging = true;
        _isRollMode = rollMode;
    }

    public void EndDrag()
    {
        _isDragging = false;
    }

    /// <summary>
    /// Oblicza przyrostową macierz obrotu R_delta na podstawie przesunięcia kursora.
    /// </summary>
    public Matrix3x3 ComputeDeltaRotation(Point currentMousePos, Size viewportSize)
    {
        if (!_isDragging || currentMousePos == _lastMousePosition || viewportSize.Width <= 0 || viewportSize.Height <= 0)
            return Matrix3x3.Identity;

        Matrix3x3 delta;

        if (_isRollMode)
        {
            // Czysty obrót wokół osi Z (roll) na podstawie zmiany kąta biegunowego względem środka ekranu
            float cx = viewportSize.Width * 0.5f;
            float cy = viewportSize.Height * 0.5f;

            float prevAngle = MathF.Atan2(cy - _lastMousePosition.Y, _lastMousePosition.X - cx);
            float currAngle = MathF.Atan2(cy - currentMousePos.Y, currentMousePos.X - cx);
            float dAngle = currAngle - prevAngle;

            // Normalizacja skoku przez granicę -PI / +PI
            if (dAngle > MathF.PI) dAngle -= 2f * MathF.PI;
            if (dAngle < -MathF.PI) dAngle += 2f * MathF.PI;

            delta = Matrix3x3.CreateRotationZ(dAngle);
        }
        else
        {
            // Hybrydowy Trackball (sfera wewnątrz, hiperboloida na zewnątrz -> brak skoków i brak gimbal locka)
            Vector3 v1 = ProjectToTrackball(_lastMousePosition, viewportSize);
            Vector3 v2 = ProjectToTrackball(currentMousePos, viewportSize);

            Vector3 axis = Vector3.Cross(v1, v2);
            float axisLen = axis.Length();

            if (axisLen < 1e-6f)
            {
                delta = Matrix3x3.Identity;
            }
            else
            {
                float dot = Math.Clamp(Vector3.Dot(v1, v2), -1f, 1f);
                float angle = MathF.Acos(dot) * Sensitivity;
                delta = Matrix3x3.CreateFromAxisAngle(axis / axisLen, angle);
            }
        }

        _lastMousePosition = currentMousePos;
        return delta;
    }

    /// <summary>
    /// Rzutuje punkt ekranowy 2D na powierzchnię wirtualnej sfery z płynnym przejściem w hiperboloidę.
    /// </summary>
    private static Vector3 ProjectToTrackball(Point p, Size viewportSize)
    {
        float radius = 0.45f * MathF.Min(viewportSize.Width, viewportSize.Height);
        if (radius < 1f) radius = 1f;

        float x = (p.X - viewportSize.Width * 0.5f) / radius;
        float y = (viewportSize.Height * 0.5f - p.Y) / radius; // oś Y w górę

        float d2 = x * x + y * y;
        float z = d2 <= 0.5f
            ? MathF.Sqrt(1f - d2)         // górna półsfera
            : 0.5f / MathF.Sqrt(d2);      // hiperboloida styczna przy r = 1/sqrt(2)

        return Vector3.Normalize(new Vector3(x, y, z));
    }
}