using System.Numerics;
using YimyWalk.Core.Display;

namespace YimyWalk.Core.Simulation;

public readonly record struct WorldRect(float Left, float Top, float Right, float Bottom)
{
    public float Width => Math.Max(0, Right - Left);
    public float Height => Math.Max(0, Bottom - Top);

    public Vector2 ClampFoot(Vector2 foot)
        => new(Math.Clamp(foot.X, Left, Right), Math.Clamp(foot.Y, Top, Bottom));

    public bool Contains(Vector2 point)
        => point.X >= Left && point.X < Right && point.Y >= Top && point.Y < Bottom;

    public float DistanceSquared(Vector2 point)
    {
        var dx = point.X < Left ? Left - point.X : point.X > Right ? point.X - Right : 0;
        var dy = point.Y < Top ? Top - point.Y : point.Y > Bottom ? point.Y - Bottom : 0;
        return dx * dx + dy * dy;
    }
}

public interface IWorldSurface
{
    WorldRect WorkingArea { get; }
    bool IsValid { get; }
    IDisplayProvider? DisplayProvider => null;
    bool WindowLandingEnabled => false;
    IReadOnlyList<WindowSurfaceCandidate> JumpCandidates => Array.Empty<WindowSurfaceCandidate>();

    float DisplayScaleForPoint(Vector2 point)
        => DisplayForPoint(point)?.Scale is { } scale && float.IsFinite(scale) && scale > 0 ? scale : 1f;

    DisplayInfo? DisplayForPoint(Vector2 point)
        => DisplayProvider?.Resolve(point);

    WorldRect WorkingAreaForPoint(Vector2 point)
        => DisplayForPoint(point)?.WorkingArea ?? WorkingArea;

    WorldRect HorizontalTravelBounds
    {
        get
        {
            var displays = DisplayProvider?.Displays;
            if (displays is { Count: > 0 })
            {
                var left = float.PositiveInfinity;
                var top = float.PositiveInfinity;
                var right = float.NegativeInfinity;
                var bottom = float.NegativeInfinity;
                foreach (var display in displays)
                {
                    left = Math.Min(left, display.WorkingArea.Left);
                    top = Math.Min(top, display.WorkingArea.Top);
                    right = Math.Max(right, display.WorkingArea.Right);
                    bottom = Math.Max(bottom, display.WorkingArea.Bottom);
                }

                if (float.IsFinite(left) && float.IsFinite(top)
                    && float.IsFinite(right) && float.IsFinite(bottom) && right > left)
                    return new WorldRect(left, top, right, bottom);
            }

            return WorkingArea;
        }
    }

    WorldRect SpawnArea => WorkingArea;

    SurfaceSelection SurfaceForPet(Vector2 footPosition, float footHalfWidth, Vector2 nextFootPosition)
        => new(WorkingAreaForPoint(nextFootPosition), IsFallback: true, SourceId: null);
}

public interface IRandomSource
{
    double NextDouble();
    int Next(int minInclusive, int maxExclusive);
}

public enum PetState
{
    Idle,
    Walk,
    JumpCharge,
    Jump,
    Apex,
    Fall,
    Dragged,
    Eat,
    Climb,
    TopWalk
}

public enum FacingDirection
{
    Left = -1,
    Right = 1
}
