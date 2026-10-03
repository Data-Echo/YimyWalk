using System.Numerics;

namespace YimyWalk.Core.Simulation;

public readonly record struct WorldRect(float Left, float Top, float Right, float Bottom)
{
    public float Width => Math.Max(0, Right - Left);
    public float Height => Math.Max(0, Bottom - Top);

    public Vector2 ClampFoot(Vector2 foot)
        => new(Math.Clamp(foot.X, Left, Right), Math.Clamp(foot.Y, Top, Bottom));
}

public interface IWorldSurface
{
    WorldRect WorkingArea { get; }
    bool IsValid { get; }
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
