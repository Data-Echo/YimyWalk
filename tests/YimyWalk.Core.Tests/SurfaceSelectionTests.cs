using YimyWalk.Core.Display;
using YimyWalk.Core.Simulation;

namespace YimyWalk.Core.Tests;

public sealed class SurfaceSelectionTests
{
    private static readonly WorldRect Floor = new(0, 0, 1920, 1080);

    [Fact]
    public void SelectsNearestVisibleTopUnderTheFootSpan()
    {
        var candidates = new[]
        {
            Candidate("far", new WorldRect(0, 700, 800, 900)),
            Candidate("near", new WorldRect(0, 420, 800, 600)),
            Candidate("above", new WorldRect(0, 80, 800, 300))
        };

        var selection = SurfaceSelector.Select(candidates, Floor, footLeft: 100, footRight: 160, currentY: 100, nextY: 800);

        Assert.False(selection.IsFallback);
        Assert.Equal("near", selection.SourceId);
        Assert.Equal(420, selection.Area.Top);
        Assert.Equal(800, selection.Area.Right);
    }

    [Fact]
    public void RejectsHiddenMinimizedStaleExcludedAndZeroAreaWindows()
    {
        var candidates = new[]
        {
            Candidate("hidden", new WorldRect(0, 400, 800, 500)) with { IsVisible = false },
            Candidate("minimized", new WorldRect(0, 410, 800, 500)) with { IsMinimized = true },
            Candidate("stale", new WorldRect(0, 420, 800, 500)) with { IsStale = true },
            Candidate("yimywalk", new WorldRect(0, 430, 800, 500)) with { IsExcluded = true },
            Candidate("zero", new WorldRect(0, 440, 0, 500))
        };

        var selection = SurfaceSelector.Select(candidates, Floor, footLeft: 100, footRight: 160, currentY: 100, nextY: 800);

        Assert.True(selection.IsFallback);
        Assert.Equal(Floor, selection.Area);
    }

    [Fact]
    public void FallsBackWhenTopIsOutsideHorizontalSpanOrAboveCurrentFoot()
    {
        var candidates = new[]
        {
            Candidate("side", new WorldRect(400, 300, 800, 500)),
            Candidate("above", new WorldRect(0, 50, 800, 80))
        };

        var selection = SurfaceSelector.Select(candidates, Floor, footLeft: 100, footRight: 160, currentY: 100, nextY: 250);

        Assert.True(selection.IsFallback);
        Assert.Equal(Floor, selection.Area);
    }

    private static WindowSurfaceCandidate Candidate(string id, WorldRect bounds)
        => new(id, bounds, IsVisible: true, IsMinimized: false, IsExcluded: false, IsStale: false);
}
