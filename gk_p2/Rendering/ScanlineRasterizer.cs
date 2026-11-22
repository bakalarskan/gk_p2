using System.Numerics;
using BezierSurfacesApp.Models;

namespace BezierSurfacesApp.Rendering;

public sealed class ScanlineRasterizer
{
    private struct EdgeEntry
    {
        public int YMax;
        public float CurrentX;
        public float InvSlope; // dx / dy (1/m)
    }

    private List<EdgeEntry>[] _edgeTable = Array.Empty<List<EdgeEntry>>();
    private readonly List<EdgeEntry> _activeEdgeTable = new(4);
    private int _allocatedHeight;

    private void EnsureBucketTable(int height)
    {
        if (_allocatedHeight == height) return;

        _edgeTable = new List<EdgeEntry>[height];
        for (int i = 0; i < height; i++)
            _edgeTable[i] = new List<EdgeEntry>(4);

        _allocatedHeight = height;
    }

    public void RasterizeSurface(DirectBitmap target, BezierSurface surface, Scene scene)
    {
        int width = target.Width;
        int height = target.Height;
        EnsureBucketTable(height);

        float halfW = width * 0.5f;
        float halfH = height * 0.5f;

        Vector3 lightPos = scene.LightCurve.CurrentLightPosition;
        Vector3 lightColor = scene.LightColorNormalized;
        bool useNormalMap = scene.UseNormalMap;

        int[] bits = target.Bits;
        float[] zBuffer = target.ZBuffer;

        foreach (var tri in surface.Triangles)
        {
            Vertex v0 = tri.V0;
            Vertex v1 = tri.V1;
            Vertex v2 = tri.V2;

            // Rzut prostokątny na płaszczyznę xy ze środkiem w (width/2, height/2), oś Y w górę
            float x0 = halfW + v0.RotatedPosition.X;
            float y0 = halfH - v0.RotatedPosition.Y;
            float x1 = halfW + v1.RotatedPosition.X;
            float y1 = halfH - v1.RotatedPosition.Y;
            float x2 = halfW + v2.RotatedPosition.X;
            float y2 = halfH - v2.RotatedPosition.Y;

            // Wyznacznik do współrzędnych barycentrycznych
            float det = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2);
            if (MathF.Abs(det) < 1e-4f)
                continue; // trójkąt zdegenerowany (powierzchnia 0)

            float invDet = 1f / det;

            // Budowa kubełkowej tablicy krawędzi (Edge Table - ET)
            int scanYMin = height;
            int scanYMax = -1;

            AddEdgeToBuckets(x0, y0, x1, y1, height, ref scanYMin, ref scanYMax);
            AddEdgeToBuckets(x1, y1, x2, y2, height, ref scanYMin, ref scanYMax);
            AddEdgeToBuckets(x2, y2, x0, y0, height, ref scanYMin, ref scanYMax);

            if (scanYMin >= scanYMax)
                continue;

            _activeEdgeTable.Clear();

            // Prekalkulacja przyrostów współrzędnych barycentrycznych wzdłuż osi X
            float dAlphaDx = (y1 - y2) * invDet;
            float dBetaDx = (y2 - y0) * invDet;

            float z0 = v0.RotatedPosition.Z;
            float z1 = v1.RotatedPosition.Z;
            float z2 = v2.RotatedPosition.Z;

            // Główna pętla algorytmu scan-line po liniach poziomych Y
            for (int y = scanYMin; y < scanYMax; y++)
            {
                // 1. Przenieś krawędzie z kubełka ET[y] do aktywnej listy AET
                var bucket = _edgeTable[y];
                if (bucket.Count > 0)
                {
                    for (int i = 0; i < bucket.Count; i++)
                        _activeEdgeTable.Add(bucket[i]);
                    bucket.Clear();
                }

                // 2. Usuń z AET krawędzie kończące się na bieżącym Y
                for (int i = _activeEdgeTable.Count - 1; i >= 0; i--)
                {
                    if (_activeEdgeTable[i].YMax <= y)
                        _activeEdgeTable.RemoveAt(i);
                }

                if (_activeEdgeTable.Count < 2)
                    continue;

                // 3. Posortuj AET rosnąco po bieżącej współrzędnej X
                SortActiveEdges();

                int rowOffset = y * width;
                float worldY = halfH - y;

                // 4. Wypełniaj piksele między parami przecięć w AET
                for (int i = 0; i + 1 < _activeEdgeTable.Count; i += 2)
                {
                    int xStart = (int)MathF.Ceiling(_activeEdgeTable[i].CurrentX);
                    int xEnd = (int)MathF.Ceiling(_activeEdgeTable[i + 1].CurrentX) - 1;

                    if (xStart < 0) xStart = 0;
                    if (xEnd >= width) xEnd = width - 1;
                    if (xStart > xEnd) continue;

                    // Początkowe współrzędne barycentryczne dla (xStart, y)
                    float alpha = ((y1 - y2) * (xStart - x2) + (x2 - x1) * (y - y2)) * invDet;
                    float beta = ((y2 - y0) * (xStart - x2) + (x0 - x2) * (y - y2)) * invDet;

                    for (int x = xStart; x <= xEnd; x++)
                    {
                        float gamma = 1f - alpha - beta;

                        // Interpolacja głębokości Z
                        float z = alpha * z0 + beta * z1 + gamma * z2;
                        int pixelIdx = rowOffset + x;

                        // Test bufora głębokości (Z-buffer): bliżej obserwatora V=(0,0,1) => większe Z
                        if (z > zBuffer[pixelIdx])
                        {
                            zBuffer[pixelIdx] = z;

                            // Interpolacja wektora normalnego i parametrów (u, v)
                            Vector3 interpN = alpha * v0.RotatedNormal + beta * v1.RotatedNormal + gamma * v2.RotatedNormal;
                            Vector3 nNorm = interpN.LengthSquared() > 1e-8f ? Vector3.Normalize(interpN) : Vector3.UnitZ;

                            float u = Math.Clamp(alpha * v0.U + beta * v1.U + gamma * v2.U, 0f, 1f);
                            float v = Math.Clamp(alpha * v0.V + beta * v1.V + gamma * v2.V, 0f, 1f);

                            Vector3 puNorm = Vector3.UnitX;
                            Vector3 pvNorm = Vector3.UnitY;
                            if (useNormalMap)
                            {
                                Vector3 interpPu = alpha * v0.RotatedPu + beta * v1.RotatedPu + gamma * v2.RotatedPu;
                                Vector3 interpPv = alpha * v0.RotatedPv + beta * v1.RotatedPv + gamma * v2.RotatedPv;
                                puNorm = interpPu.LengthSquared() > 1e-8f ? Vector3.Normalize(interpPu) : Vector3.UnitX;
                                pvNorm = interpPv.LengthSquared() > 1e-8f ? Vector3.Normalize(interpPv) : Vector3.UnitY;
                            }

                            Vector3 fragPos = new(x - halfW, worldY, z);
                            bits[pixelIdx] = LightingModel.ComputePixelArgb(
                                fragPos, nNorm, puNorm, pvNorm, u, v, surface, scene, lightPos, lightColor);
                        }

                        alpha += dAlphaDx;
                        beta += dBetaDx;
                    }
                }

                // 5. Zaktualizuj współrzędną X w aktywnych krawędziach: x += 1/m
                for (int i = 0; i < _activeEdgeTable.Count; i++)
                {
                    EdgeEntry e = _activeEdgeTable[i];
                    e.CurrentX += e.InvSlope;
                    _activeEdgeTable[i] = e;
                }
            }

            // Bezpieczne wyczyszczenie ewentualnych pozostałości kubełków na granicy Y
            for (int y = scanYMin; y < scanYMax && y < height; y++)
                _edgeTable[y].Clear();
        }
    }

    private void AddEdgeToBuckets(
        float ax, float ay, float bx, float by,
        int viewportHeight, ref int scanYMin, ref int scanYMax)
    {
        // Uporządkowanie wierzchołków krawędzi tak, aby ay <= by
        if (ay > by)
        {
            (ax, bx) = (bx, ax);
            (ay, by) = (by, ay);
        }

        int yStart = (int)MathF.Ceiling(ay);
        int yEnd = (int)MathF.Ceiling(by);

        // Pomijamy krawędzie poziome oraz całkowicie poza obszarem rysowania
        if (yStart >= yEnd || yEnd <= 0 || yStart >= viewportHeight)
            return;

        float invSlope = (bx - ax) / (by - ay);
        float startX = ax + (yStart - ay) * invSlope;

        // Przycinanie krawędzi do górnej i dolnej granicy ekranu
        if (yStart < 0)
        {
            startX += (-yStart) * invSlope;
            yStart = 0;
        }
        if (yEnd > viewportHeight)
        {
            yEnd = viewportHeight;
        }

        _edgeTable[yStart].Add(new EdgeEntry
        {
            YMax = yEnd,
            CurrentX = startX,
            InvSlope = invSlope
        });

        if (yStart < scanYMin) scanYMin = yStart;
        if (yEnd > scanYMax) scanYMax = yEnd;
    }

    private void SortActiveEdges()
    {
        // Sortowanie przez wstawianie – optymalne dla małej liczby aktywnych krawędzi (2..3)
        for (int i = 1; i < _activeEdgeTable.Count; i++)
        {
            EdgeEntry key = _activeEdgeTable[i];
            int j = i - 1;
            while (j >= 0 && _activeEdgeTable[j].CurrentX > key.CurrentX)
            {
                _activeEdgeTable[j + 1] = _activeEdgeTable[j];
                j--;
            }
            _activeEdgeTable[j + 1] = key;
        }
    }
}