using System.Numerics;

namespace BezierSurfacesApp.Models;

public sealed class Vertex
{
    // Stan przed obrotem
    public Vector3 OriginalPosition { get; set; }
    public Vector3 OriginalPu { get; set; }
    public Vector3 OriginalPv { get; set; }
    public Vector3 OriginalNormal { get; set; }

    // Stan po obrocie
    public Vector3 RotatedPosition { get; set; }
    public Vector3 RotatedPu { get; set; }
    public Vector3 RotatedPv { get; set; }
    public Vector3 RotatedNormal { get; set; }

    // Parametry powierzchni z zakresu [0, 1]
    public float U { get; set; }
    public float V { get; set; }
}