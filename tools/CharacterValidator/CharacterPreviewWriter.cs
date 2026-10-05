using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using YimyWalk.Core.Characters;

namespace YimyWalk.CharacterTools;

public static class CharacterPreviewWriter
{
    public static void EnsureOutputDoesNotReplaceRoleFile(string roleDirectory, string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        var roleRoot = Path.GetFullPath(roleDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var fullOutput = Path.GetFullPath(outputPath);
        if (fullOutput.StartsWith(roleRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(fullOutput))
            throw new IOException($"output path must not replace an existing file in the role directory: {fullOutput}");
    }

    public static void WritePreview(CharacterDefinition definition, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        var fullPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        using var image = new Image<Rgba32>(definition.CellWidth * 3, definition.CellHeight * 3, new Rgba32(0, 0, 0, 0));
        foreach (var frame in definition.Frames)
        {
            var cellX = frame.Index % 3 * definition.CellWidth;
            var cellY = frame.Index / 3 * definition.CellHeight;
            var pixels = frame.RgbaPixels.Span;
            for (var y = 0; y < definition.CellHeight; y++)
            for (var x = 0; x < definition.CellWidth; x++)
            {
                var offset = (y * definition.CellWidth + x) * 4;
                image[cellX + x, cellY + y] = new Rgba32(pixels[offset], pixels[offset + 1], pixels[offset + 2], pixels[offset + 3]);
            }
        }
        image.SaveAsPng(fullPath);
    }

    public static void WriteReference(string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        var fullPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        const int cell = 64;
        using var image = new Image<Rgba32>(cell * 3, cell * 3, new Rgba32(0, 0, 0, 0));
        var grid = new Rgba32(110, 110, 110, 255);
        var anchor = new Rgba32(210, 70, 70, 255);
        for (var i = 0; i < 3 * cell; i++)
        {
            if (i % cell != 0 && i != 3 * cell - 1) continue;
            for (var x = 0; x < 3 * cell; x++)
            {
                image[x, i] = grid;
                image[i, x] = grid;
            }
        }
        for (var row = 0; row < 3; row++)
        for (var column = 0; column < 3; column++)
        {
            var x = column * cell + cell / 2;
            var y = row * cell + (int)(cell * 0.9f);
            for (var delta = -5; delta <= 5; delta++)
            {
                if (x + delta >= 0 && x + delta < image.Width) image[x + delta, y] = anchor;
                if (y + delta >= 0 && y + delta < image.Height) image[x, y + delta] = anchor;
            }
        }
        image.SaveAsPng(fullPath);
    }
}
