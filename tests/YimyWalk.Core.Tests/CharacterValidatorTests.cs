using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using YimyWalk.CharacterTools;

namespace YimyWalk.Core.Tests;

public sealed class CharacterValidatorTests
{
    [Fact]
    public void SpriteOnlyRoleReportsSharedLoaderDiagnostics()
    {
        var directory = TestSpriteFactory.WriteSprite(96, 96, fullyTransparent: false, "sprite-only");
        try
        {
            var report = CharacterValidator.Validate(directory);

            Assert.True(report.IsValid);
            Assert.Equal("sprite-only", report.CharacterId);
            Assert.Equal((96, 96), (report.Width, report.Height));
            Assert.Equal((32, 32), (report.CellWidth, report.CellHeight));
            Assert.Equal(9, report.FrameCount);
            Assert.False(report.HasConfig);
            Assert.False(report.HasDialogue);
            Assert.Contains(report.Diagnostics, line => line.Contains("alpha", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(report.Diagnostics, line => line.Contains("frames", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            TestSpriteFactory.DeleteFixture(directory);
        }
    }

    [Fact]
    public void InvalidConfigIsReportedWithoutThrowing()
    {
        var directory = TestSpriteFactory.WriteSprite(96, 96, fullyTransparent: false, "bad-config");
        try
        {
            File.WriteAllText(Path.Combine(directory, "character.json"), "{\"scale\":99}");

            var report = CharacterValidator.Validate(directory);

            Assert.False(report.IsValid);
            Assert.Contains(report.Diagnostics, line => line.Contains("scale", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            TestSpriteFactory.DeleteFixture(directory);
        }
    }

    [Fact]
    public void InvalidUtf8DialogueIsReported()
    {
        var directory = TestSpriteFactory.WriteSprite(96, 96, fullyTransparent: false, "bad-dialogue");
        try
        {
            File.WriteAllBytes(Path.Combine(directory, "dialogue.txt"), new byte[] { 0xC3, 0x28 });

            var report = CharacterValidator.Validate(directory);

            Assert.False(report.IsValid);
            Assert.Contains(report.Diagnostics, line => line.Contains("UTF-8", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            TestSpriteFactory.DeleteFixture(directory);
        }
    }

    [Fact]
    public void InvalidPathIsReportedWithoutThrowing()
    {
        var report = CharacterValidator.Validate("bad\0path");

        Assert.False(report.IsValid);
        Assert.Contains(report.Diagnostics, line => line.StartsWith("ERROR", StringComparison.Ordinal));
    }

    [Fact]
    public void OutputCannotOverwriteAnExistingRoleFile()
    {
        var directory = TestSpriteFactory.WriteSprite(96, 96, fullyTransparent: false, "protected-output");
        try
        {
            Assert.Throws<IOException>(() => CharacterPreviewWriter.EnsureOutputDoesNotReplaceRoleFile(
                directory,
                Path.Combine(directory, "sprite.png")));
        }
        finally
        {
            TestSpriteFactory.DeleteFixture(directory);
        }
    }

    [Fact]
    public void InvalidRoleDoesNotCreateOrReplacePreview()
    {
        var directory = TestSpriteFactory.WriteCorruptSprite();
        var outputDirectory = Path.Combine(Path.GetTempPath(), "YimyWalkTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);
        var preview = Path.Combine(outputDirectory, "preview.png");
        var original = new byte[] { 7, 8, 9 };
        File.WriteAllBytes(preview, original);
        try
        {
            var exitCode = CharacterValidatorCli.Run(new[] { directory, "--preview", preview });

            Assert.Equal(1, exitCode);
            Assert.Equal(original, File.ReadAllBytes(preview));
        }
        finally
        {
            TestSpriteFactory.DeleteFixture(directory);
            Directory.Delete(outputDirectory, recursive: true);
        }
    }

    [Fact]
    public void ReferenceWithoutRoleIsUsageErrorWithoutCreatingOutput()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "YimyWalkTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);
        var reference = Path.Combine(outputDirectory, "reference.png");
        try
        {
            var exitCode = CharacterValidatorCli.Run(new[] { "--reference", reference });

            Assert.Equal(2, exitCode);
            Assert.False(File.Exists(reference));
        }
        finally
        {
            Directory.Delete(outputDirectory, recursive: true);
        }
    }

    [Fact]
    public void PreviewAndReferenceArePngsAndDoNotModifyInput()
    {
        var directory = TestSpriteFactory.WriteSprite(96, 96, fullyTransparent: false, "preview");
        var preview = Path.Combine(directory, "preview.png");
        var reference = Path.Combine(directory, "reference.png");
        var inputHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(directory, "sprite.png"))));
        try
        {
            var report = CharacterValidator.Validate(directory);
            Assert.True(report.IsValid);

            CharacterPreviewWriter.WritePreview(report.Definition!, preview);
            CharacterPreviewWriter.WriteReference(reference);

            using var previewImage = Image.Load<Rgba32>(preview);
            using var referenceImage = Image.Load<Rgba32>(reference);
            Assert.Equal(96, previewImage.Width);
            Assert.Equal(96, previewImage.Height);
            Assert.Equal(192, referenceImage.Width);
            Assert.Equal(192, referenceImage.Height);
            Assert.Equal(0, referenceImage[10, 10].A);
            Assert.NotEqual(0, referenceImage[64, 64].A);
            Assert.Equal(inputHash, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(directory, "sprite.png")))));
        }
        finally
        {
            reportCleanup(directory);
        }

        static void reportCleanup(string path) => TestSpriteFactory.DeleteFixture(path);
    }
}
