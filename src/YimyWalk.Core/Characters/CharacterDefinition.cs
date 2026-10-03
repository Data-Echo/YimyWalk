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
    public required IReadOnlyList<CharacterFrame> Frames { get; init; }
    public required CharacterMovement Movement { get; init; }
    public required CharacterDefaults Defaults { get; init; }
    public required DialogueSet Dialogue { get; init; }

    public CharacterFrame SelectFrame(FrameKind kind)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        if (kind == FrameKind.WalkLeft)
        {
            var right = Frames.First(frame => frame.Kind == FrameKind.WalkRight);
            return right with { Kind = FrameKind.WalkLeft, Mirrored = true };
        }

        return Frames.First(frame => frame.Kind == kind);
    }

    /// <summary>
    /// Character frames are managed-memory snapshots. The method is idempotent
    /// so a catalog and its renderer leases can safely converge on disposal.
    /// </summary>
    public void Dispose()
    {
        Interlocked.Exchange(ref _disposed, 1);
    }
}
