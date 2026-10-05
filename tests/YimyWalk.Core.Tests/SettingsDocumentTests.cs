using System.Text;
using YimyWalk.Core.Settings;

namespace YimyWalk.Core.Tests;

public sealed class SettingsDocumentTests
{
    [Fact]
    public void DefaultsAreSafeAndValid()
    {
        var settings = SettingsDocument.Defaults;

        Assert.Equal(1, settings.SchemaVersion);
        Assert.Equal("cat", settings.SelectedRoleId);
        Assert.Equal(1f, settings.Scale);
        Assert.Equal(1f, settings.ActivityRate);
        Assert.True(settings.DefaultWalkEnabled);
        Assert.True(settings.DefaultDialogueEnabled);
        Assert.True(settings.DialogueEnabled);
        Assert.True(settings.HungerEnabled);
        Assert.Equal(1f, settings.HungerRate);
        Assert.False(settings.ExternalEventsEnabled);
        Assert.False(settings.WindowLandingEnabled);
        Assert.Null(settings.PreferredDisplayId);
        Assert.Null(settings.WarningLimit);
        Assert.True(settings.TryValidate(out var error), error);
    }

    [Theory]
    [InlineData("{\"schemaVersion\":2}")]
    [InlineData("{\"selectedRoleId\":\"\"}")]
    [InlineData("{\"scale\":0.01}")]
    [InlineData("{\"scale\":NaN}")]
    [InlineData("{\"scale\":1e999}")]
    [InlineData("{\"activityRate\":5}")]
    [InlineData("{\"warningLimit\":0}")]
    [InlineData("{\"warningLimit\":10001}")]
    public void InvalidValuesReturnDefaults(string json)
    {
        var parsed = SettingsDocument.TryParse(Encoding.UTF8.GetBytes(json), out var settings, out var error);

        Assert.False(parsed);
        Assert.Equal(SettingsDocument.Defaults, settings);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void UnknownPropertiesAreRejected()
    {
        var json = Encoding.UTF8.GetBytes("{\"unknown\":true}");

        Assert.False(SettingsDocument.TryParse(json, out var settings, out _));
        Assert.Equal(SettingsDocument.Defaults, settings);
    }

    [Fact]
    public void ValidDocumentRoundTripsAsUtf8Json()
    {
        var original = SettingsDocument.Defaults with
        {
            SelectedRoleId = "slime",
            Scale = 1.5f,
            ActivityRate = 0.75f,
            DialogueEnabled = false,
            HungerEnabled = false,
            HungerRate = 0.5f,
            ExternalEventsEnabled = true,
            WindowLandingEnabled = true,
            PreferredDisplayId = "DISPLAY1",
            WarningLimit = 250
        };

        var json = SettingsDocument.Serialize(original);

        Assert.True(SettingsDocument.TryParse(json, out var roundTripped, out var error), error);
        Assert.Equal(original, roundTripped);
        Assert.Contains("\"selectedRoleId\": \"slime\"", Encoding.UTF8.GetString(json));
    }
}
