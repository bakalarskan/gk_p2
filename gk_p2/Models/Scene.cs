using System.Numerics;
using BezierSurfacesApp.Math3D;

namespace BezierSurfacesApp.Models;

public sealed class Scene
{
    public List<BezierSurface> Surfaces { get; } = new();
    public Matrix3x3 RotationMatrix { get; private set; } = Matrix3x3.Identity;
    public LightCurve LightCurve { get; } = new();

    // Parametry modelu oświetlenia
    public float Kd { get; set; } = 0.75f;
    public float Ks { get; set; } = 0.45f;
    public int ShininessM { get; set; } = 35;
    public Color LightColor { get; set; } = Color.White;

    // Mapa normalnych
    public bool UseNormalMap { get; set; } = false;
    public TextureBuffer NormalMap { get; set; } = TextureBuffer.CreateProceduralNormalMap("Cegły");

    // Triangulacja i widoczność warstw
    public int TriangulationDivisions { get; set; } = 20;
    public bool ShowControlPolygon { get; set; } = true;
    public bool ShowWireframe { get; set; } = false;
    public bool ShowFilledTriangles { get; set; } = true;
    public bool ShowLightCurve { get; set; } = true;
    public bool EditLightCurveMode { get; set; } = false;

    public Vector3 LightColorNormalized =>
        new(LightColor.R / 255f, LightColor.G / 255f, LightColor.B / 255f);

    public int TotalTriangles => Surfaces.Sum(s => s.Triangles.Count);

    public void ApplyDeltaRotation(Matrix3x3 delta)
    {
        RotationMatrix = (delta * RotationMatrix).Orthonormalize();
        UpdateRotatedGeometry();
    }

    public void ResetRotation()
    {
        // Lekki startowy obrót, aby obie przecinające się powierzchnie były dobrze widoczne w 3D
        RotationMatrix =
            Matrix3x3.CreateRotationX(-0.55f) *
            Matrix3x3.CreateRotationY(0.45f);
        UpdateRotatedGeometry();
    }

    public void RebuildTriangulation()
    {
        foreach (var surface in Surfaces)
        {
            surface.Triangulate(TriangulationDivisions);
        }
        UpdateRotatedGeometry();
    }

    public void UpdateRotatedGeometry()
    {
        foreach (var surface in Surfaces)
        {
            surface.ApplyRotation(RotationMatrix);
        }
    }
}