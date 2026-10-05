using YimyWalk.App.Startup;

namespace YimyWalk.App;

internal static class Program
{
    internal static void InitializeDesktop() => ApplicationConfiguration.Initialize();

    [STAThread]
    private static int Main()
    {
        try
        {
            InitializeDesktop();
            using var host = new Host.AppHost();
            host.Start();
            Application.Run(host);
            return 0;
        }
        catch (StartupFailureException exception)
        {
            StartupFailureReporter.Report(exception);
            return 2;
        }
        catch (Exception exception)
        {
            StartupFailureReporter.ReportUnexpected(exception);
            return 1;
        }
    }
}
