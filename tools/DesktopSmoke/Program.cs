using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

var rounds = args.Length == 0 ? 1 : int.TryParse(args[0], out var parsed) && parsed > 0 && parsed <= 20 ? parsed : throw new ArgumentException("Rounds must be between 1 and 20.");
var role = args.Length < 2 ? "cat" : args[1];
var app = Assembly.LoadFrom(Path.GetFullPath("src/YimyWalk.App/bin/x64/Release/net8.0-windows/win-x64/publish/YimyWalk.App.dll"));
var type = app.GetType("YimyWalk.App.Host.AppHost", true)!;
var host = Activator.CreateInstance(type, nonPublic: true)!;
var session = type.GetProperty("Session")!.GetValue(host)!;
var windows = type.GetProperty("Windows")!.GetValue(host)!;
var active = session.GetType().GetProperty("ActiveCount")!;
var windowCount = windows.GetType().GetProperty("Count")!;
type.GetMethod("Start")!.Invoke(host, Array.Empty<object>());
for (var round = 1; round <= rounds; round++)
foreach (var n in new[] { 1, 10, 50, 100 })
{
    type.GetMethod("CreateMany")!.Invoke(host, new object[] { role, n });
    for (var i = 0; i < 10; i++) { Application.DoEvents(); Thread.Sleep(15); }
    using (var process = System.Diagnostics.Process.GetCurrentProcess())
    {
        process.Refresh();
        Console.WriteLine($"ROUND {round} CREATE {n} ACTIVE={active.GetValue(session)} WINDOWS={windowCount.GetValue(windows)} {Metrics(process)}");
    }
    type.GetMethod("ClearPets")!.Invoke(host, Array.Empty<object>());
    for (var i = 0; i < 5; i++) { Application.DoEvents(); Thread.Sleep(15); }
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    using (var process = System.Diagnostics.Process.GetCurrentProcess())
    {
        process.Refresh();
        Console.WriteLine($"ROUND {round} CLEAR ACTIVE=0 WINDOWS=0 {Metrics(process)}");
    }
}
if (host is IDisposable disposableHost)
{
    disposableHost.Dispose();
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    using var process = System.Diagnostics.Process.GetCurrentProcess();
    process.Refresh();
    Console.WriteLine($"FINAL {Metrics(process)}");
}

static string Metrics(System.Diagnostics.Process process)
    => $"WS={process.WorkingSet64} PRIVATE={process.PrivateMemorySize64} MANAGED={GC.GetTotalMemory(false)} HANDLES={process.HandleCount} USER={Native.GetGuiResources(process.Handle, 1)} GDI={Native.GetGuiResources(process.Handle, 0)}";

static class Native
{
    [DllImport("user32.dll")]
    public static extern uint GetGuiResources(IntPtr process, uint flags);
}
