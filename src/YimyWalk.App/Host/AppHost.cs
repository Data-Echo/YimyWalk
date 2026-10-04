using YimyWalk.App.Rendering;
using YimyWalk.App.UI;
using YimyWalk.App.Windows;
using YimyWalk.Core.Characters;
using YimyWalk.Core.Simulation;

namespace YimyWalk.App.Host;

internal sealed class AppHost : ApplicationContext
{
    private readonly DesktopWorldSurface _surface = new();
    private readonly PetWindowRegistry _windows = new();
    private readonly FrameSurfaceCache _surfaceCache = new();
    private readonly CharacterCatalog _catalog = new();
    private readonly PetSession _session;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly BatchCreationQueue _batch;
    private readonly ManagerForm _manager;
    private readonly TrayIconHost _tray;
    private bool _disposed;
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
            if (!_catalog.TryGet("cat", out var definition)) throw new InvalidOperationException("cat role is not loaded");
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

    public void Start()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "Characters");
        if (!Directory.Exists(root)) root = Path.Combine(Environment.CurrentDirectory, "Characters");
        CharacterRoot = root;
        var definitions = _catalog.Scan(root);
        foreach (var definition in definitions.Values)
            _session.RegisterDefinition(definition.Id, definition);
        _timer.Start();
        _manager.RefreshCharacters();
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
        _session.Tick(_timer.Interval / 1000d);
        foreach (var window in _windows.Windows) window.RefreshFrame();
    }

    private void OnInstanceAdded(PetInstance instance)
    {
        var window = new PetWindow(instance, _session, _surfaceCache);
        _windows.Add(window);
    }

    private void OnInstanceRemoved(PetInstance instance)
    {
        _windows.Remove(instance.InstanceId);
        _batch.NotifyRemoved();
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;
        if (disposing)
        {
            _timer.Stop();
            _timer.Dispose();
            _batch.Dispose();
            _tray.Dispose();
            _manager.Dispose();
            _session.Dispose();
            _catalog.Dispose();
            _windows.Dispose();
            _surfaceCache.Dispose();
            MainForm?.Dispose();
        }
        base.Dispose(disposing);
    }
}
