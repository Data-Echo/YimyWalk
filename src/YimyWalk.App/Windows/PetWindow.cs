using System.Numerics;
using YimyWalk.App.Rendering;
using YimyWalk.Core.Simulation;

namespace YimyWalk.App.Windows;

internal sealed class PetWindow : Form
{
    private readonly PetEngine _engine;
    private readonly FrameSurfaceCache _cache;
    private RenderedSurface? _surface;
    private bool _dragging;
    private Point _lastCursor;
    private DateTime _lastCursorTime;
    private Vector2 _dragVelocity;

    public PetWindow(PetInstance instance, PetSession session, FrameSurfaceCache cache)
    {
        Instance = instance;
        _engine = new PetEngine(session.Surface, new Host.SystemRandomSource());
        _cache = cache;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        DoubleBuffered = false;
        BackColor = Color.Magenta;
        TransparencyKey = Color.Magenta;
        MouseDown += HandleMouseDown;
        MouseMove += HandleMouseMove;
        MouseUp += HandleMouseUp;
        Deactivate += (_, _) => ReleaseDrag();
        RefreshFrame();
    }

    public PetInstance Instance { get; }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= NativeMethods.WS_EX_LAYERED | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE;
            return parameters;
        }
    }

    public void RefreshFrame()
    {
        if (IsDisposed) return;
        var frame = Instance.Definition.SelectFrame(Instance.GetFrameKind());
        _surface = _cache.Get(Instance.Definition, frame, frame.Mirrored);
        ClientSize = _surface.Bitmap.Size;
        var area = _engine.Surface.WorkingArea;
        var x = (int)Math.Round(Instance.FootPosition.X - Instance.Definition.Anchor.X);
        var y = (int)Math.Round(Instance.FootPosition.Y - Instance.Definition.Anchor.Y);
        var location = new Point(x, y);
        Location = location;
        if (IsHandleCreated)
            LayeredWindowRenderer.Present(Handle, _surface.Bitmap, location);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_NCHITTEST && _surface is not null)
        {
            var point = PointToClient(Cursor.Position);
            if (point.X < 0 || point.Y < 0 || point.X >= _surface.Bitmap.Width || point.Y >= _surface.Bitmap.Height || _surface.Alpha[point.Y * _surface.Bitmap.Width + point.X] < 12)
            {
                m.Result = (IntPtr)NativeMethods.HTTRANSPARENT;
                return;
            }
            m.Result = (IntPtr)NativeMethods.HTCLIENT;
            return;
        }
        base.WndProc(ref m);
    }

    private void HandleMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _dragging = true;
        Capture = true;
        _lastCursor = Cursor.Position;
        _lastCursorTime = DateTime.UtcNow;
        _engine.BeginDrag(Instance);
    }

    private void HandleMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        var cursor = Cursor.Position;
        var now = DateTime.UtcNow;
        var delta = (float)Math.Max(0.001, (now - _lastCursorTime).TotalSeconds);
        var toss = new Vector2((cursor.X - _lastCursor.X) / delta, (cursor.Y - _lastCursor.Y) / delta);
        _lastCursor = cursor;
        _lastCursorTime = now;
        _engine.SetDraggedFootPosition(Instance, new Vector2(cursor.X + Instance.Definition.Anchor.X, cursor.Y + Instance.Definition.Anchor.Y));
        _dragVelocity = toss;
    }

    private void HandleMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) ReleaseDrag();
    }

    private void ReleaseDrag()
    {
        if (!_dragging) return;
        _dragging = false;
        Capture = false;
        _engine.ReleaseDrag(Instance, _dragVelocity);
        _dragVelocity = Vector2.Zero;
    }
}
