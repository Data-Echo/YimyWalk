namespace YimyWalk.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var host = new Host.AppHost();
        host.Start();
        Application.Run(host);
    }
}
