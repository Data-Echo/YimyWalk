using System.Collections.ObjectModel;

namespace YimyWalk.Core.Characters;

using YimyWalk.Core.Resources;

public sealed record CharacterScanFailure(string DirectoryPath, string Reason);

public sealed class CharacterCatalog : IDisposable
{
    private IReadOnlyDictionary<string, CharacterDefinition> _definitions =
        new ReadOnlyDictionary<string, CharacterDefinition>(new Dictionary<string, CharacterDefinition>(StringComparer.OrdinalIgnoreCase));
    private Dictionary<string, DefinitionLease> _leases = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public IReadOnlyList<CharacterScanFailure> LastFailures { get; private set; } = Array.Empty<CharacterScanFailure>();
    public IReadOnlyDictionary<string, CharacterDefinition> Definitions => _definitions;

    public bool TryGet(string id, out CharacterDefinition definition)
        => _definitions.TryGetValue(id, out definition!);

    public IReadOnlyDictionary<string, CharacterDefinition> Scan(string root)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!Directory.Exists(root))
        {
            LastFailures = new[] { new CharacterScanFailure(root, "root directory does not exist") };
            return _definitions;
        }

        var next = new Dictionary<string, CharacterDefinition>(StringComparer.OrdinalIgnoreCase);
        var nextLeases = new Dictionary<string, DefinitionLease>(StringComparer.OrdinalIgnoreCase);
        var failures = new List<CharacterScanFailure>();
        foreach (var directory in Directory.EnumerateDirectories(root))
        {
            try
            {
                var definition = CharacterLoader.LoadDirectory(directory);
                var lease = new DefinitionLease(definition);
                if (nextLeases.TryGetValue(definition.Id, out var duplicate)) duplicate.Dispose();
                next[definition.Id] = definition;
                nextLeases[definition.Id] = lease;
            }
            catch (CharacterLoadException ex)
            {
                failures.Add(new CharacterScanFailure(directory, ex.Message));
                if (_definitions.TryGetValue(Path.GetFileName(directory), out var previous))
                {
                    next[previous.Id] = previous;
                    if (_leases.TryGetValue(previous.Id, out var previousLease)) nextLeases[previous.Id] = previousLease;
                }
            }
        }

        var old = _definitions;
        var oldLeases = _leases;
        _definitions = new ReadOnlyDictionary<string, CharacterDefinition>(next);
        _leases = nextLeases;
        LastFailures = failures;
        foreach (var pair in old)
            if (!next.TryGetValue(pair.Key, out var current) || !ReferenceEquals(pair.Value, current))
                oldLeases[pair.Key].Dispose();
        return _definitions;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var lease in _leases.Values) lease.Dispose();
        _definitions = new ReadOnlyDictionary<string, CharacterDefinition>(new Dictionary<string, CharacterDefinition>(StringComparer.OrdinalIgnoreCase));
        _leases = new Dictionary<string, DefinitionLease>(StringComparer.OrdinalIgnoreCase);
    }
}
