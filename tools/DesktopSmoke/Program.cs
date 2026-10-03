using System.Reflection;
using System.Windows.Forms;

var app = Assembly.LoadFrom(Path.GetFullPath("src/YimyWalk.App/bin/x64/Release/net8.0-windows/win-x64/publish/YimyWalk.App.dll"));
var type = app.GetType("YimyWalk.App.Host.AppHost", true)!;
var host = Activator.CreateInstance(type, nonPublic: true)!;
var session = type.GetProperty("Session")!.GetValue(host)!;
var windows = type.GetProperty("Windows")!.GetValue(host)!;
var active = session.GetType().GetProperty("ActiveCount")!;
var windowCount = windows.GetType().GetProperty("Count")!;
type.GetMethod("Start")!.Invoke(host, Array.Empty<object>());
foreach (var n in new[] { 1, 10, 50, 100 })
{
    type.GetMethod("CreateMany")!.Invoke(host, new object[] { "cat", n });
    for (var i = 0; i < 10; i++) { Application.DoEvents(); Thread.Sleep(15); }
    using (var process = System.Diagnostics.Process.GetCurrentProcess())
    {
        process.Refresh();
        Console.WriteLine($"CREATE {n} ACTIVE={active.GetValue(session)} WINDOWS={windowCount.GetValue(windows)} WS={process.WorkingSet64} HANDLES={process.HandleCount}");
    }
    type.GetMethod("ClearPets")!.Invoke(host, Array.Empty<object>());
    for (var i = 0; i < 5; i++) { Application.DoEvents(); Thread.Sleep(15); }
    using (var process = System.Diagnostics.Process.GetCurrentProcess())
    {
        process.Refresh();
        Console.WriteLine($"CLEAR ACTIVE={active.GetValue(session)} WINDOWS={windowCount.GetValue(windows)} WS={process.WorkingSet64} HANDLES={process.HandleCount}");
    }
}
(host as IDisposable)?.Dispose();
