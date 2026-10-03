namespace YimyWalk.Core.Characters;

public sealed class DialogueSet
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _groups;

    public DialogueSet(IReadOnlyDictionary<string, IReadOnlyList<string>> groups)
    {
        _groups = new Dictionary<string, IReadOnlyList<string>>(groups, StringComparer.OrdinalIgnoreCase);
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
            : _groups["idle"];
}
