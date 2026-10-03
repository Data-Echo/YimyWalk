namespace YimyWalk.Core.Characters;

public sealed class DialogueSet
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _groups;

    public DialogueSet(IReadOnlyDictionary<string, IReadOnlyList<string>> groups)
    {
        var copy = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in groups)
        {
            var lines = pair.Value
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Trim())
                .ToArray();
            if (lines.Length > 0)
                copy[pair.Key] = Array.AsReadOnly(lines);
        }
        _groups = copy;
    }

    public static DialogueSet Default { get; } = new(new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
    {
        ["idle"] = new[] { "……", "喵。" },
        ["walk"] = new[] { "走走。" },
        ["jump"] = new[] { "呀！" }
    });

    public IReadOnlyList<string> Get(string group)
        => _groups.TryGetValue(group, out var lines) && lines.Count > 0
            ? lines
            : Default._groups.TryGetValue(group, out var fallback)
                ? fallback
                : Default._groups["idle"];
}
