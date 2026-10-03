using YimyWalk.Core.Simulation;

namespace YimyWalk.App.Host;

internal sealed class SystemRandomSource : IRandomSource
{
    private readonly Random _random = Random.Shared;
    public double NextDouble() => _random.NextDouble();
    public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
}
