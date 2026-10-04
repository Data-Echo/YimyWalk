using YimyWalk.Core.Simulation;
using YimyWalk.Core.Display;

namespace YimyWalk.App.Host;

internal sealed class DesktopWorldSurface : IWorldSurface
{
    private static readonly WorldRect FallbackArea = new(0, 0, 1280, 720);
    private readonly WindowsDisplayAdapter _adapter = new();
    private DisplaySnapshot _snapshot;
    private DateTime _nextRefreshUtc;

    public DesktopWorldSurface()
    {
        _snapshot = _adapter.Capture();
        _nextRefreshUtc = DateTime.UtcNow.AddSeconds(1);
    }

    public IDisplayProvider DisplayProvider => GetSnapshot();

    public DisplaySnapshot Snapshot => GetSnapshot();

    public WorldRect WorkingArea
    {
        get
        {
            return GetSnapshot().Primary?.WorkingArea ?? FallbackArea;
        }
    }

    public bool IsValid => GetSnapshot().Displays.Count > 0;

    public void RefreshNow()
    {
        _snapshot = _adapter.Capture();
        _nextRefreshUtc = DateTime.UtcNow.AddSeconds(1);
    }

    public void RefreshForWindow(IntPtr windowHandle)
    {
        _snapshot = _adapter.Capture(windowHandle);
        _nextRefreshUtc = DateTime.UtcNow.AddSeconds(1);
    }

    private DisplaySnapshot GetSnapshot()
    {
        if (DateTime.UtcNow >= _nextRefreshUtc)
        {
            _snapshot = _adapter.Capture();
            _nextRefreshUtc = DateTime.UtcNow.AddSeconds(1);
        }

        return _snapshot;
    }
}
