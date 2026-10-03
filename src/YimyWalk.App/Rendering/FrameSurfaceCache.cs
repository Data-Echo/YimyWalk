using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using YimyWalk.Core.Characters;
using YimyWalk.Core.Resources;

namespace YimyWalk.App.Rendering;

internal sealed class FrameSurfaceCache : IDisposable
{
    private readonly Dictionary<SurfaceKey, RenderedSurface> _surfaces = new();

    public RenderedSurface Get(CharacterDefinition definition, CharacterFrame frame, bool mirrored, int scale = 1)
    {
        var key = new SurfaceKey(definition, frame.Index, mirrored, Math.Max(1, scale));
        if (_surfaces.TryGetValue(key, out var existing)) return existing;
        var created = RenderedSurface.Create(definition, frame, mirrored, key.Scale);
        _surfaces.Add(key, created);
        return created;
    }

    public void Dispose()
    {
        foreach (var surface in _surfaces.Values) surface.Dispose();
        _surfaces.Clear();
    }

    private readonly record struct SurfaceKey(CharacterDefinition Definition, int FrameIndex, bool Mirrored, int Scale);
}

internal sealed class RenderedSurface : IDisposable
{
    private RenderedSurface(Bitmap bitmap, byte[] alpha, DefinitionLease owner, DefinitionLease.LeaseHandle lease)
    {
        Bitmap = bitmap;
        Alpha = alpha;
        Owner = owner;
        Lease = lease;
    }

    public Bitmap Bitmap { get; }
    public byte[] Alpha { get; }
    private DefinitionLease Owner { get; }
    public DefinitionLease.LeaseHandle Lease { get; }

    public static RenderedSurface Create(CharacterDefinition definition, CharacterFrame frame, bool mirrored, int scale)
    {
        var bitmap = new Bitmap(frame.Width * scale, frame.Height * scale, PixelFormat.Format32bppPArgb);
        var alpha = new byte[bitmap.Width * bitmap.Height];
        var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
        try
        {
            var pixels = frame.RgbaPixels.Span;
            var stride = data.Stride;
            for (var y = 0; y < bitmap.Height; y++)
            for (var x = 0; x < bitmap.Width; x++)
            {
                var sourceX = mirrored ? frame.Width - 1 - (x / scale) : x / scale;
                var sourceY = y / scale;
                var source = (sourceY * frame.Width + sourceX) * 4;
                var a = pixels[source + 3];
                var destination = y * stride + x * 4;
                var rgbScale = a / 255f;
                Marshal.WriteByte(data.Scan0, destination, (byte)(pixels[source + 2] * rgbScale));
                Marshal.WriteByte(data.Scan0, destination + 1, (byte)(pixels[source + 1] * rgbScale));
                Marshal.WriteByte(data.Scan0, destination + 2, (byte)(pixels[source] * rgbScale));
                Marshal.WriteByte(data.Scan0, destination + 3, a);
                alpha[y * bitmap.Width + x] = a;
            }
        }
        finally { bitmap.UnlockBits(data); }

        // The cache owns one renderer reference per decoded definition, so an
        // old definition survives refresh until all windows and paints release.
        var definitionLease = new DefinitionLease(definition);
        return new RenderedSurface(bitmap, alpha, definitionLease, definitionLease.AcquireRenderer());
    }

    public void Dispose()
    {
        Bitmap.Dispose();
        Lease.Dispose();
        Owner.Dispose();
    }
}
