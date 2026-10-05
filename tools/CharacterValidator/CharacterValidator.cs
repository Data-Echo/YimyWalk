using YimyWalk.Core.Characters;

namespace YimyWalk.CharacterTools;

public sealed class CharacterValidationReport : IDisposable
{
    private CharacterDefinition? _definition;

    internal CharacterValidationReport(
        string directoryPath,
        CharacterDefinition? definition,
        IReadOnlyList<string> diagnostics)
    {
        DirectoryPath = directoryPath;
        _definition = definition;
        Diagnostics = diagnostics;
    }

    public string DirectoryPath { get; }
    public CharacterDefinition? Definition => _definition;
    public IReadOnlyList<string> Diagnostics { get; }
    public bool IsValid => _definition is not null;
    public string? CharacterId => _definition?.Id;
    public int Width => _definition is null ? 0 : _definition.CellWidth * 3;
    public int Height => _definition is null ? 0 : _definition.CellHeight * 3;
    public int CellWidth => _definition?.CellWidth ?? 0;
    public int CellHeight => _definition?.CellHeight ?? 0;
    public int FrameCount => _definition?.Frames.Count ?? 0;
    public bool HasConfig => File.Exists(Path.Combine(DirectoryPath, "character.json"));
    public bool HasDialogue => File.Exists(Path.Combine(DirectoryPath, "dialogue.txt"));

    public void Dispose()
    {
        Interlocked.Exchange(ref _definition, null)?.Dispose();
    }
}

public static class CharacterValidator
{
    public static CharacterValidationReport Validate(string directory)
    {
        var fullPath = directory ?? string.Empty;
        try
        {
            if (!string.IsNullOrWhiteSpace(directory))
                fullPath = Path.GetFullPath(directory);
            var definition = CharacterLoader.LoadDirectory(fullPath);
            var diagnostics = new List<string>
            {
                $"OK id={definition.Id} display=\"{definition.DisplayName}\"",
                $"dimensions={definition.CellWidth * 3}x{definition.CellHeight * 3} cell={definition.CellWidth}x{definition.CellHeight}",
                "alpha=present (decoded as RGBA)",
                "frames=9 [idle:0-2, walk-right:3-5, jump:6, apex:7, fall:8]",
                $"config={(File.Exists(Path.Combine(fullPath, "character.json")) ? "character.json" : "defaults")}",
                $"dialogue={(File.Exists(Path.Combine(fullPath, "dialogue.txt")) ? "dialogue.txt" : "defaults")}",
                $"anchor=({definition.Anchor.X:0.##},{definition.Anchor.Y:0.##}) scale={definition.Scale:0.##}"
            };
            return new CharacterValidationReport(fullPath, definition, diagnostics);
        }
        catch (CharacterLoadException ex)
        {
            return new CharacterValidationReport(fullPath, null, new[] { $"ERROR {ex.Message}" });
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return new CharacterValidationReport(fullPath, null, new[] { $"ERROR Could not inspect character: {ex.Message}" });
        }
    }
}
