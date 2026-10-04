using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace YimyWalk.Core.Characters;

public static class CharacterLoader
{
    public const int MaxDimension = 2048;
    public const long MaxPixels = 16_777_216;
    public const long MaxSpriteFileBytes = 32 * 1024 * 1024;
    public const float MinScale = 0.1f;
    public const float MaxScale = 8f;
    public const int MaxRenderedFrameDimension = 1024;
    public const float MaxWalkSpeed = 2_000f;
    public const float MaxJumpSpeed = 5_000f;
    public const float MaxGravity = 20_000f;
    public const float MaxFallSpeed = 5_000f;
    public const float MaxAnimationFps = 60f;
    public const float MaxBehaviorSeconds = 86_400f;
    public const long MaxDialogueFileBytes = 256 * 1024;
    public const int MaxDialogueLines = 1_000;
    public const int MaxDialogueLineLength = 500;

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
            var header = ReadPngHeader(spritePath, fullDirectory);
            ValidateDimensions(header.Width, header.Height, fullDirectory);

            using var image = Image.Load<Rgba32>(spritePath);
            if (image.Width != header.Width || image.Height != header.Height)
                throw new CharacterLoadException(fullDirectory, "sprite dimensions changed while loading");
            ValidatePixels(image, fullDirectory);

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
                Dialogue = dialogue,
                Animation = config.Animation
            };
        }
        catch (CharacterLoadException)
        {
            throw;
        }
        catch (Exception ex) when (ex is InvalidImageContentException or UnknownImageFormatException
            or InvalidOperationException or IOException or JsonException or ArgumentException
            or NotSupportedException or EndOfStreamException or OverflowException)
        {
            throw new CharacterLoadException(fullDirectory, ex.Message, ex);
        }
    }

    private static PngHeader ReadPngHeader(string path, string directory)
    {
        var file = new FileInfo(path);
        if (file.Length <= 0 || file.Length > MaxSpriteFileBytes)
            throw new CharacterLoadException(directory, $"sprite file exceeds {MaxSpriteFileBytes} bytes");

        using var stream = File.OpenRead(path);
        Span<byte> fixedHeader = stackalloc byte[33];
        ReadExactly(stream, fixedHeader);
        ReadOnlySpan<byte> signature = stackalloc byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        if (!fixedHeader[..8].SequenceEqual(signature)
            || BinaryPrimitives.ReadUInt32BigEndian(fixedHeader[8..12]) != 13
            || !fixedHeader[12..16].SequenceEqual("IHDR"u8))
            throw new CharacterLoadException(directory, "sprite.png is not a valid PNG");

        var width = BinaryPrimitives.ReadUInt32BigEndian(fixedHeader[16..20]);
        var height = BinaryPrimitives.ReadUInt32BigEndian(fixedHeader[20..24]);
        if (width == 0 || height == 0 || width > int.MaxValue || height > int.MaxValue)
            throw new CharacterLoadException(directory, "sprite dimensions are invalid");

        var colorType = fixedHeader[25];
        var hasAlpha = colorType is 4 or 6;
        var hasPaletteTransparency = false;
        var chunkHeader = new byte[8];
        while (stream.Position + 12 <= stream.Length)
        {
            ReadExactly(stream, chunkHeader);
            var length = BinaryPrimitives.ReadUInt32BigEndian(chunkHeader[..4]);
            var type = chunkHeader.AsSpan(4, 4);
            if (length > stream.Length - stream.Position - 4)
                break;
            if (type.SequenceEqual("tRNS"u8))
                hasPaletteTransparency = true;
            stream.Seek(length + 4, SeekOrigin.Current);
            if (type.SequenceEqual("IDAT"u8))
                break;
        }

        if (colorType == 3)
            hasAlpha = hasPaletteTransparency;
        if (!hasAlpha)
            throw new CharacterLoadException(directory, "sprite.png must include an alpha channel");

        return new PngHeader((int)width, (int)height);
    }

    private static void ReadExactly(Stream stream, Span<byte> destination)
    {
        var offset = 0;
        while (offset < destination.Length)
        {
            var read = stream.Read(destination[offset..]);
            if (read == 0)
                throw new EndOfStreamException("PNG header is truncated");
            offset += read;
        }
    }

    private static void ValidateDimensions(int width, int height, string directory)
    {
        if (width <= 0 || height <= 0 || width % 3 != 0 || height % 3 != 0)
            throw new CharacterLoadException(directory, $"sprite dimensions {width}x{height} must be positive and divisible by 3");
        if (width > MaxDimension || height > MaxDimension || (long)width * height > MaxPixels)
            throw new CharacterLoadException(directory, $"sprite dimensions exceed {MaxDimension}px or {MaxPixels} pixels");
    }

    private static void ValidatePixels(Image<Rgba32> image, string directory)
    {
        for (var y = 0; y < image.Height; y++)
        for (var x = 0; x < image.Width; x++)
            if (image[x, y].A != 0)
                return;
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
        return frames.AsReadOnly();
    }

    private static CharacterConfig ReadConfig(string directory, string id, int cellWidth, int cellHeight)
    {
        var defaults = new CharacterConfig(
            id,
            1f,
            new Anchor(cellWidth * 0.5f, cellHeight * 0.9f),
            new CharacterMovement(60f, 450f, 1500f, 1500f),
            new CharacterDefaults(2f, 8f, 45f, 0.01f),
            new CharacterAnimation(3f, 8f));

        var path = Path.Combine(directory, "character.json");
        if (!File.Exists(path))
            return defaults;
        if (new FileInfo(path).Length > 64 * 1024)
            throw new CharacterLoadException(directory, "character.json exceeds 65536 bytes");

        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new CharacterLoadException(directory, "character.json root must be an object");

        if (root.TryGetProperty("schemaVersion", out var schema))
        {
            if (schema.ValueKind != JsonValueKind.Number || !schema.TryGetInt32(out var version) || version != 1)
                throw new CharacterLoadException(directory, "character.json schemaVersion must be 1");
        }

        var displayName = ReadString(root, "displayName", directory) ?? defaults.DisplayName;
        if (displayName.Length > 100 || string.IsNullOrWhiteSpace(displayName))
            throw new CharacterLoadException(directory, "displayName must be 1 to 100 characters");

        var scale = ReadFinite(root, "scale", directory) ?? defaults.Scale;
        EnsureRange(scale, MinScale, MaxScale, "scale", directory);
        if (cellWidth * scale > MaxRenderedFrameDimension || cellHeight * scale > MaxRenderedFrameDimension)
            throw new CharacterLoadException(directory, $"scale renders a frame larger than {MaxRenderedFrameDimension}px");

        var anchor = defaults.Anchor;
        if (root.TryGetProperty("anchor", out var anchorElement))
        {
            EnsureObject(anchorElement, "anchor", directory);
            var x = ReadFinite(anchorElement, "x", directory) ?? 0.5f;
            var y = ReadFinite(anchorElement, "y", directory) ?? 0.9f;
            EnsureRange(x, 0f, 1f, "anchor.x", directory);
            EnsureRange(y, 0f, 1f, "anchor.y", directory);
            anchor = new Anchor(cellWidth * x, cellHeight * y);
        }

        var movement = defaults.Movement;
        if (root.TryGetProperty("movement", out var movementElement))
        {
            EnsureObject(movementElement, "movement", directory);
            movement = new CharacterMovement(
                ReadBoundedPositive(movementElement, "walkSpeed", 0f, MaxWalkSpeed, directory) ?? movement.WalkSpeed,
                ReadBoundedPositive(movementElement, "jumpSpeed", 0f, MaxJumpSpeed, directory) ?? movement.JumpSpeed,
                ReadBoundedPositive(movementElement, "gravity", 0f, MaxGravity, directory) ?? movement.Gravity,
                ReadBoundedPositive(movementElement, "maxFallSpeed", 0f, MaxFallSpeed, directory) ?? movement.MaxFallSpeed);
        }

        var animation = defaults.Animation;
        if (root.TryGetProperty("animation", out var animationElement))
        {
            EnsureObject(animationElement, "animation", directory);
            animation = new CharacterAnimation(
                ReadBoundedPositive(animationElement, "idleFps", 0f, MaxAnimationFps, directory) ?? animation.IdleFps,
                ReadBoundedPositive(animationElement, "walkFps", 0f, MaxAnimationFps, directory) ?? animation.WalkFps);
        }

        var characterDefaults = defaults.Defaults;
        if (root.TryGetProperty("behavior", out var behaviorElement))
        {
            EnsureObject(behaviorElement, "behavior", directory);
            var idleMin = ReadBoundedPositive(behaviorElement, "idleSecondsMin", 0f, MaxBehaviorSeconds, directory) ?? characterDefaults.IdleSecondsMin;
            var idleMax = ReadBoundedPositive(behaviorElement, "idleSecondsMax", 0f, MaxBehaviorSeconds, directory) ?? characterDefaults.IdleSecondsMax;
            var cooldown = ReadBoundedPositive(behaviorElement, "speechCooldownSeconds", 0f, MaxBehaviorSeconds, directory) ?? characterDefaults.SpeechCooldownSeconds;
            var hunger = ReadFinite(behaviorElement, "hungerIncreasePerMinute", directory) ?? characterDefaults.HungerIncreasePerMinute;
            if (hunger is < 0 or > 1)
                throw new CharacterLoadException(directory, "hungerIncreasePerMinute must be between 0 and 1");
            if (idleMax < idleMin)
                throw new CharacterLoadException(directory, "idleSecondsMax must be at least idleSecondsMin");
            characterDefaults = new CharacterDefaults(idleMin, idleMax, cooldown, hunger);
        }

        return new CharacterConfig(displayName, scale, anchor, movement, characterDefaults, animation);
    }

    private static DialogueSet ReadDialogue(string directory)
    {
        var path = Path.Combine(directory, "dialogue.txt");
        if (!File.Exists(path))
            return DialogueSet.Default;
        if (new FileInfo(path).Length > MaxDialogueFileBytes)
            throw new CharacterLoadException(directory, $"dialogue.txt exceeds {MaxDialogueFileBytes} bytes");

        string[] lines;
        try
        {
            lines = File.ReadAllLines(path, new UTF8Encoding(false, true));
        }
        catch (DecoderFallbackException ex)
        {
            throw new CharacterLoadException(directory, "dialogue.txt must be valid UTF-8", ex);
        }

        var groups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var current = "idle";
        var accepted = 0;
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith(';'))
                continue;
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                var group = line[1..^1].Trim();
                if (group.Length is 0 or > 64)
                    throw new CharacterLoadException(directory, "dialogue group name is invalid");
                current = group;
                continue;
            }
            if (line.Length > MaxDialogueLineLength)
                throw new CharacterLoadException(directory, $"dialogue line exceeds {MaxDialogueLineLength} characters");
            if (++accepted > MaxDialogueLines)
                throw new CharacterLoadException(directory, $"dialogue exceeds {MaxDialogueLines} lines");
            if (!groups.TryGetValue(current, out var list))
                groups[current] = list = new List<string>();
            list.Add(line);
        }

        var immutable = groups.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<string>)Array.AsReadOnly(pair.Value.ToArray()),
            StringComparer.OrdinalIgnoreCase);
        return immutable.Count == 0 ? DialogueSet.Default : new DialogueSet(immutable);
    }

    private static string? ReadString(JsonElement element, string name, string directory)
    {
        if (!element.TryGetProperty(name, out var value))
            return null;
        if (value.ValueKind != JsonValueKind.String)
            throw new CharacterLoadException(directory, $"{name} must be a string");
        return value.GetString();
    }

    private static float? ReadBoundedPositive(JsonElement element, string name, float minimumExclusive, float maximum, string directory)
    {
        var value = ReadFinite(element, name, directory);
        if (value is null)
            return null;
        EnsureRange(value.Value, minimumExclusive, maximum, name, directory, minimumExclusive == 0f);
        return value;
    }

    private static float? ReadFinite(JsonElement element, string name, string directory)
    {
        if (!element.TryGetProperty(name, out var value))
            return null;
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetSingle(out var result) || !float.IsFinite(result))
            throw new CharacterLoadException(directory, $"{name} must be a finite number");
        return result;
    }

    private static void EnsureObject(JsonElement value, string name, string directory)
    {
        if (value.ValueKind != JsonValueKind.Object)
            throw new CharacterLoadException(directory, $"{name} must be an object");
    }

    private static void EnsureRange(float value, float minimum, float maximum, string name, string directory, bool minimumExclusive = false)
    {
        if (!float.IsFinite(value) || (minimumExclusive ? value <= minimum : value < minimum) || value > maximum)
            throw new CharacterLoadException(directory, $"{name} must be {(minimumExclusive ? "greater than" : "at least")} {minimum} and at most {maximum}");
    }

    private sealed record CharacterConfig(
        string DisplayName,
        float Scale,
        Anchor Anchor,
        CharacterMovement Movement,
        CharacterDefaults Defaults,
        CharacterAnimation Animation);

    private readonly record struct PngHeader(int Width, int Height);
}
