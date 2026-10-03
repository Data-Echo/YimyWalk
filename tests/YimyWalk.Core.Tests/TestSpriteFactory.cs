using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using YimyWalk.Core.Characters;

namespace YimyWalk.Core.Tests;

internal static class TestSpriteFactory
{
    public static CharacterDefinition LoadSpriteOnly(string id)
    {
        var directory = WriteSprite(96, 96, fullyTransparent: false, id);
        return CharacterLoader.LoadDirectory(directory);
    }

    public static string WriteSprite(int width, int height, bool fullyTransparent, string? id = null)
    {
        var directory = Path.Combine(Path.GetTempPath(), "YimyWalkTests", Guid.NewGuid().ToString("N"), id ?? "role");
        Directory.CreateDirectory(directory);
        using var image = new Image<Rgba32>(width, height, new Rgba32(0, 0, 0, 0));
        if (!fullyTransparent)
        {
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                if (((x / Math.Max(1, width / 3)) + (y / Math.Max(1, height / 3))) % 2 == 0)
                    image[x, y] = new Rgba32(40, 180, 80, 255);
        }
        image.SaveAsPng(Path.Combine(directory, "sprite.png"));
        return directory;
    }

    public static string WriteCorruptSprite()
    {
        var directory = Path.Combine(Path.GetTempPath(), "YimyWalkTests", Guid.NewGuid().ToString("N"), "corrupt");
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, "sprite.png"), new byte[] { 1, 2, 3, 4, 5 });
        return directory;
    }
}
