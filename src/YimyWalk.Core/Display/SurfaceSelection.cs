using YimyWalk.Core.Simulation;

namespace YimyWalk.Core.Display;

public readonly record struct WindowSurfaceCandidate(
    string Id,
    WorldRect Bounds,
    bool IsVisible,
    bool IsMinimized,
    bool IsExcluded,
    bool IsStale,
    bool IsOccluded = false,
    int ZOrder = int.MaxValue,
    bool IsOcclusionBlocker = true,
    bool IsMaximized = false)
{
    public bool IsValid
        => !string.IsNullOrWhiteSpace(Id)
            && Bounds.Width > 0
            && Bounds.Height > 0;
}

public readonly record struct SurfaceSelection(WorldRect Area, bool IsFallback, string? SourceId);

public static class SurfaceSelector
{
    public static SurfaceSelection Select(
        IEnumerable<WindowSurfaceCandidate> candidates,
        WorldRect fallback,
        float footLeft,
        float footRight,
        float currentY,
        float nextY)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (footRight < footLeft) (footLeft, footRight) = (footRight, footLeft);
        var descending = nextY >= currentY;
        WindowSurfaceCandidate? best = null;
        var bestDistance = float.PositiveInfinity;

        if (descending)
        {
            foreach (var candidate in candidates)
            {
                if (!candidate.IsValid || !candidate.IsVisible || candidate.IsMinimized || candidate.IsExcluded || candidate.IsStale || candidate.IsOccluded || candidate.IsMaximized)
                    continue;
                if (candidate.Bounds.Right <= footLeft || candidate.Bounds.Left >= footRight)
                    continue;
                if (candidate.Bounds.Top < currentY || candidate.Bounds.Top > nextY)
                    continue;

                var distance = candidate.Bounds.Top - currentY;
                if (distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }
        }

        if (!best.HasValue)
            return new SurfaceSelection(fallback, IsFallback: true, SourceId: null);

        var selected = best.Value;
        return new SurfaceSelection(
            new WorldRect(selected.Bounds.Left, selected.Bounds.Top, selected.Bounds.Right, selected.Bounds.Top),
            IsFallback: false,
            selected.Id);
    }
}
