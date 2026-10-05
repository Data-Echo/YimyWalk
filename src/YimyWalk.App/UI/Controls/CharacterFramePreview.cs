using YimyWalk.App.Rendering;
using YimyWalk.Core.Characters;

namespace YimyWalk.App.UI.Controls;

internal sealed class CharacterFramePreview : UserControl
{
    private readonly FrameSurfaceCache _cache;
    private readonly PictureBox _picture = new()
    {
        SizeMode = PictureBoxSizeMode.CenterImage,
        BackColor = Color.Transparent,
        Dock = DockStyle.Fill,
        TabStop = false
    };
    private CharacterDefinition? _definition;
    private RenderedSurface? _surface;

    public CharacterFramePreview(FrameSurfaceCache cache)
    {
        _cache = cache;
        BackColor = Color.FromArgb(255, 246, 224);
        BorderStyle = BorderStyle.FixedSingle;
        MinimumSize = new Size(120, 120);
        Size = new Size(180, 180);
        Controls.Add(_picture);
    }

    public CharacterDefinition? Definition => _definition;
    public int? CurrentFrameIndex => _surface is null ? null : 0;

    public void ShowIdleFrame(CharacterDefinition? definition, float scale = 2f)
    {
        if (ReferenceEquals(_definition, definition) && _picture.Image is not null) return;
        ReleaseCurrent();
        if (definition is null || definition.IsDisposed || definition.Frames.Count == 0) return;

        _definition = definition;
        _cache.RetainDefinition(definition);
        try
        {
            var frame = definition.SelectFrame(FrameKind.Idle, 0);
            // SelectFrame(..., 0) is the required idle1/index 0 preview frame.
            _surface = _cache.Get(definition, frame, mirrored: false, Math.Clamp(scale, 0.1f, 8f));
            _picture.Image = _surface.Bitmap;
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
        _picture.Image = null;
        if (_definition is not null) _cache.ReleaseDefinition(_definition);
        _definition = null;
        _surface = null;
    }
}
