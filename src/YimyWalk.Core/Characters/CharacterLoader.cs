using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace YimyWalk.Core.Characters;

public static class CharacterLoader
{
    public const int MaxDimension = 2048;
    public const long MaxPixels = 16_777_216;

    public static CharacterDefinition LoadDirectory(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new CharacterLoadException(directory ?? string.Empty, "directory is empty");

        var fullDirectory = Path.GetFullPath(directory);
        if (!Directory.Exists(fullDirectory))
            throw new CharacterLoadException(fullDirectory, "directory does not exist");

        var id = new DirectoryInfo(fullDirectory).Name;
        var spritePath = Path.Combine(fullDirectory, "sprite.png");
        if (!File.Exists(spritePath))
            throw new CharacterLoadException(fullDirectory, "sprite.png is missing");

        try
        {
            using var image = Image.Load<Rgba32>(spritePath);
            ValidateImage(image, fullDirectory);

            var cellWidth = image.Width / 3;
            var cellHeight = image.Height / 3;
            var frames = ExtractFrames(image, cellWidth, cellHeight);
            var config = ReadConfig(fullDirectory, id, cellWidth, cellHeight);
            var dialogue = ReadDialogue(fullDirectory);

            return new CharacterDefinition
            {
                Id = id,
                DisplayName = config.DisplayName,
                CellWidth = cellWidth,
                CellHeight = cellHeight,
                Scale = config.Scale,
                Anchor = config.Anchor,
                Frames = frames,
                Movement = config.Movement,
                Defaults = config.Defaults,
                Dialogue = dialogue
            };
        }
        catch (CharacterLoadException)
        {
            throw;
        }
        catch (Exception ex) when (ex is InvalidImageContentException or UnknownImageFormatException or InvalidOperationException or IOException or JsonException)
        {
            throw new CharacterLoadException(fullDirectory, ex.Message, ex);
        }
    }

    private static void ValidateImage(Image<Rgba32> image, string directory)
    {
        if (image.Width <= 0 || image.Height <= 0 || image.Width % 3 != 0 || image.Height % 3 != 0)
            throw new CharacterLoadException(directory, $"sprite dimensions {image.Width}x{image.Height} must be positive and divisible by 3");
        if (image.Width > MaxDimension || image.Height > MaxDimension || (long)image.Width * image.Height > MaxPixels)
            throw new CharacterLoadException(directory, $"sprite dimensions exceed {MaxDimension}px or {MaxPixels} pixels");

        var hasOpaquePixel = false;
        for (var y = 0; y < image.Height && !hasOpaquePixel; y++)
        for (var x = 0; x < image.Width; x++)
        {
            if (image[x, y].A != 0)
            {
                hasOpaquePixel = true;
                break;
            }
        }

        if (!hasOpaquePixel)
            throw new CharacterLoadException(directory, "sprite is fully transparent");
    }

    private static IReadOnlyList<CharacterFrame> ExtractFrames(Image<Rgba32> image, int cellWidth, int cellHeight)
    {
        var frames = new List<CharacterFrame>(9);
        for (var index = 0; index < 9; index++)
        {
            var cellX = (index % 3) * cellWidth;
            var cellY = (index / 3) * cellHeight;
            var rgba = new byte[cellWidth * cellHeight * 4];
            var offset = 0;
            for (var y = 0; y < cellHeight; y++)
            for (var x = 0; x < cellWidth; x++)
            {
                var pixel = image[cellX + x, cellY + y];
                rgba[offset++] = pixel.R;
                rgba[offset++] = pixel.G;
                rgba[offset++] = pixel.B;
                rgba[offset++] = pixel.A;
            }

            var kind = index switch
            {
                <= 2 => FrameKind.Idle,
                <= 5 => FrameKind.WalkRight,
                6 => FrameKind.Jump,
                7 => FrameKind.Apex,
                _ => FrameKind.Fall
            };
            frames.Add(new CharacterFrame(index, cellWidth, cellHeight, rgba, kind));
        }
        return frames;
    }

    private static CharacterConfig ReadConfig(string directory, string id, int cellWidth, int cellHeight)
    {
        var defaults = new CharacterConfig(
            id,
            1f,
            new Anchor(cellWidth * 0.5f, cellHeight * 0.9f),
            new CharacterMovement(60f, 450f, 1500f, 1500f),
            new CharacterDefaults(2f, 8f, 45f));

        var path = Path.Combine(directory, "character.json");
        if (!File.Exists(path))
            return defaults;

        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        if (root.TryGetProperty("schemaVersion", out var schema) && schema.ValueKind != JsonValueKind.Number)
            throw new CharacterLoadException(directory, "character.json schemaVersion must be a number");
        if (root.TryGetProperty("schemaVersion", out schema) && schema.GetInt32() != 1)
            throw new CharacterLoadException(directory, "character.json schemaVersion must be 1");

        var displayName = ReadString(root, "displayName") ?? defaults.DisplayName;
        var scale = ReadFinitePositive(root, "scale") ?? defaults.Scale;
        var anchor = defaults.Anchor;
        if (root.TryGetProperty("anchor", out var anchorElement))
        {
            var x = ReadFinite(anchorElement, "x") ?? 0.5f;
            var y = ReadFinite(anchorElement, "y") ?? 0.9f;
            if (x < 0 || x > 1 || y < 0 || y > 1)
                throw new CharacterLoadException(directory, "anchor values must be between 0 and 1");
            anchor = new Anchor(cellWidth * x, cellHeight * y);
        }

        var movement = defaults.Movement;
        if (root.TryGetProperty("movement", out var movementElement))
        {
            movement = new CharacterMovement(
                ReadFinitePositive(movementElement, "walkSpeed") ?? movement.WalkSpeed,
                ReadFinitePositive(movementElement, "jumpSpeed") ?? movement.JumpSpeed,
                ReadFinitePositive(movementElement, "gravity") ?? movement.Gravity,
                ReadFinitePositive(movementElement, "maxFallSpeed") ?? movement.MaxFallSpeed);
        }

        var behavior = root.TryGetProperty("behavior", out var behaviorElement) ? behaviorElement : default;
        var characterDefaults = new CharacterDefaults(
            ReadFinitePositive(behavior, "idleSecondsMin") ?? defaults.Defaults.IdleSecondsMin,
            ReadFinitePositive(behavior, "idleSecondsMax") ?? defaults.Defaults.IdleSecondsMax,
            ReadFinitePositive(behavior, "speechCooldownSeconds") ?? defaults.Defaults.SpeechCooldownSeconds);
        if (characterDefaults.IdleSecondsMax < characterDefaults.IdleSecondsMin)
            throw new CharacterLoadException(directory, "idleSecondsMax must be at least idleSecondsMin");

        return new CharacterConfig(displayName, scale, anchor, movement, characterDefaults);
    }

    private static DialogueSet ReadDialogue(string directory)
    {
        var path = Path.Combine(directory, "dialogue.txt");
        if (!File.Exists(path))
            return DialogueSet.Default;

        var lines = File.ReadAllLines(path)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToArray();
        return lines.Length == 0
            ? DialogueSet.Default
            : new DialogueSet(new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["idle"] = lines
            });
    }

    private static string? ReadString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static float? ReadFinitePositive(JsonElement element, string name)
    {
        var value = ReadFinite(element, name);
        if (value is <= 0)
            throw new CharacterLoadException("character.json", $"{name} must be positive");
        return value;
    }

    private static float? ReadFinite(JsonElement element, string name)
    {
        if (!element.ValueKind.Equals(JsonValueKind.Object) || !element.TryGetProperty(name, out var value))
            return null;
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetSingle(out var result) || !float.IsFinite(result))
            throw new CharacterLoadException("character.json", $"{name} must be a finite number");
        return result;
    }

    private sealed record CharacterConfig(
        string DisplayName,
        float Scale,
        Anchor Anchor,
        CharacterMovement Movement,
        CharacterDefaults Defaults);
}
