using YimyWalk.App.Rendering;
using YimyWalk.Core.Characters;

namespace YimyWalk.App.UI.Controls;

internal sealed class CharacterFramePreview : UserControl
{
    private readonly FrameSurfaceCache _cache;
    private CharacterDefinition? _definition;
    private RenderedSurface? _surface;

    public CharacterFramePreview(FrameSurfaceCache cache)
    {
        _cache = cache;
        BackColor = Color.FromArgb(255, 246, 224);
        BorderStyle = BorderStyle.FixedSingle;
        MinimumSize = new Size(120, 120);
        Size = new Size(180, 180);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Resize += (_, _) => Invalidate();
    }

    public CharacterDefinition? Definition => _definition;
    public int? CurrentFrameIndex => _surface is null ? null : 0;

    public void ShowIdleFrame(CharacterDefinition? definition, float scale = 2f)
    {
        if (ReferenceEquals(_definition, definition) && _surface is not null) return;
        ReleaseCurrent();
        if (definition is null || definition.IsDisposed || definition.Frames.Count == 0) return;

        _definition = definition;
        _cache.RetainDefinition(definition);
        try
        {
            var frame = definition.SelectFrame(FrameKind.Idle, 0);
            // SelectFrame(..., 0) is the required idle1/index 0 preview frame.
            // Keep the source at its original pixel resolution. Preview sizing
            // is a presentation concern and must not change pet/definition scale.
            _surface = _cache.Get(definition, frame, mirrored: false, 1f);
            Invalidate();
        }
        catch
        {
            ReleaseCurrent();
            throw;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) ReleaseCurrent();
        base.Dispose(disposing);
    }

    private void ReleaseCurrent()
    {
        if (_definition is not null) _cache.ReleaseDefinition(_definition);
        _definition = null;
        _surface = null;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_surface is null || _surface.Bitmap.Width <= 0 || _surface.Bitmap.Height <= 0) return;

        var client = ClientRectangle;
        client.Inflate(-Padding.Horizontal / 2, -Padding.Vertical / 2);
        const int margin = 4;
        client.Inflate(-margin, -margin);
        if (client.Width <= 0 || client.Height <= 0) return;
        var source = _surface.Bitmap;
        var ratio = Math.Min(client.Width / (float)source.Width, client.Height / (float)source.Height);
        if (ratio <= 0) return;
        var destinationWidth = Math.Max(1, (int)Math.Round(source.Width * ratio));
        var destinationHeight = Math.Max(1, (int)Math.Round(source.Height * ratio));
        var destination = new Rectangle(
            client.X + (client.Width - destinationWidth) / 2,
            client.Y + (client.Height - destinationHeight) / 2,
            destinationWidth,
            destinationHeight);

        var state = e.Graphics.Save();
        try
        {
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            e.Graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            e.Graphics.DrawImage(source, destination, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel);
        }
        finally { e.Graphics.Restore(state); }
    }
}
