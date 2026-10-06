using YimyWalk.Core.Simulation;
using YimyWalk.Core.Characters;
using System.Numerics;

namespace YimyWalk.App.UI;

internal sealed record PetListItem(Guid InstanceId, CharacterDefinition Definition, PetState State,
    float Hunger, Vector2 Position, PetBehaviorOverrides BehaviorOverrides)
{
    public string RoleName => Definition.DisplayName;
    public static PetListItem Capture(PetInstance pet)
        => new(pet.InstanceId, pet.Definition, pet.State, pet.Hunger, pet.FootPosition, pet.BehaviorOverrides);
}

/// <summary>
/// Keeps list rows keyed by the immutable instance id. The projection does not
/// expose row indexes, so a refresh cannot accidentally retarget an editor.
/// </summary>
internal sealed class PetListProjection
{
    private readonly Dictionary<Guid, PetListItem> _items = new();

    public IReadOnlyCollection<PetListItem> Items => _items.Values;
    public Guid? SelectedInstanceId { get; private set; }
    public event Action? Changed;

    public void SetSelected(Guid? instanceId)
    {
        if (instanceId is not null && !_items.ContainsKey(instanceId.Value))
            instanceId = null;
        if (SelectedInstanceId == instanceId) return;
        SelectedInstanceId = instanceId;
        Changed?.Invoke();
    }

    public void Add(PetInstance instance)
    {
        _items[instance.InstanceId] = PetListItem.Capture(instance);
        EnsureSelection();
        Changed?.Invoke();
    }

    public void Remove(Guid instanceId)
    {
        var orderedBefore = _items.Keys.OrderBy(id => id).ToArray();
        var removedIndex = Array.IndexOf(orderedBefore, instanceId);
        if (!_items.Remove(instanceId)) return;
        if (SelectedInstanceId == instanceId)
        {
            var ordered = _items.Keys.OrderBy(id => id).ToArray();
            var nextIndex = Math.Min(Math.Max(removedIndex, 0), ordered.Length - 1);
            SelectedInstanceId = ordered.Length == 0 ? null : ordered[nextIndex];
            if (SelectedInstanceId == Guid.Empty) SelectedInstanceId = null;
        }
        EnsureSelection();
        Changed?.Invoke();
    }

    public void Refresh(IEnumerable<PetListItem> instances)
    {
        var previousSelection = SelectedInstanceId;
        var previousOrder = _items.Keys.OrderBy(id => id).ToArray();
        var next = instances.ToDictionary(instance => instance.InstanceId);
        foreach (var stale in _items.Keys.Where(id => !next.ContainsKey(id)).ToArray())
            _items.Remove(stale);
        foreach (var pair in next)
            _items[pair.Key] = pair.Value;
        if (previousSelection is not null && !next.ContainsKey(previousSelection.Value))
        {
            var removedIndex = Array.IndexOf(previousOrder, previousSelection.Value);
            var ordered = next.Keys.OrderBy(id => id).ToArray();
            SelectedInstanceId = ordered.Length == 0 ? null : ordered[Math.Min(Math.Max(removedIndex, 0), ordered.Length - 1)];
        }
        EnsureSelection();
        Changed?.Invoke();
    }

    private void EnsureSelection()
    {
        if (SelectedInstanceId is not null && _items.ContainsKey(SelectedInstanceId.Value)) return;
        SelectedInstanceId = _items.Keys.OrderBy(id => id).FirstOrDefault();
        if (SelectedInstanceId == Guid.Empty) SelectedInstanceId = null;
    }

}
