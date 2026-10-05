using System.Numerics;
using YimyWalk.Core.Simulation;
using YimyWalk.Core.Display;

namespace YimyWalk.App.Host;

internal sealed class DesktopWorldSurface : IWorldSurface
{
    private static readonly WorldRect FallbackArea = new(0, 0, 1280, 720);
    private readonly WindowsDisplayAdapter _adapter = new();
    private readonly WindowSurfaceResolver _windowSurfaces;
    private DisplaySnapshot _snapshot;
    private DateTime _nextRefreshUtc;

    public DesktopWorldSurface()
    {
        _snapshot = _adapter.Capture();
        _nextRefreshUtc = DateTime.UtcNow.AddSeconds(1);
        _windowSurfaces = new WindowSurfaceResolver(point =>
            GetSnapshot().Resolve(point)?.WorkingArea ?? FallbackArea);
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

    public WorldRect SpawnArea
    {
        get
        {
            var snapshot = GetSnapshot();
            var preferred = PreferredDisplayId is { } id
                ? snapshot.Resolve(new Vector2(0, 0), id)
                : snapshot.Primary;
            return preferred?.WorkingArea ?? FallbackArea;
        }
    }

    public DisplayId? PreferredDisplayId { get; private set; }

    public bool IsValid => GetSnapshot().Displays.Count > 0;
    public bool WindowLandingEnabled { get => _windowSurfaces.Enabled; set => _windowSurfaces.Enabled = value; }
    public bool WindowSurfaceSnapshotValid => _windowSurfaces.HasValidSnapshot;
    public Func<IntPtr, bool>? ExcludeWindow { get => _windowSurfaces.ExcludeWindow; set => _windowSurfaces.ExcludeWindow = value; }
    public IReadOnlyList<WindowSurfaceCandidate> WindowSurfaceCandidates => _windowSurfaces.Candidates;

    public SurfaceSelection SurfaceForPet(Vector2 footPosition, float footHalfWidth, Vector2 nextFootPosition)
        => _windowSurfaces.Select(footPosition, footHalfWidth, nextFootPosition);

    public void RefreshWindowSurfaces() => _windowSurfaces.RefreshNow();

    public void SetPreferredDisplay(string? displayId)
        => PreferredDisplayId = string.IsNullOrWhiteSpace(displayId) ? null : new DisplayId(displayId);

    public float ScaleForWindow(IntPtr windowHandle) => _adapter.ScaleForWindow(windowHandle);

    public float ScaleForPoint(Vector2 point)
        => GetSnapshot().Resolve(point)?.Scale ?? 1f;

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

    public void Dispose() => _windowSurfaces.Dispose();
}
