using System.Globalization;
using System.Numerics;
using System.Text;
using YimyWalk.App.Windows;
using YimyWalk.Core.Display;
using YimyWalk.Core.Simulation;

namespace YimyWalk.App.Host;

/// <summary>
/// Reads a low-frequency snapshot of visible top-level windows and delegates
/// the safety/ordering rules to the platform-neutral SurfaceSelector.
/// </summary>
internal sealed class WindowSurfaceResolver : IDisposable
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(250);
    private const int MinimumSurfaceWidth = 32;
    private const int MinimumSurfaceHeight = 16;
    private readonly uint _hostProcessId = unchecked((uint)Environment.ProcessId);
    private readonly Func<Vector2, WorldRect> _fallback;
    private IReadOnlyList<WindowSurfaceCandidate> _candidates = Array.Empty<WindowSurfaceCandidate>();
    private DateTime _nextRefreshUtc;
    private bool _hasSnapshot;
    private bool _disposed;

    public WindowSurfaceResolver(Func<Vector2, WorldRect> fallback)
    {
        _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
    }

    public bool Enabled { get; set; }
    public bool HasValidSnapshot => _hasSnapshot;
    public Func<IntPtr, bool>? ExcludeWindow { get; set; }
    public IReadOnlyList<WindowSurfaceCandidate> Candidates => _candidates;

    public SurfaceSelection Select(Vector2 footPosition, float footHalfWidth, float nextFootY)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var fallback = _fallback(footPosition);
        if (!Enabled) return new SurfaceSelection(fallback, IsFallback: true, SourceId: null);
        RefreshIfNeeded();
        if (!_hasSnapshot) return new SurfaceSelection(fallback, IsFallback: true, SourceId: null);

        return SurfaceSelector.Select(
            _candidates,
            fallback,
            footPosition.X - footHalfWidth,
            footPosition.X + footHalfWidth,
            footPosition.Y,
            nextFootY);
    }

    public void RefreshNow()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var candidates = new List<WindowSurfaceCandidate>();
        try
        {
            NativeMethods.EnumWindows((window, _) =>
            {
                if (!NativeMethods.IsWindow(window) || !NativeMethods.IsWindowVisible(window) || NativeMethods.IsIconic(window))
                    return true;
                if (IsDesktopShellWindow(window)) return true;
                if (NativeMethods.GetWindowThreadProcessId(window, out var processId) == 0)
                    return true;
                if (!NativeMethods.GetWindowRect(window, out var rectangle) || rectangle.Right <= rectangle.Left || rectangle.Bottom <= rectangle.Top)
                    return true;
                if (rectangle.Right - rectangle.Left < MinimumSurfaceWidth
                    || rectangle.Bottom - rectangle.Top < MinimumSurfaceHeight)
                    return true;

                var excluded = processId == _hostProcessId
                    && (ExcludeWindow is null || ExcludeWindow(window));
                var id = window.ToInt64().ToString("X", CultureInfo.InvariantCulture);
                candidates.Add(new WindowSurfaceCandidate(
                    id,
                    new WorldRect(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom),
                    IsVisible: true,
                    IsMinimized: false,
                    IsExcluded: excluded,
                    IsStale: false));
                return true;
            }, IntPtr.Zero);
            _candidates = candidates;
            _hasSnapshot = true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            _candidates = Array.Empty<WindowSurfaceCandidate>();
            _hasSnapshot = false;
        }

        _nextRefreshUtc = DateTime.UtcNow.Add(RefreshInterval);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _candidates = Array.Empty<WindowSurfaceCandidate>();
        _hasSnapshot = false;
    }

    private void RefreshIfNeeded()
    {
        if (DateTime.UtcNow >= _nextRefreshUtc) RefreshNow();
    }

    private static bool IsDesktopShellWindow(IntPtr window)
    {
        var name = new StringBuilder(128);
        if (NativeMethods.GetClassName(window, name, name.Capacity) <= 0) return false;
        // These are desktop shell, DWM/input, or UWP child/surface windows.
        // They can be visible and cover the screen while not representing a
        // user window whose top edge is safe to land on.
        return name.ToString() is
            "Progman"
            or "WorkerW"
            or "Shell_TrayWnd"
            or "Shell_SecondaryTrayWnd"
            or "LockScreenBackstopFrame"
            or "ThumbnailDeviceHelperWnd"
            or "EdgeUiInputTopWndClass"
            or "DummyDWMListenerWindow"
            or "Windows.UI.Core.CoreWindow";
    }
}
