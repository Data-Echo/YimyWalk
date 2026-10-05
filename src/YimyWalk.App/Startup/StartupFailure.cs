using System.Text;
using YimyWalk.Core.Startup;

namespace YimyWalk.App.Startup;

internal sealed class StartupFailureException : Exception
{
    public StartupFailureException(StartupDiagnostic diagnostic)
        : base(diagnostic.ToString())
    {
        Diagnostic = diagnostic;
    }

    public StartupDiagnostic Diagnostic { get; }
}

internal static class StartupFailureReporter
{
    private const string LogFileName = "startup-error.log";

    public static void Report(StartupFailureException exception)
    {
        var diagnostic = exception.Diagnostic;
        var message = new StringBuilder()
            .AppendLine(diagnostic.Summary)
            .AppendLine()
            .AppendLine(diagnostic.Details)
            .AppendLine()
            .AppendLine($"处理方法：{diagnostic.Recovery}")
            .AppendLine($"错误码：{diagnostic.Code}")
            .ToString();

        WriteLog(message);
        ShowMessage(message, "YimyWalk 启动失败");
    }

    public static void ReportUnexpected(Exception exception)
    {
        var message = new StringBuilder()
            .AppendLine("YimyWalk 无法启动。")
            .AppendLine()
            .AppendLine(exception.Message)
            .AppendLine()
            .AppendLine("处理方法：检查发布包文件是否完整，并确认已安装 .NET 8 Windows Desktop Runtime (x64)。")
            .AppendLine("错误码：STARTUP_UNEXPECTED")
            .ToString();

        WriteLog(message);
        ShowMessage(message, "YimyWalk 启动失败");
    }

    private static void WriteLog(string message)
    {
        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "YimyWalk");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, LogFileName), message, Encoding.UTF8);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A diagnostic must never hide the original startup failure.
        }
    }

    private static void ShowMessage(string message, string caption)
    {
        try
        {
            MessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // Headless sessions still receive the log and non-zero exit code.
        }
    }
}
