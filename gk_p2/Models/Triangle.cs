namespace BezierSurfacesApp.Models;

public sealed class Triangle
{
    public Vertex V0 { get; }
    public Vertex V1 { get; }
    public Vertex V2 { get; }

    public Triangle(Vertex v0, Vertex v1, Vertex v2)
    {
        V0 = v0;
        V1 = v1;
        V2 = v2;
    }
}