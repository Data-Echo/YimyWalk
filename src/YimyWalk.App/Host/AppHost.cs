using YimyWalk.App.Rendering;
using YimyWalk.App.Settings;
using YimyWalk.App.UI;
using YimyWalk.App.Windows;
using YimyWalk.Core.Characters;
using YimyWalk.Core.Display;
using YimyWalk.Core.Settings;
using YimyWalk.Core.Simulation;
using System.Diagnostics;

namespace YimyWalk.App.Host;

internal sealed class AppHost : ApplicationContext
{
    private readonly DesktopWorldSurface _surface = new();
    private readonly PetWindowRegistry _windows = new();
    private readonly FrameSurfaceCache _surfaceCache = new();
    private readonly CharacterCatalog _catalog = new();
    private readonly SettingsStore _settingsStore = new();
    private readonly PetSession _session;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly BatchCreationQueue _batch;
    private readonly ManagerForm _manager;
    private readonly TrayIconHost _tray;
    private readonly HashSet<string> _registeredDefinitionIds = new(StringComparer.OrdinalIgnoreCase);
    private SettingsDocument _settings = SettingsDocument.Defaults;
    private SettingsLoadResult _settingsLoad = new(SettingsDocument.Defaults, UsedDefaults: true, UsedBackup: false, null);
    private bool _disposed;
    private long _lastTickTimestamp;
    internal bool IsExiting { get; private set; }

    public AppHost()
    {
        _session = new PetSession(_surface, new SystemRandomSource());
        _session.InstanceAdded += OnInstanceAdded;
        _session.InstanceRemoved += OnInstanceRemoved;
        _timer = new System.Windows.Forms.Timer { Interval = 16 };
        _timer.Tick += (_, _) => Tick();
        MainForm = new Form { ShowInTaskbar = false, Opacity = 0, Width = 1, Height = 1 };
        MainForm.Load += (_, _) => MainForm.Hide();
        _batch = new BatchCreationQueue(() =>
        {
            if (!_catalog.TryGet(_settings.SelectedRoleId, out var definition))
                definition = _catalog.Definitions.Values.FirstOrDefault()
                    ?? throw new InvalidOperationException("no character role is loaded");
            return _session.Create(definition.Id);
        });
        _manager = new ManagerForm(this);
        _tray = new TrayIconHost(this);
    }

    public PetSession Session => _session;
    public PetWindowRegistry Windows => _windows;
    public CharacterCatalog Catalog => _catalog;
    public string CharacterRoot { get; private set; } = string.Empty;
    public BatchCreationQueue Batch => _batch;
    public SettingsDocument Settings => _settings;
    public SettingsLoadResult SettingsLoad => _settingsLoad;
    public string SelectedRoleId => _settings.SelectedRoleId;
    public float GlobalScale => _settings.Scale;
    public IDisplayProvider DisplayProvider => _surface.DisplayProvider;
    public IReadOnlyList<DisplayInfo> Displays => _surface.Snapshot.Displays;
    public bool WindowLandingEnabled
    {
        get => _surface.WindowLandingEnabled;
        set
        {
            if (_surface.WindowLandingEnabled == value) return;
            _surface.WindowLandingEnabled = value;
            if (!_disposed && CharacterRoot.Length > 0)
            {
                _settings = _settings with { WindowLandingEnabled = value };
                SaveSettings();
            }
        }
    }
    public bool WindowSurfaceSnapshotValid => _surface.WindowSurfaceSnapshotValid;
    public IReadOnlyList<WindowSurfaceCandidate> WindowSurfaceCandidates => _surface.WindowSurfaceCandidates;
    public int CachedSurfaceCount => _surfaceCache.Count;

    public void RefreshWindowSurfaces() => _surface.RefreshWindowSurfaces();

    public void Start()
    {
        _surface.ExcludeWindow = IsHostWindow;
        if (MainForm is { } form)
        _surface.RefreshForWindow(form.Handle);
        else
            _surface.RefreshNow();
        _surface.RefreshWindowSurfaces();
        _settingsLoad = _settingsStore.Load();
        _settings = _settingsLoad.Settings;
        _surface.SetPreferredDisplay(_settings.PreferredDisplayId);
        _surface.WindowLandingEnabled = _settings.WindowLandingEnabled;
        var root = Path.Combine(AppContext.BaseDirectory, "Characters");
        if (!Directory.Exists(root)) root = Path.Combine(Environment.CurrentDirectory, "Characters");
        CharacterRoot = root;
        var definitions = _catalog.Scan(root);
        ApplyDefinitions(definitions);
        _timer.Start();
        _lastTickTimestamp = Stopwatch.GetTimestamp();
        _manager.RefreshCharacters();
        _manager.SetReloadStatus(definitions.Count, _catalog.LastFailures.Count);
        _manager.SetSettingsStatus(GetSettingsStatus());
    }

    public IReadOnlyList<CharacterScanFailure> ReloadCharacters()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (string.IsNullOrWhiteSpace(CharacterRoot))
            throw new InvalidOperationException("The host has not been started.");

        var definitions = _catalog.Scan(CharacterRoot);
        ApplyDefinitions(definitions);
        _manager.RefreshCharacters();
        _manager.SetReloadStatus(definitions.Count, _catalog.LastFailures.Count);
        return _catalog.LastFailures;
    }

    public void CreateMany(string id, int count)
    {
        _session.CreateMany(_catalog.TryGet(id, out var definition) ? definition : throw new KeyNotFoundException(id), count);
    }

    public void ClearPets()
    {
        _batch.Cancel();
        _session.Clear();
    }

    public void QueueCreate(string id, int count)
    {
        if (!_catalog.TryGet(id, out var definition)) return;
        _batch.Enqueue(() => _session.Create(definition.Id), count);
    }

    public void SetSelectedRole(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !_catalog.TryGet(id, out _)) return;
        if (_settings.SelectedRoleId.Equals(id, StringComparison.OrdinalIgnoreCase)) return;
        _settings = _settings with { SelectedRoleId = id };
        SaveSettings();
    }

    public void ShowManager()
    {
        _manager.Show();
        _manager.BringToFront();
    }

    public void SetPetsVisible(bool visible)
    {
        foreach (var window in _windows.Windows)
            window.SetVisibleWithoutActivation(visible);
    }

    public void ExitApplication()
    {
        IsExiting = true;
        _batch.Cancel();
        ExitThread();
    }

    private void Tick()
    {
        var now = Stopwatch.GetTimestamp();
        var delta = _lastTickTimestamp == 0
            ? _timer.Interval / 1000d
            : (now - _lastTickTimestamp) / (double)Stopwatch.Frequency;
        _lastTickTimestamp = now;
        _session.Tick(Math.Clamp(delta, 0, PetEngine.MaxDeltaSeconds));
        foreach (var window in _windows.Windows) window.RefreshFrame();
    }

    private void OnInstanceAdded(PetInstance instance)
    {
        var window = new PetWindow(instance, _session, _surfaceCache, _settings.Scale);
        _windows.Add(window);
    }

    private void OnInstanceRemoved(PetInstance instance)
    {
        _windows.Remove(instance.InstanceId);
        _batch.NotifyRemoved();
    }

    private bool IsHostWindow(IntPtr handle)
    {
        if (MainForm?.IsHandleCreated == true && MainForm.Handle == handle) return true;
        if (_manager.IsHandleCreated && _manager.Handle == handle) return true;
        return _windows.Windows.Any(window => window.IsHandleCreated && window.Handle == handle);
    }

    private void ApplyDefinitions(IReadOnlyDictionary<string, CharacterDefinition> definitions)
    {
        foreach (var definition in definitions.Values)
            _session.ReplaceDefinition(definition.Id, definition);

        foreach (var removedId in _registeredDefinitionIds.Where(id => !definitions.ContainsKey(id)).ToArray())
            _session.RemoveDefinition(removedId);

        _registeredDefinitionIds.Clear();
        foreach (var id in definitions.Keys)
            _registeredDefinitionIds.Add(id);
    }

    private string GetSettingsStatus()
    {
        if (_settingsLoad.UsedBackup) return "Settings: recovered from backup";
        if (_settingsLoad.UsedDefaults)
            return string.IsNullOrWhiteSpace(_settingsLoad.Diagnostic)
                ? "Settings: using defaults"
                : $"Settings: using defaults ({_settingsLoad.Diagnostic})";
        return "Settings: loaded";
    }

    private void SaveSettings()
    {
        try
        {
            _settingsStore.Save(_settings);
            _settingsLoad = new SettingsLoadResult(_settings, UsedDefaults: false, UsedBackup: false, null);
            if (!_disposed) _manager.SetSettingsStatus("Settings: saved");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (!_disposed) _manager.SetSettingsStatus($"Settings: save failed ({ex.Message})");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;
        if (disposing)
        {
            if (CharacterRoot.Length > 0) SaveSettings();
            _timer.Stop();
            _timer.Dispose();
            _batch.Dispose();
            _tray.Dispose();
            _manager.Dispose();
            _session.Dispose();
            _catalog.Dispose();
            _registeredDefinitionIds.Clear();
            _windows.Dispose();
            _surfaceCache.Dispose();
            _surface.Dispose();
            MainForm?.Dispose();
        }
        base.Dispose(disposing);
    }
}
