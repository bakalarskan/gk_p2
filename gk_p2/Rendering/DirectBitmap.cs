using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace BezierSurfacesApp.Rendering;

public sealed class DirectBitmap : IDisposable
{
    public Bitmap Bitmap { get; }
    public int[] Bits { get; }
    public float[] ZBuffer { get; }
    public int Width { get; }
    public int Height { get; }

    private readonly GCHandle _bitsHandle;
    private bool _disposed;

    public DirectBitmap(int width, int height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);

        Bits = new int[Width * Height];
        ZBuffer = new float[Width * Height];
        _bitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);

        Bitmap = new Bitmap(
            Width,
            Height,
            Width * 4,
            PixelFormat.Format32bppArgb,
            _bitsHandle.AddrOfPinnedObject());
    }

    public void Clear(Color backgroundColor)
    {
        Array.Fill(Bits, backgroundColor.ToArgb());
        Array.Fill(ZBuffer, float.NegativeInfinity);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Bitmap.Dispose();
        if (_bitsHandle.IsAllocated)
            _bitsHandle.Free();
    }
}