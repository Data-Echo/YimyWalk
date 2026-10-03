using YimyWalk.Core.Characters;

namespace YimyWalk.Core.Tests;

public sealed class CharacterCatalogTests
{
    [Fact]
    public void FailedRefreshKeepsLastKnownGoodDefinition()
    {
        var root = Path.Combine(Path.GetTempPath(), "YimyWalkTests", Guid.NewGuid().ToString("N"));
        var role = Path.Combine(root, "cat");
        Directory.CreateDirectory(role);
        var source = TestSpriteFactory.WriteSprite(96, 96, fullyTransparent: false);
        File.Copy(Path.Combine(source, "sprite.png"), Path.Combine(role, "sprite.png"));

        var catalog = new CharacterCatalog();
        var first = catalog.Scan(root)["cat"];
        File.WriteAllBytes(Path.Combine(role, "sprite.png"), new byte[] { 1, 2, 3 });

        catalog.Scan(root);

        Assert.True(catalog.TryGet("cat", out var retained));
        Assert.Same(first, retained);
        Assert.Single(catalog.LastFailures);
        Assert.False(first.IsDisposed);
    }
}
