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

    public SurfaceSelection Select(Vector2 footPosition, float footHalfWidth, Vector2 nextFootPosition)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var fallback = _fallback(nextFootPosition);
        if (!Enabled) return new SurfaceSelection(fallback, IsFallback: true, SourceId: null);
        RefreshIfNeeded();
        if (!_hasSnapshot) return new SurfaceSelection(fallback, IsFallback: true, SourceId: null);

        var selection = SurfaceSelector.Select(
            _candidates,
            fallback,
            nextFootPosition.X - footHalfWidth,
            nextFootPosition.X + footHalfWidth,
            footPosition.Y,
            nextFootPosition.Y);
        if (!selection.IsFallback && selection.SourceId is { } sourceId && !IsLiveCandidate(sourceId))
        {
            RefreshNow();
            return SurfaceSelector.Select(
                _candidates,
                _fallback(nextFootPosition),
                nextFootPosition.X - footHalfWidth,
                nextFootPosition.X + footHalfWidth,
                footPosition.Y,
                nextFootPosition.Y);
        }

        return selection;
    }

    public void RefreshNow()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var candidates = new List<WindowSurfaceCandidate>();
        var zOrder = 0;
        try
        {
            var enumerationSucceeded = NativeMethods.EnumWindows((window, _) =>
            {
                if (!NativeMethods.IsWindow(window) || !NativeMethods.IsWindowVisible(window) || NativeMethods.IsIconic(window))
                    return true;
                var className = GetWindowClassName(window);
                if (NativeMethods.GetWindowThreadProcessId(window, out var processId) == 0)
                    return true;
                if (!NativeMethods.GetWindowRect(window, out var rectangle) || rectangle.Right <= rectangle.Left || rectangle.Bottom <= rectangle.Top)
                    return true;

                var tooSmall = rectangle.Right - rectangle.Left < MinimumSurfaceWidth
                    || rectangle.Bottom - rectangle.Top < MinimumSurfaceHeight;
                var hostExcluded = processId == _hostProcessId
                    && (ExcludeWindow is null || ExcludeWindow(window));
                var unsafeSurface = IsUnsafeSurfaceClass(className);
                var isBackgroundShell = className is "Progman" or "WorkerW";
                var id = window.ToInt64().ToString("X", CultureInfo.InvariantCulture);
                candidates.Add(new WindowSurfaceCandidate(
                    id,
                    new WorldRect(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom),
                    IsVisible: true,
                    IsMinimized: false,
                    IsExcluded: hostExcluded || unsafeSurface || tooSmall,
                    IsStale: false,
                    IsOccluded: false,
                    ZOrder: zOrder++,
                    IsOcclusionBlocker: !hostExcluded && !isBackgroundShell && !tooSmall));
                return true;
            }, IntPtr.Zero);
            if (!enumerationSucceeded)
            {
                _candidates = Array.Empty<WindowSurfaceCandidate>();
                _hasSnapshot = false;
                _nextRefreshUtc = DateTime.UtcNow.Add(RefreshInterval);
                return;
            }

            for (var index = 0; index < candidates.Count; index++)
            {
                var candidate = candidates[index];
                var occluded = candidates.Any(higher =>
                    higher.ZOrder < candidate.ZOrder
                    && higher.IsVisible
                    && !higher.IsMinimized
                    && higher.IsOcclusionBlocker
                    && higher.Bounds.Left < candidate.Bounds.Right
                    && higher.Bounds.Right > candidate.Bounds.Left
                    && higher.Bounds.Top <= candidate.Bounds.Top
                    && higher.Bounds.Bottom > candidate.Bounds.Top);
                candidates[index] = candidate with { IsOccluded = occluded };
            }

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

    private bool IsLiveCandidate(string sourceId)
    {
        if (!long.TryParse(sourceId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var raw)) return false;
        var window = new IntPtr(raw);
        if (!NativeMethods.IsWindow(window) || !NativeMethods.IsWindowVisible(window) || NativeMethods.IsIconic(window)) return false;
        if (!NativeMethods.GetWindowRect(window, out var rectangle)) return false;
        var candidate = _candidates.FirstOrDefault(item => item.Id.Equals(sourceId, StringComparison.OrdinalIgnoreCase));
        return candidate.IsValid
            && candidate.Bounds.Left == rectangle.Left
            && candidate.Bounds.Top == rectangle.Top
            && candidate.Bounds.Right == rectangle.Right
            && candidate.Bounds.Bottom == rectangle.Bottom;
    }

    private static string GetWindowClassName(IntPtr window)
    {
        var name = new StringBuilder(128);
        return NativeMethods.GetClassName(window, name, name.Capacity) > 0 ? name.ToString() : string.Empty;
    }

    private static bool IsUnsafeSurfaceClass(string className)
        => className is
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
