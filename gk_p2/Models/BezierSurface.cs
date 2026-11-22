using System.Numerics;
using BezierSurfacesApp.Math3D;

namespace BezierSurfacesApp.Models;

public enum SurfaceColorMode
{
    SolidColor,
    Texture
}

public sealed class BezierSurface
{
    public string Name { get; }
    public Vector3[,] ControlPoints { get; } = new Vector3[4, 4];
    public Vector3[,] RotatedControlPoints { get; } = new Vector3[4, 4];

    public List<Vertex> Vertices { get; } = new();
    public List<Triangle> Triangles { get; } = new();

    // Niezależne ustawienia koloru obiektu I_O dla każdej powierzchni
    public SurfaceColorMode ColorMode { get; set; } = SurfaceColorMode.SolidColor;
    public Color SolidColor { get; set; }
    public TextureBuffer Texture { get; set; }

    // Kolor rysowania wieloboku Béziera dla odróżnienia powierzchni
    public Color ControlNetColor { get; set; }

    public BezierSurface(string name, Vector3[,] controlPoints, Color defaultColor, Color controlNetColor, TextureBuffer defaultTexture)
    {
        Name = name;
        SolidColor = defaultColor;
        ControlNetColor = controlNetColor;
        Texture = defaultTexture;

        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
            {
                ControlPoints[i, j] = controlPoints[i, j];
                RotatedControlPoints[i, j] = controlPoints[i, j];
            }
    }

    public Vector3 SolidColorNormalized =>
        new(SolidColor.R / 255f, SolidColor.G / 255f, SolidColor.B / 255f);

    /// <summary>
    /// Buduje siatkę trójkątów (divisions x divisions oczek -> 2 * divisions^2 trójkątów).
    /// </summary>
    public void Triangulate(int divisions)
    {
        divisions = Math.Clamp(divisions, 2, 100);
        Vertices.Clear();
        Triangles.Clear();

        int stride = divisions + 1;
        var grid = new Vertex[stride, stride];

        for (int iu = 0; iu <= divisions; iu++)
        {
            float u = iu / (float)divisions;
            for (int iv = 0; iv <= divisions; iv++)
            {
                float v = iv / (float)divisions;

                Vector3 p = EvaluatePoint(u, v);
                Vector3 pu = EvaluateDerivativeU(u, v);
                Vector3 pv = EvaluateDerivativeV(u, v);

                Vector3 puNorm = pu.LengthSquared() > 1e-8f ? Vector3.Normalize(pu) : Vector3.UnitX;
                Vector3 pvNorm = pv.LengthSquared() > 1e-8f ? Vector3.Normalize(pv) : Vector3.UnitY;

                Vector3 cross = Vector3.Cross(pu, pv);
                Vector3 normal = cross.LengthSquared() > 1e-8f ? Vector3.Normalize(cross) : Vector3.UnitZ;

                var vertex = new Vertex
                {
                    U = u,
                    V = v,
                    OriginalPosition = p,
                    OriginalPu = puNorm,
                    OriginalPv = pvNorm,
                    OriginalNormal = normal,
                    RotatedPosition = p,
                    RotatedPu = puNorm,
                    RotatedPv = pvNorm,
                    RotatedNormal = normal
                };

                grid[iu, iv] = vertex;
                Vertices.Add(vertex);
            }
        }

        for (int iu = 0; iu < divisions; iu++)
        {
            for (int iv = 0; iv < divisions; iv++)
            {
                Vertex v00 = grid[iu, iv];
                Vertex v10 = grid[iu + 1, iv];
                Vertex v01 = grid[iu, iv + 1];
                Vertex v11 = grid[iu + 1, iv + 1];

                Triangles.Add(new Triangle(v00, v10, v11));
                Triangles.Add(new Triangle(v00, v11, v01));
            }
        }
    }

    /// <summary>
    /// Obraca wierzchołki, wektory styczne, normalne oraz punkty kontrolne macierzą R.
    /// </summary>
    public void ApplyRotation(Matrix3x3 rotation)
    {
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                RotatedControlPoints[i, j] = rotation.Transform(ControlPoints[i, j]);

        foreach (var v in Vertices)
        {
            v.RotatedPosition = rotation.Transform(v.OriginalPosition);
            v.RotatedPu = Vector3.Normalize(rotation.Transform(v.OriginalPu));
            v.RotatedPv = Vector3.Normalize(rotation.Transform(v.OriginalPv));
            v.RotatedNormal = Vector3.Normalize(rotation.Transform(v.OriginalNormal));
        }
    }

    private Vector3 EvaluatePoint(float u, float v)
    {
        Vector3 sum = Vector3.Zero;
        for (int i = 0; i <= 3; i++)
        {
            float bu = Bernstein.EvaluateCubic(i, u);
            for (int j = 0; j <= 3; j++)
            {
                float bv = Bernstein.EvaluateCubic(j, v);
                sum += ControlPoints[i, j] * (bu * bv);
            }
        }
        return sum;
    }

    private Vector3 EvaluateDerivativeU(float u, float v)
    {
        Vector3 sum = Vector3.Zero;
        for (int i = 0; i <= 2; i++)
        {
            float bu = Bernstein.EvaluateQuadratic(i, u);
            for (int j = 0; j <= 3; j++)
            {
                float bv = Bernstein.EvaluateCubic(j, v);
                sum += (ControlPoints[i + 1, j] - ControlPoints[i, j]) * (bu * bv);
            }
        }
        return 3f * sum;
    }

    private Vector3 EvaluateDerivativeV(float u, float v)
    {
        Vector3 sum = Vector3.Zero;
        for (int i = 0; i <= 3; i++)
        {
            float bu = Bernstein.EvaluateCubic(i, u);
            for (int j = 0; j <= 2; j++)
            {
                float bv = Bernstein.EvaluateQuadratic(j, v);
                sum += (ControlPoints[i, j + 1] - ControlPoints[i, j]) * (bu * bv);
            }
        }
        return 3f * sum;
    }
}