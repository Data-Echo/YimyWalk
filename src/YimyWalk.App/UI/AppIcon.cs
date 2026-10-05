using System.Drawing;

namespace YimyWalk.App.UI;

internal static class AppIcon
{
    private const string ResourceName = "YimyWalk.App.Resources.yimywalk.ico";

    public static Icon Load()
    {
        var assembly = typeof(AppIcon).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Application icon resource is missing: {ResourceName}");
        using var source = new Icon(stream);
        return new Icon(source, source.Size);
    }
}
