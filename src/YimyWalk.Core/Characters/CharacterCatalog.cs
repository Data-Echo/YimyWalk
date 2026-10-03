namespace YimyWalk.Core.Characters;

public sealed record CharacterScanFailure(string DirectoryPath, string Reason);

public sealed class CharacterCatalog
{
    private IReadOnlyDictionary<string, CharacterDefinition> _definitions =
        new Dictionary<string, CharacterDefinition>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<CharacterScanFailure> LastFailures { get; private set; } = Array.Empty<CharacterScanFailure>();
    public IReadOnlyDictionary<string, CharacterDefinition> Definitions => _definitions;

    public bool TryGet(string id, out CharacterDefinition definition)
        => _definitions.TryGetValue(id, out definition!);

    public IReadOnlyDictionary<string, CharacterDefinition> Scan(string root)
    {
        if (!Directory.Exists(root))
        {
            LastFailures = new[] { new CharacterScanFailure(root, "root directory does not exist") };
            return _definitions;
        }

        var next = new Dictionary<string, CharacterDefinition>(StringComparer.OrdinalIgnoreCase);
        var failures = new List<CharacterScanFailure>();
        foreach (var directory in Directory.EnumerateDirectories(root))
        {
            try
            {
                var definition = CharacterLoader.LoadDirectory(directory);
                next[definition.Id] = definition;
            }
            catch (CharacterLoadException ex)
            {
                failures.Add(new CharacterScanFailure(directory, ex.Message));
                if (_definitions.TryGetValue(Path.GetFileName(directory), out var previous))
                    next[previous.Id] = previous;
            }
        }

        var old = _definitions;
        _definitions = next;
        LastFailures = failures;
        foreach (var pair in old)
            if (!next.ContainsKey(pair.Key) && !ReferenceEquals(pair.Value, next.Values.FirstOrDefault(value => ReferenceEquals(value, pair.Value))))
                pair.Value.Dispose();
        return _definitions;
    }
}
