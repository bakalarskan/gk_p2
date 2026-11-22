using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;

namespace BezierSurfacesApp.Models;

public sealed class TextureBuffer
{
    public int Width { get; }
    public int Height { get; }
    public string Name { get; }

    private readonly Vector3[] _colors;
    private readonly Vector3[] _normals;

    private TextureBuffer(int width, int height, string name, Vector3[] colors, Vector3[] normals)
    {
        Width = width;
        Height = height;
        Name = name;
        _colors = colors;
        _normals = normals;
    }

    public static TextureBuffer FromFile(string path)
    {
        using var loaded = new Bitmap(path);
        return FromBitmap(loaded, Path.GetFileName(path));
    }

    public static TextureBuffer FromBitmap(Bitmap source, string name)
    {
        int w = source.Width;
        int h = source.Height;

        using var bmp32 = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp32))
        {
            g.DrawImage(source, 0, 0, w, h);
        }

        var rect = new Rectangle(0, 0, w, h);
        var data = bmp32.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        int[] pixels = new int[w * h];
        try
        {
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
        }
        finally
        {
            bmp32.UnlockBits(data);
        }

        var colors = new Vector3[pixels.Length];
        var normals = new Vector3[pixels.Length];

        for (int i = 0; i < pixels.Length; i++)
        {
            int argb = pixels[i];
            int r = (argb >> 16) & 0xFF;
            int g = (argb >> 8) & 0xFF;
            int b = argb & 0xFF;

            colors[i] = new Vector3(r / 255f, g / 255f, b / 255f);

            // Dekodowanie NormalMap: RGB(127, 127, 255) -> (0, 0, 1); B w [128..255] => Nz > 0
            float nx = (r == 127 || r == 128) ? 0f : (r / 127.5f) - 1f;
            float ny = (g == 127 || g == 128) ? 0f : (g / 127.5f) - 1f;
            float nz = MathF.Max(0.001f, (b / 127.5f) - 1f);
            normals[i] = Vector3.Normalize(new Vector3(nx, ny, nz));
        }

        return new TextureBuffer(w, h, name, colors, normals);
    }

    public Vector3 SampleColor(float u, float v)
    {
        int x = Math.Clamp((int)(u * (Width - 1)), 0, Width - 1);
        int y = Math.Clamp((int)((1f - v) * (Height - 1)), 0, Height - 1);
        return _colors[y * Width + x];
    }

    public Vector3 SampleNormal(float u, float v)
    {
        int x = Math.Clamp((int)(u * (Width - 1)), 0, Width - 1);
        int y = Math.Clamp((int)((1f - v) * (Height - 1)), 0, Height - 1);
        return _normals[y * Width + x];
    }

    /// <summary>
    /// Domyślna tekstura proceduralna dla powierzchni (wzór geometryczny / mozaika).
    /// </summary>
    public static TextureBuffer CreateDefaultColorTexture(int variant = 0)
    {
        const int size = 256;
        var colors = new Vector3[size * size];
        var normals = new Vector3[size * size];

        for (int y = 0; y < size; y++)
        {
            float v = 1f - y / (float)(size - 1);
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)(size - 1);
                int idx = y * size + x;
                normals[idx] = Vector3.UnitZ;

                if (variant % 2 == 0)
                {
                    // Szachownica z gradientem koralowo-kremowym
                    bool check = (((int)(u * 8)) + ((int)(v * 8))) % 2 == 0;
                    colors[idx] = check
                        ? new Vector3(0.86f, 0.36f + 0.2f * u, 0.45f + 0.15f * v)
                        : new Vector3(0.95f, 0.90f, 0.85f);
                }
                else
                {
                    // Pasy faliste morsko-turkusowe
                    float wave = 0.5f + 0.5f * MathF.Sin((u * 6f + 0.35f * MathF.Sin(v * MathF.Tau * 2f)) * MathF.Tau);
                    colors[idx] = Vector3.Lerp(
                        new Vector3(0.12f, 0.38f, 0.58f),
                        new Vector3(0.42f, 0.84f, 0.78f),
                        wave);
                }
            }
        }

        return new TextureBuffer(size, size, $"Domyślna tekstura #{variant + 1}", colors, normals);
    }

    /// <summary>
    /// Domyślne proceduralne mapy wektorów normalnych (wszystkie z Nz > 0).
    /// </summary>
    public static TextureBuffer CreateProceduralNormalMap(string preset)
    {
        const int size = 256;
        var colors = new Vector3[size * size];
        var normals = new Vector3[size * size];

        for (int y = 0; y < size; y++)
        {
            float v = 1f - y / (float)(size - 1);
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)(size - 1);
                Vector3 n;

                if (preset == "Cegły")
                {
                    float rows = 8f, cols = 4f;
                    int rowIdx = (int)(v * rows);
                    float localV = (v * rows) % 1f;
                    float localU = ((u * cols) + (rowIdx % 2 == 1 ? 0.5f : 0f)) % 1f;

                    float nx = 0f, ny = 0f;
                    const float bevel = 0.09f;
                    if (localU < bevel) nx = -0.65f;
                    else if (localU > 1f - bevel) nx = 0.65f;

                    if (localV < bevel * 1.6f) ny = -0.65f;
                    else if (localV > 1f - bevel * 1.6f) ny = 0.65f;

                    n = Vector3.Normalize(new Vector3(nx, ny, 1f));
                }
                else if (preset == "Pikowanie")
                {
                    float freq = 6f * MathF.PI;
                    float nx = 0.55f * MathF.Cos(u * freq) * MathF.Sin(v * freq);
                    float ny = 0.55f * MathF.Sin(u * freq) * MathF.Cos(v * freq);
                    n = Vector3.Normalize(new Vector3(nx, ny, 1f));
                }
                else // "Fale koncentryczne"
                {
                    float dx = u - 0.5f;
                    float dy = v - 0.5f;
                    float dist = MathF.Sqrt(dx * dx + dy * dy) + 1e-5f;
                    float slope = 0.55f * MathF.Cos(dist * 38f);
                    n = Vector3.Normalize(new Vector3((dx / dist) * slope, (dy / dist) * slope, 1f));
                }

                int idx = y * size + x;
                normals[idx] = n;
                colors[idx] = new Vector3((n.X + 1f) * 0.5f, (n.Y + 1f) * 0.5f, (n.Z + 1f) * 0.5f);
            }
        }

        return new TextureBuffer(size, size, $"Proceduralna: {preset}", colors, normals);
    }
}