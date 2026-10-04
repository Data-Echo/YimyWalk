using System.Text.Json;
using System.Text.Json.Serialization;

namespace YimyWalk.Core.Settings;

public sealed record SettingsDocument
{
    public const int CurrentSchemaVersion = 1;
    public const float MinScale = 0.1f;
    public const float MaxScale = 8f;
    public const float MinActivityRate = 0.1f;
    public const float MaxActivityRate = 4f;
    public const int MaxWarningLimit = 10_000;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public string SelectedRoleId { get; init; } = "cat";
    public float Scale { get; init; } = 1f;
    public float ActivityRate { get; init; } = 1f;
    public bool DialogueEnabled { get; init; } = true;
    public bool HungerEnabled { get; init; } = true;
    public float HungerRate { get; init; } = 1f;
    public bool ExternalEventsEnabled { get; init; }
    public bool WindowLandingEnabled { get; init; }
    public string? PreferredDisplayId { get; init; }
    public int? WarningLimit { get; init; }

    public static SettingsDocument Defaults => new();

    public bool TryValidate(out string error)
    {
        if (SchemaVersion != CurrentSchemaVersion)
            return Fail($"schemaVersion must be {CurrentSchemaVersion}", out error);
        if (string.IsNullOrWhiteSpace(SelectedRoleId) || SelectedRoleId.Length > 64
            || SelectedRoleId.Contains(Path.DirectorySeparatorChar)
            || SelectedRoleId.Contains(Path.AltDirectorySeparatorChar)
            || SelectedRoleId is "." or "..")
            return Fail("selectedRoleId must be a valid role id", out error);
        if (!IsInRange(Scale, MinScale, MaxScale))
            return Fail($"scale must be between {MinScale} and {MaxScale}", out error);
        if (!IsInRange(ActivityRate, MinActivityRate, MaxActivityRate))
            return Fail($"activityRate must be between {MinActivityRate} and {MaxActivityRate}", out error);
        if (!IsInRange(HungerRate, MinActivityRate, MaxActivityRate))
            return Fail($"hungerRate must be between {MinActivityRate} and {MaxActivityRate}", out error);
        if (PreferredDisplayId is { Length: > 128 } or { Length: 0 })
            return Fail("preferredDisplayId must be empty or at most 128 characters", out error);
        if (WarningLimit is <= 0 or > MaxWarningLimit)
            return Fail($"warningLimit must be between 1 and {MaxWarningLimit}", out error);

        error = string.Empty;
        return true;
    }

    public static bool TryParse(ReadOnlySpan<byte> utf8Json, out SettingsDocument settings, out string error)
    {
        error = string.Empty;
        try
        {
            var parsed = JsonSerializer.Deserialize<SettingsDocument>(utf8Json, JsonOptions);
            if (parsed is not null && parsed.TryValidate(out error))
            {
                settings = parsed;
                return true;
            }

            settings = Defaults;
            if (string.IsNullOrWhiteSpace(error)) error = "settings document is empty";
            return false;
        }
        catch (JsonException ex)
        {
            settings = Defaults;
            error = $"settings JSON is invalid: {ex.Message}";
            return false;
        }
        catch (NotSupportedException ex)
        {
            settings = Defaults;
            error = $"settings JSON is unsupported: {ex.Message}";
            return false;
        }
    }

    public static byte[] Serialize(SettingsDocument settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!settings.TryValidate(out var error)) throw new ArgumentException(error, nameof(settings));
        return JsonSerializer.SerializeToUtf8Bytes(settings, JsonOptions);
    }

    private static bool IsInRange(float value, float minimum, float maximum)
        => float.IsFinite(value) && value >= minimum && value <= maximum;

    private static bool Fail(string message, out string error)
    {
        error = message;
        return false;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true
    };
}
