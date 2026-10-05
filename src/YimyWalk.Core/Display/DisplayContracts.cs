using System.Collections.ObjectModel;
using System.Numerics;
using YimyWalk.Core.Simulation;

namespace YimyWalk.Core.Display;

public readonly record struct DisplayId(string Value)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(Value);

    public override string ToString() => Value ?? string.Empty;
}

public readonly record struct DisplayInfo(
    DisplayId Id,
    WorldRect Bounds,
    WorldRect WorkingArea,
    float Scale,
    bool IsPrimary)
{
    public bool IsValid
        => Id.IsValid
            && Bounds.Width > 0
            && Bounds.Height > 0
            && WorkingArea.Width > 0
            && WorkingArea.Height > 0
            && float.IsFinite(Scale)
            && Scale > 0;

    public bool Contains(Vector2 point) => Bounds.Contains(point);
}

public interface IDisplayProvider
{
    IReadOnlyList<DisplayInfo> Displays { get; }
    DisplayInfo? Primary { get; }
    DisplayInfo? FindForPoint(Vector2 point);
    DisplayInfo? FindById(DisplayId id);
    DisplayInfo? Resolve(Vector2 point, DisplayId? preferred = null);
}

/// <summary>
/// Immutable display topology captured by the platform adapter.
/// </summary>
public sealed class DisplaySnapshot : IDisplayProvider
{
    private readonly IReadOnlyList<DisplayInfo> _displays;

    public DisplaySnapshot(IEnumerable<DisplayInfo> displays)
    {
        ArgumentNullException.ThrowIfNull(displays);
        var unique = new List<DisplayInfo>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var display in displays)
        {
            if (display.IsValid && seen.Add(display.Id.Value)) unique.Add(display);
        }

        _displays = new ReadOnlyCollection<DisplayInfo>(unique);
        if (unique.Count > 0)
        {
            var left = unique.Min(display => display.WorkingArea.Left);
            var top = unique.Min(display => display.WorkingArea.Top);
            var right = unique.Max(display => display.WorkingArea.Right);
            var bottom = unique.Max(display => display.WorkingArea.Bottom);
            HorizontalTravelBounds = new WorldRect(left, top, right, bottom);
        }
    }

    public IReadOnlyList<DisplayInfo> Displays => _displays;

    /// <summary>
    /// Bounding horizontal span of all valid display working areas. The span
    /// intentionally includes coordinate gaps so walking can cross displays.
    /// </summary>
    public WorldRect? HorizontalTravelBounds { get; }

    public DisplayInfo? Primary
    {
        get
        {
            foreach (var display in _displays)
                if (display.IsPrimary) return display;
            return _displays.Count == 0 ? null : _displays[0];
        }
    }

    public DisplayInfo? FindForPoint(Vector2 point)
    {
        foreach (var display in _displays)
            if (display.Contains(point)) return display;
        return null;
    }

    public DisplayInfo? FindById(DisplayId id)
    {
        foreach (var display in _displays)
            if (string.Equals(display.Id.Value, id.Value, StringComparison.OrdinalIgnoreCase)) return display;
        return null;
    }

    public DisplayInfo? Resolve(Vector2 point, DisplayId? preferred = null)
    {
        if (preferred is { } preferredId)
            return FindById(preferredId) ?? Primary;

        var containing = FindForPoint(point);
        if (containing.HasValue) return containing;

        DisplayInfo? nearest = null;
        var nearestDistance = float.PositiveInfinity;
        foreach (var display in _displays)
        {
            var distance = display.Bounds.DistanceSquared(point);
            if (distance < nearestDistance)
            {
                nearest = display;
                nearestDistance = distance;
            }
        }

        return nearest ?? Primary;
    }
}
