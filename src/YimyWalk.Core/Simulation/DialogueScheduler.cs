using YimyWalk.Core.Characters;

namespace YimyWalk.Core.Simulation;

public sealed record DialogueBubble(Guid Id, Guid PetId, string Text, double CreatedAt, double ExpiresAt);

public readonly record struct DialoguePublishResult(bool Accepted, DialogueBubble? Bubble)
{
    public static DialoguePublishResult Rejected => new(false, null);
}

/// <summary>
/// Platform-neutral dialogue admission and expiry. Rendering is owned by App;
/// this type only holds bounded text and pet identifiers.
/// </summary>
public sealed class DialogueScheduler
{
    public const int DefaultMaxVisible = 3;
    public const double DefaultLifetimeSeconds = 4;
    private readonly List<DialogueBubble> _active = new();
    private readonly Dictionary<Guid, double> _lastPublished = new();
    private readonly Dictionary<Guid, int> _nextLine = new();
    private readonly int _maxVisible;
    private readonly double _lifetimeSeconds;

    public DialogueScheduler(int maxVisible = DefaultMaxVisible, double lifetimeSeconds = DefaultLifetimeSeconds)
    {
        _maxVisible = Math.Clamp(maxVisible, 0, 100);
        _lifetimeSeconds = Math.Clamp(lifetimeSeconds, 0.1, 60);
    }

    public IReadOnlyList<DialogueBubble> Active => _active;

    public DialoguePublishResult TryPublish(PetInstance pet, string group, double nowSeconds)
    {
        ArgumentNullException.ThrowIfNull(pet);
        if (!float.IsFinite((float)nowSeconds) || nowSeconds < 0 || _active.Count >= _maxVisible)
            return DialoguePublishResult.Rejected;
        if (string.IsNullOrWhiteSpace(group)) group = "idle";
        if (_lastPublished.TryGetValue(pet.InstanceId, out var last)
            && nowSeconds - last < Math.Max(0, pet.Definition.Defaults.SpeechCooldownSeconds))
            return DialoguePublishResult.Rejected;

        var lines = pet.Definition.Dialogue.Get(group);
        if (lines.Count == 0) return DialoguePublishResult.Rejected;
        _nextLine.TryGetValue(pet.InstanceId, out var lineIndex);
        var text = lines[lineIndex % lines.Count];
        _nextLine[pet.InstanceId] = lineIndex + 1;
        var bubble = new DialogueBubble(Guid.NewGuid(), pet.InstanceId, text, nowSeconds, nowSeconds + _lifetimeSeconds);
        _active.Add(bubble);
        _lastPublished[pet.InstanceId] = nowSeconds;
        return new DialoguePublishResult(true, bubble);
    }

    public void Advance(double nowSeconds)
    {
        if (!double.IsFinite(nowSeconds)) return;
        _active.RemoveAll(bubble => bubble.ExpiresAt <= nowSeconds);
    }

    public void RemovePet(Guid petId)
    {
        _active.RemoveAll(bubble => bubble.PetId == petId);
        _lastPublished.Remove(petId);
        _nextLine.Remove(petId);
    }

    public void Clear()
    {
        _active.Clear();
        _lastPublished.Clear();
        _nextLine.Clear();
    }
}
