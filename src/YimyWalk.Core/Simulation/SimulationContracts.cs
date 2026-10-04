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

    DisplayInfo? DisplayForPoint(Vector2 point)
        => DisplayProvider?.Resolve(point);

    WorldRect WorkingAreaForPoint(Vector2 point)
        => DisplayForPoint(point)?.WorkingArea ?? WorkingArea;
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
    Jump,
    Apex,
    Fall,
    Dragged
}

public enum FacingDirection
{
    Left = -1,
    Right = 1
}
