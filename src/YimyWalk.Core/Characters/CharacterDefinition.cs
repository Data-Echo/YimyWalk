namespace YimyWalk.Core.Characters;

/// <summary>
/// Immutable, platform-neutral data shared by all instances of one role.
/// It deliberately contains no window, bitmap, or other desktop resource.
/// </summary>
public sealed class CharacterDefinition : IDisposable
{
    private int _disposed;

    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required int CellWidth { get; init; }
    public required int CellHeight { get; init; }
    public required float Scale { get; init; }
    public required Anchor Anchor { get; init; }
    private IReadOnlyList<CharacterFrame> _frames = Array.Empty<CharacterFrame>();
    public required IReadOnlyList<CharacterFrame> Frames { get => _frames; init => _frames = Array.AsReadOnly(value.ToArray()); }
    public required CharacterMovement Movement { get; init; }
    public required CharacterDefaults Defaults { get; init; }
    public required DialogueSet Dialogue { get; init; }
    public CharacterAnimation Animation { get; init; } = new(3, 8);
    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    public CharacterFrame SelectFrame(FrameKind kind, float animationSeconds = 0)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        var walking = kind is FrameKind.WalkRight or FrameKind.WalkLeft;
        var offset = kind == FrameKind.Idle || walking
            ? (int)(Math.Max(0, animationSeconds) * (walking ? Animation.WalkFps : Animation.IdleFps) % 3) : 0;
        var index = kind switch { FrameKind.Idle => offset, FrameKind.WalkRight or FrameKind.WalkLeft => 3 + offset, FrameKind.Jump => 6, FrameKind.Apex => 7, _ => 8 };
        return kind == FrameKind.WalkLeft ? Frames[index] with { Kind = kind, Mirrored = true } : Frames[index];
    }

    /// <summary>
    /// Character frames are managed-memory snapshots. The method is idempotent
    /// so a catalog and its renderer leases can safely converge on disposal.
    /// </summary>
    public void Dispose()
    {
        Interlocked.Exchange(ref _disposed, 1);
        _frames = Array.Empty<CharacterFrame>();
    }
}
