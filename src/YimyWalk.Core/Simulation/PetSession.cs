using System.Numerics;
using YimyWalk.Core.Characters;
using YimyWalk.Core.Resources;

namespace YimyWalk.Core.Simulation;

public sealed class PetSession : IDisposable
{
    private readonly Dictionary<Guid, (PetInstance Pet, DefinitionLease.LeaseHandle Lease)> _instances = new();
    private readonly Dictionary<string, DefinitionLease> _definitions = new(StringComparer.OrdinalIgnoreCase);
    private readonly PetEngine _engine;
    private bool _disposed;

    public PetSession(IWorldSurface surface, IRandomSource random, int? hardLimit = null)
    {
        _engine = new PetEngine(surface, random);
        HardLimit = hardLimit;
    }

    public int? HardLimit { get; }
    public bool IsPaused { get; private set; }
    public int ActiveCount => _instances.Count;
    public IWorldSurface Surface => _engine.Surface;
    public IReadOnlyCollection<PetInstance> Instances => _instances.Values.Select(value => value.Pet).ToArray();

    public event Action<PetInstance>? InstanceAdded;
    public event Action<PetInstance>? InstanceRemoved;

    public void RegisterDefinition(string id, CharacterDefinition definition)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(definition);
        var lease = new DefinitionLease(definition);
        if (_definitions.TryGetValue(id, out var previous))
            previous.Dispose();
        _definitions[id] = lease;
    }

    public bool ReplaceDefinition(string id, CharacterDefinition definition)
    {
        if (!_definitions.ContainsKey(id))
        {
            RegisterDefinition(id, definition);
            return true;
        }
        RegisterDefinition(id, definition);
        return true;
    }

    public bool RemoveDefinition(string id)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_definitions.Remove(id, out var lease)) return false;
        lease.Dispose();
        return true;
    }

    public PetInstance Create(string id, Vector2? footPosition = null)
    {
        if (!_definitions.TryGetValue(id, out var lease))
            throw new KeyNotFoundException($"Character '{id}' is not registered.");
        return Create(lease, footPosition ?? DefaultSpawnPosition());
    }

    public IReadOnlyList<PetInstance> CreateMany(CharacterDefinition definition, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (count <= 0) return Array.Empty<PetInstance>();
        var lease = GetOrAddLease(definition);
        var available = HardLimit is null ? count : Math.Max(0, HardLimit.Value - ActiveCount);
        var result = new List<PetInstance>(Math.Min(count, available));
        for (var i = 0; i < available && i < count; i++)
            result.Add(Create(lease, DefaultSpawnPosition(i)));
        return result;
    }

    public bool Remove(Guid instanceId)
    {
        if (!_instances.Remove(instanceId, out var entry)) return false;
        entry.Lease.Dispose();
        InstanceRemoved?.Invoke(entry.Pet);
        return true;
    }

    public void Clear()
    {
        foreach (var id in _instances.Keys.ToArray())
            Remove(id);
    }

    public void Tick(double deltaSeconds)
    {
        if (!IsPaused) _engine.Tick(_instances.Values.Select(entry => entry.Pet), deltaSeconds);
    }

    public void Pause() => IsPaused = true;
    public void Resume() => IsPaused = false;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Clear();
        foreach (var lease in _definitions.Values)
            lease.Dispose();
        _definitions.Clear();
        InstanceAdded = null;
        InstanceRemoved = null;
    }

    private DefinitionLease GetOrAddLease(CharacterDefinition definition)
    {
        var existing = _definitions.Values.FirstOrDefault(lease => ReferenceEquals(lease.Definition, definition));
        if (existing is not null) return existing;
        var lease = new DefinitionLease(definition);
        _definitions[definition.Id] = lease;
        return lease;
    }

    private PetInstance Create(DefinitionLease lease, Vector2 position)
    {
        if (HardLimit is not null && ActiveCount >= HardLimit.Value)
            throw new InvalidOperationException("Configured pet session limit reached.");
        var pet = new PetInstance(lease.Definition, position);
        _instances.Add(pet.InstanceId, (pet, lease.AcquireInstance()));
        InstanceAdded?.Invoke(pet);
        return pet;
    }

    private Vector2 DefaultSpawnPosition(int offset = 0)
    {
        var area = Surface.SpawnArea;
        var x = area.Left + Math.Min(area.Width, 80 + (offset % 10) * 50);
        return new Vector2(x, area.Top - 32);
    }
}
