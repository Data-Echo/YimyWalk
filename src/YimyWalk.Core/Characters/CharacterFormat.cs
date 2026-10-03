namespace YimyWalk.Core.Characters;

public enum FrameKind
{
    Idle,
    WalkRight,
    WalkLeft,
    Jump,
    Apex,
    Fall
}

public readonly record struct Anchor(float X, float Y);

public sealed record CharacterFrame(
    int Index,
    int Width,
    int Height,
    ReadOnlyMemory<byte> RgbaPixels,
    FrameKind Kind,
    bool Mirrored = false);

public sealed record CharacterMovement(
    float WalkSpeed,
    float JumpSpeed,
    float Gravity,
    float MaxFallSpeed);

public sealed record CharacterDefaults(
    float IdleSecondsMin,
    float IdleSecondsMax,
    float SpeechCooldownSeconds);

public sealed record CharacterAnimation(float IdleFps, float WalkFps);

public sealed class CharacterLoadException : Exception
{
    public CharacterLoadException(string directory, string reason, Exception? innerException = null)
        : base($"Could not load character '{directory}': {reason}", innerException)
    {
        DirectoryPath = directory;
    }

    public string DirectoryPath { get; }
}
