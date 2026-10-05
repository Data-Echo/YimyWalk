namespace YimyWalk.App;

internal static class Program
{
    internal static void InitializeDesktop() => ApplicationConfiguration.Initialize();

    [STAThread]
    private static void Main()
    {
        InitializeDesktop();
        using var host = new Host.AppHost();
        host.Start();
        Application.Run(host);
    }
}
