using System.Drawing.Drawing2D;
using System.Numerics;
using BezierSurfacesApp.Models;

namespace BezierSurfacesApp.Rendering;

public sealed class SceneRenderer
{
    private static readonly Color BackgroundColor = Color.FromArgb(24, 27, 34);
    private readonly ScanlineRasterizer _rasterizer = new();

    public void Render(DirectBitmap buffer, Scene scene, int hoveredLightPointIndex = -1)
    {
        buffer.Clear(BackgroundColor);

        // 1. Wypełnione trójkąty z oświetleniem i Z-bufferem
        if (scene.ShowFilledTriangles)
        {
            foreach (var surface in scene.Surfaces)
            {
                _rasterizer.RasterizeSurface(buffer, surface, scene);
            }
        }

        // 2. Nakładki wektorowe (siatka, wieloboki Béziera, krzywa światła)
        using var g = Graphics.FromImage(buffer.Bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        float halfW = buffer.Width * 0.5f;
        float halfH = buffer.Height * 0.5f;

        if (scene.ShowWireframe)
            DrawWireframe(g, scene, halfW, halfH);

        if (scene.ShowControlPolygon)
            DrawControlPolygons(g, scene, halfW, halfH);

        if (scene.ShowLightCurve || scene.EditLightCurveMode)
            DrawLightCurve(g, scene, halfW, halfH, hoveredLightPointIndex);
    }

    private static PointF Project(Vector3 p, float halfW, float halfH) =>
        new(halfW + p.X, halfH - p.Y);

    private static PointF Project(Vector2 p, float halfW, float halfH) =>
        new(halfW + p.X, halfH - p.Y);

    private static void DrawWireframe(Graphics g, Scene scene, float halfW, float halfH)
    {
        using var pen = new Pen(Color.FromArgb(145, 225, 235, 245), 1f);

        foreach (var surface in scene.Surfaces)
        {
            foreach (var tri in surface.Triangles)
            {
                PointF p0 = Project(tri.V0.RotatedPosition, halfW, halfH);
                PointF p1 = Project(tri.V1.RotatedPosition, halfW, halfH);
                PointF p2 = Project(tri.V2.RotatedPosition, halfW, halfH);

                g.DrawLine(pen, p0, p1);
                g.DrawLine(pen, p1, p2);
                g.DrawLine(pen, p2, p0);
            }
        }
    }

    private static void DrawControlPolygons(Graphics g, Scene scene, float halfW, float halfH)
    {
        foreach (var surface in scene.Surfaces)
        {
            using var linePen = new Pen(Color.FromArgb(200, surface.ControlNetColor), 1.5f)
            {
                DashStyle = DashStyle.Dash
            };
            using var pointBrush = new SolidBrush(surface.ControlNetColor);

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    PointF p = Project(surface.RotatedControlPoints[i, j], halfW, halfH);

                    if (i < 3)
                    {
                        PointF pRight = Project(surface.RotatedControlPoints[i + 1, j], halfW, halfH);
                        g.DrawLine(linePen, p, pRight);
                    }
                    if (j < 3)
                    {
                        PointF pDown = Project(surface.RotatedControlPoints[i, j + 1], halfW, halfH);
                        g.DrawLine(linePen, p, pDown);
                    }

                    g.FillEllipse(pointBrush, p.X - 3.5f, p.Y - 3.5f, 7f, 7f);
                }
            }
        }
    }

    private static void DrawLightCurve(Graphics g, Scene scene, float halfW, float halfH, int hoveredIdx)
    {
        var curve = scene.LightCurve;
        const int samples = 120;
        var curvePoints = new PointF[samples + 1];

        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            curvePoints[i] = Project(curve.Evaluate2D(t), halfW, halfH);
        }

        using (var curvePen = new Pen(Color.FromArgb(180, 255, 225, 110), 1.8f))
        {
            g.DrawLines(curvePen, curvePoints);
        }

        // Wielobok kontrolny i uchwyty punktów w trybie edycji krzywej
        if (scene.EditLightCurveMode)
        {
            using var polyPen = new Pen(Color.FromArgb(110, 255, 180, 90), 1f) { DashStyle = DashStyle.Dot };
            for (int i = 0; i < curve.ControlPoints.Count - 1; i++)
            {
                PointF a = Project(curve.ControlPoints[i], halfW, halfH);
                PointF b = Project(curve.ControlPoints[i + 1], halfW, halfH);
                g.DrawLine(polyPen, a, b);
            }

            using var font = new Font("Segoe UI", 8f, FontStyle.Bold);
            for (int i = 0; i < curve.ControlPoints.Count - 1; i++)
            {
                PointF pt = Project(curve.ControlPoints[i], halfW, halfH);
                float r = (i == hoveredIdx) ? 7.5f : 5.5f;

                using var fill = new SolidBrush(i == 0 ? Color.OrangeRed : Color.Gold);
                using var border = new Pen(Color.White, 1.5f);

                g.FillEllipse(fill, pt.X - r, pt.Y - r, r * 2, r * 2);
                g.DrawEllipse(border, pt.X - r, pt.Y - r, r * 2, r * 2);

                string label = i == 0 ? $"V0=V{curve.ControlPoints.Count - 1}" : $"V{i}";
                g.DrawString(label, font, Brushes.WhiteSmoke, pt.X + 8, pt.Y - 8);
            }
        }

        // Aktualne położenie źródła światła na krzywej
        Vector3 lightPos = curve.CurrentLightPosition;
        PointF lightScreen = Project(new Vector2(lightPos.X, lightPos.Y), halfW, halfH);

        using var haloBrush = new SolidBrush(Color.FromArgb(80, scene.LightColor));
        using var coreBrush = new SolidBrush(scene.LightColor);
        using var ringPen = new Pen(Color.Gold, 2f);

        g.FillEllipse(haloBrush, lightScreen.X - 13, lightScreen.Y - 13, 26, 26);
        g.FillEllipse(coreBrush, lightScreen.X - 7, lightScreen.Y - 7, 14, 14);
        g.DrawEllipse(ringPen, lightScreen.X - 7, lightScreen.Y - 7, 14, 14);
    }
}