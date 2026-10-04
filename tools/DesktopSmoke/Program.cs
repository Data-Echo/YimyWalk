using System.Reflection;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Windows.Forms;

var rounds = args.Length == 0 ? 1 : int.TryParse(args[0], out var parsed) && parsed > 0 && parsed <= 20 ? parsed : throw new ArgumentException("Rounds must be between 1 and 20.");
var role = args.Length < 2 ? "cat" : args[1];
var landingRequested = args.Length >= 3 && args[2].Equals("landing", StringComparison.OrdinalIgnoreCase);
var app = Assembly.LoadFrom(Path.GetFullPath("src/YimyWalk.App/bin/x64/Release/net8.0-windows/win-x64/publish/YimyWalk.App.dll"));
var settingsRoot = Path.Combine(Path.GetTempPath(), "YimyWalkSettingsSmoke", Guid.NewGuid().ToString("N"));
var settingsType = app.GetType("YimyWalk.App.Settings.SettingsStore", true)!;
var settingsStore = Activator.CreateInstance(settingsType, new object[] { settingsRoot })!;
var settingsLoad = settingsType.GetMethod("Load")!.Invoke(settingsStore, Array.Empty<object>())!;
var settingsResultType = settingsLoad.GetType();
if (!(bool)settingsResultType.GetProperty("UsedDefaults")!.GetValue(settingsLoad)!)
    throw new InvalidOperationException("missing settings did not use defaults");
var settingsDocument = Assembly.Load("YimyWalk.Core").GetType("YimyWalk.Core.Settings.SettingsDocument", true)!;
var defaults = settingsDocument.GetProperty("Defaults")!.GetValue(null)!;
settingsType.GetMethod("Save")!.Invoke(settingsStore, new[] { defaults });
settingsType.GetMethod("Save")!.Invoke(settingsStore, new[] { defaults });
var savedLoad = settingsType.GetMethod("Load")!.Invoke(settingsStore, Array.Empty<object>())!;
if ((bool)savedLoad.GetType().GetProperty("UsedDefaults")!.GetValue(savedLoad)!)
    throw new InvalidOperationException("saved settings did not load");
var settingsPath = (string)settingsType.GetProperty("SettingsPath")!.GetValue(settingsStore)!;
File.WriteAllBytes(settingsPath, new byte[] { 1, 2, 3 });
var recoveredLoad = settingsType.GetMethod("Load")!.Invoke(settingsStore, Array.Empty<object>())!;
if ((bool)recoveredLoad.GetType().GetProperty("UsedDefaults")!.GetValue(recoveredLoad)!)
    throw new InvalidOperationException("backup settings did not recover");
if (!(bool)recoveredLoad.GetType().GetProperty("UsedBackup")!.GetValue(recoveredLoad)!)
    throw new InvalidOperationException("backup settings recovery was not reported");
Console.WriteLine("SETTINGS STORE DEFAULT_SAVE_BACKUP=PASS");
Directory.Delete(settingsRoot, recursive: true);
var type = app.GetType("YimyWalk.App.Host.AppHost", true)!;
var host = Activator.CreateInstance(type, nonPublic: true)!;
var session = type.GetProperty("Session")!.GetValue(host)!;
var windows = type.GetProperty("Windows")!.GetValue(host)!;
var active = session.GetType().GetProperty("ActiveCount")!;
var windowCount = windows.GetType().GetProperty("Count")!;
type.GetMethod("Start")!.Invoke(host, Array.Empty<object>());
var displayProvider = type.GetProperty("DisplayProvider")!.GetValue(host)!;
var displayValues = ((System.Collections.IEnumerable)displayProvider.GetType().GetProperty("Displays")!.GetValue(displayProvider)!).Cast<object>().ToArray();
Console.WriteLine($"DISPLAY COUNT={displayValues.Length}");
foreach (var display in displayValues)
{
    var id = display.GetType().GetProperty("Id")!.GetValue(display)!;
    var bounds = display.GetType().GetProperty("Bounds")!.GetValue(display)!;
    var workingArea = display.GetType().GetProperty("WorkingArea")!.GetValue(display)!;
    var scale = display.GetType().GetProperty("Scale")!.GetValue(display)!;
    var primary = display.GetType().GetProperty("IsPrimary")!.GetValue(display)!;
    Console.WriteLine($"DISPLAY ID={id} BOUNDS={bounds} WORKING={workingArea} SCALE={scale} PRIMARY={primary}");
}

if (landingRequested)
{
    using var landingProbe = new Form
    {
        FormBorderStyle = FormBorderStyle.FixedToolWindow,
        StartPosition = FormStartPosition.Manual,
        ShowInTaskbar = false,
        Bounds = new Rectangle(1370, 300, 120, 240),
        BackColor = Color.CornflowerBlue,
        Text = "YimyWalk landing probe"
    };
    landingProbe.Show();
    Application.DoEvents();
    type.GetProperty("WindowLandingEnabled")!.SetValue(host, true);
    var refreshWindowSurfaces = type.GetMethod("RefreshWindowSurfaces", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new MissingMethodException(type.FullName, "RefreshWindowSurfaces");
    refreshWindowSurfaces.Invoke(host, Array.Empty<object>());
    var createAt = session.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Single(method => method.Name == "Create" && method.GetParameters().Length == 2);
    createAt.Invoke(session, new object[] { "cat", new Vector2(1430, -32) });
    var landingDeadline = Environment.TickCount64 + 5000;
    while (Environment.TickCount64 < landingDeadline)
    {
        Application.DoEvents();
        Thread.Sleep(15);
        var landingWindows = ((System.Collections.IEnumerable)windows.GetType().GetProperty("Windows")!.GetValue(windows)!).Cast<object>().ToArray();
        if (landingWindows.Length == 1 && string.Equals(Property(Property(landingWindows[0], "Instance"), "State").ToString(), "Idle", StringComparison.Ordinal))
            break;
    }
    var landingPet = ((System.Collections.IEnumerable)windows.GetType().GetProperty("Windows")!.GetValue(windows)!).Cast<object>().Single();
    var landingInstance = Property(landingPet, "Instance");
    var landingY = FootY(landingInstance);
    if (Math.Abs(landingY - landingProbe.Top) > 2)
        throw new InvalidOperationException($"window landing did not select probe top: pet={landingY}, probe={landingProbe.Top}");
    Console.WriteLine($"WINDOW LANDING TOP={landingProbe.Top} PET={landingY} PASS");
    landingProbe.Close();
    refreshWindowSurfaces.Invoke(host, Array.Empty<object>());
    var fallDeadline = Environment.TickCount64 + 5000;
    while (Environment.TickCount64 < fallDeadline && FootY(landingInstance) < landingProbe.Top + 20)
    {
        Application.DoEvents();
        Thread.Sleep(15);
    }
    if (FootY(landingInstance) < landingProbe.Top + 20)
        throw new InvalidOperationException("pet did not re-fall after landing probe closed");
    Console.WriteLine($"WINDOW LANDING CLOSE_REFALL_Y={FootY(landingInstance):0.0} PASS");
    type.GetMethod("ClearPets")!.Invoke(host, Array.Empty<object>());
    type.GetProperty("WindowLandingEnabled")!.SetValue(host, false);
}

var characterRoot = (string)type.GetProperty("CharacterRoot")!.GetValue(host)!;
var catDirectory = Path.Combine(characterRoot, "cat");
var spritePath = Path.Combine(catDirectory, "sprite.png");
var configPath = Path.Combine(catDirectory, "character.json");
var originalSprite = File.ReadAllBytes(spritePath);
var hadConfig = File.Exists(configPath);
var originalConfig = hadConfig ? File.ReadAllBytes(configPath) : Array.Empty<byte>();
try
{
    type.GetMethod("CreateMany")!.Invoke(host, new object[] { "cat", 1 });
    for (var i = 0; i < 10; i++) { Application.DoEvents(); Thread.Sleep(15); }
    var oldPet = ((System.Collections.IEnumerable)windows.GetType().GetProperty("Windows")!.GetValue(windows)!).Cast<object>().Single();
    var oldDefinition = oldPet.GetType().GetProperty("Instance")!.GetValue(oldPet)!.GetType().GetProperty("Definition")!.GetValue(oldPet.GetType().GetProperty("Instance")!.GetValue(oldPet)!);
    File.WriteAllText(configPath, "{\"schemaVersion\":1,\"displayName\":\"Reloaded Cat\"}");
    var reloadMethod = type.GetMethod("ReloadCharacters") ?? throw new MissingMethodException(type.FullName, "ReloadCharacters");
    var validFailures = (System.Collections.IEnumerable)reloadMethod.Invoke(host, Array.Empty<object>())!;
    if (validFailures.Cast<object>().Any()) throw new InvalidOperationException("valid role reload reported failures");
    type.GetMethod("CreateMany")!.Invoke(host, new object[] { "cat", 1 });
    for (var i = 0; i < 10; i++) { Application.DoEvents(); Thread.Sleep(15); }
    var pets = ((System.Collections.IEnumerable)windows.GetType().GetProperty("Windows")!.GetValue(windows)!).Cast<object>().ToArray();
    var newPet = pets.Single(candidate => !ReferenceEquals(candidate, oldPet));
    var newInstance = newPet.GetType().GetProperty("Instance")!.GetValue(newPet)!;
    var newDefinition = newInstance.GetType().GetProperty("Definition")!.GetValue(newInstance);
    if (ReferenceEquals(oldDefinition, newDefinition)) throw new InvalidOperationException("new pet retained old definition");
    File.WriteAllBytes(spritePath, new byte[] { 1, 2, 3 });
    var invalidFailures = (System.Collections.IEnumerable)reloadMethod.Invoke(host, Array.Empty<object>())!;
    if (!invalidFailures.Cast<object>().Any()) throw new InvalidOperationException("invalid role reload did not report a failure");
    var catalog = type.GetProperty("Catalog")!.GetValue(host)!;
    var definitions = catalog.GetType().GetProperty("Definitions")!.GetValue(catalog)!;
    var retained = definitions.GetType().GetProperty("Item")!.GetValue(definitions, new object[] { "cat" });
    if (!ReferenceEquals(newDefinition, retained)) throw new InvalidOperationException("failed reload replaced last good definition");
    Console.WriteLine("ROLE RELOAD SUCCESS_AND_FAILURE_RETENTION=PASS");
}
finally
{
    File.WriteAllBytes(spritePath, originalSprite);
    if (hadConfig) File.WriteAllBytes(configPath, originalConfig);
    else if (File.Exists(configPath)) File.Delete(configPath);
    type.GetMethod("ClearPets")!.Invoke(host, Array.Empty<object>());
    type.GetMethod("ReloadCharacters")?.Invoke(host, Array.Empty<object>());
}

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

static object Property(object target, string name)
    => target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(target)!;

static float FootY(object instance)
{
    var value = Property(instance, "FootPosition");
    return (float)value.GetType().GetField("Y")!.GetValue(value)!;
}

static class Native
{
    [DllImport("user32.dll")]
    public static extern uint GetGuiResources(IntPtr process, uint flags);
}
