using YimyWalk.App.Host;
using YimyWalk.App.UI.Pages;

namespace YimyWalk.App.UI;

/// <summary>
/// The taskbar-facing shell. Pages are cached so navigation never resets their
/// selection, scroll position, or an editor that is currently being used.
/// </summary>
internal sealed class ManagerForm : Form
{
    private readonly AppHost _host;
    private readonly Panel _pageHost = new() { Dock = DockStyle.Fill, BackColor = UiTheme.Cream };
    private readonly Label _pageTitle = new() { AutoSize = true, Font = UiTheme.SectionFont, ForeColor = UiTheme.Ink };
    private readonly Dictionary<ManagerPageId, ManagerPage> _pages = new();
    private readonly Dictionary<ManagerPageId, Button> _navigation = new();
    private ManagerPageId _selectedPage;
    private bool _allowClose;

    public ManagerForm(AppHost host)
    {
        _host = host;
        Text = "YimyWalk";
        Icon = AppIcon.Load();
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1120, 720);
        MinimumSize = new Size(900, 580);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        ShowInTaskbar = true;
        KeyPreview = true;

        BuildShell();
        _host.Session.InstanceAdded += OnSessionChanged;
        _host.Session.InstanceRemoved += OnSessionChanged;
        _host.Batch.Changed += OnBatchChanged;
        FormClosing += OnFormClosing;
        FormClosed += OnFormClosed;
    }

    public ManagerPageId SelectedPage => _selectedPage;

    private void BuildShell()
    {
        var sidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = UiTheme.SidebarWidth,
            BackColor = UiTheme.Yellow,
            Padding = new Padding(UiTheme.Space12, UiTheme.Space16, UiTheme.Space12, UiTheme.Space12)
        };
        Controls.Add(sidebar);

        var brand = new Label
        {
            Text = "YimyWalk",
            Dock = DockStyle.Top,
            Height = 42,
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = UiTheme.Ink,
            TextAlign = ContentAlignment.MiddleLeft,
            TabStop = false
        };
        sidebar.Controls.Add(brand);

        var nav = new Panel { Dock = DockStyle.Top, Height = 5 * 44 + UiTheme.Space8, Padding = new Padding(0, UiTheme.Space8, 0, 0) };
        sidebar.Controls.Add(nav);
        AddNavigation(nav, ManagerPageId.Settings, "设置");
        AddNavigation(nav, ManagerPageId.Interactions, "互动");
        AddNavigation(nav, ManagerPageId.Characters, "角色");
        AddNavigation(nav, ManagerPageId.Pets, "桌宠");
        AddNavigation(nav, ManagerPageId.Overview, "总览");

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 132, Padding = new Padding(0, UiTheme.Space12, 0, 0) };
        sidebar.Controls.Add(footer);
        var explanation = new Label
        {
            Text = "关闭主菜单后，桌宠仍会继续运行。",
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 44,
            Font = UiTheme.SmallFont,
            ForeColor = UiTheme.Ink
        };
        footer.Controls.Add(explanation);
        var exit = UiTheme.CreateButton("退出 YimyWalk");
        exit.Dock = DockStyle.Top;
        exit.Margin = new Padding(0, UiTheme.Space8, 0, 0);
        exit.Click += (_, _) => _host.ExitApplication();
        footer.Controls.Add(exit);

        var content = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Cream, Padding = Padding.Empty };
        Controls.Add(content);
        content.Controls.Add(_pageHost);
        var header = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = UiTheme.Cream, Padding = new Padding(UiTheme.Space24, 14, UiTheme.Space24, 0) };
        header.Controls.Add(_pageTitle);
        content.Controls.Add(header);

        SelectPage(ManagerPageId.Overview);
    }

    private void AddNavigation(Panel nav, ManagerPageId pageId, string label)
    {
        var button = UiTheme.CreateNavButton(label);
        button.Tag = pageId;
        button.AccessibleName = $"导航到{label}";
        button.Click += (_, _) => SelectPage(pageId);
        nav.Controls.Add(button);
        _navigation.Add(pageId, button);
    }

    public void SelectPage(ManagerPageId pageId)
    {
        if (!_pages.TryGetValue(pageId, out var page))
        {
            page = CreatePage(pageId);
            _pages.Add(pageId, page);
        }

        _pageHost.SuspendLayout();
        try
        {
            _pageHost.Controls.Clear();
            _pageHost.Controls.Add(page);
            _selectedPage = pageId;
            _pageTitle.Text = page.PageTitle;
            foreach (var pair in _navigation)
            {
                pair.Value.BackColor = pair.Key == pageId ? UiTheme.Coral : UiTheme.Yellow;
                pair.Value.AccessibleDescription = pair.Key == pageId ? "当前页面" : string.Empty;
            }
            page.RefreshFromHost();
        }
        finally { _pageHost.ResumeLayout(true); }
    }

    private ManagerPage CreatePage(ManagerPageId pageId)
        => pageId switch
        {
            ManagerPageId.Overview => new OverviewPage(_host),
            ManagerPageId.Pets => new PetsPage(_host),
            ManagerPageId.Characters => new CharactersPage(_host),
            ManagerPageId.Interactions => new InteractionsPage(_host),
            ManagerPageId.Settings => new SettingsPage(_host),
            _ => throw new ArgumentOutOfRangeException(nameof(pageId))
        };

    public void RefreshCharacters()
    {
        if (_pages.TryGetValue(ManagerPageId.Characters, out var page) && page is CharactersPage characters)
            characters.RefreshCharacters();
        RefreshAllPages();
    }

    public void SetReloadStatus(int loadedCount, int failureCount)
    {
        if (_pages.TryGetValue(ManagerPageId.Characters, out var page) && page is CharactersPage characters)
            characters.SetReloadStatus(loadedCount, failureCount);
    }

    public void SetSettingsStatus(string status)
    {
        if (_pages.TryGetValue(ManagerPageId.Settings, out var page) && page is SettingsPage settings)
            settings.RefreshStatus(status);
        RefreshAllPages();
    }

    public void UpdateCount() => RefreshAllPages();

    public void RefreshAllPages()
    {
        foreach (var page in _pages.Values) page.RefreshFromHost();
    }

    private void OnSessionChanged(YimyWalk.Core.Simulation.PetInstance _)
        => RefreshAllPages();

    private void OnBatchChanged() => RefreshAllPages();

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_allowClose || _host.IsExiting || e.CloseReason is CloseReason.WindowsShutDown or CloseReason.TaskManagerClosing)
        {
            _host.MarkExiting();
            return;
        }

        e.Cancel = true;
        WindowState = FormWindowState.Minimized;
    }

    private void OnFormClosed(object? sender, FormClosedEventArgs e)
    {
        _host.Session.InstanceAdded -= OnSessionChanged;
        _host.Session.InstanceRemoved -= OnSessionChanged;
        _host.Batch.Changed -= OnBatchChanged;
    }

    public void AllowClose() => _allowClose = true;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var page in _pages.Values) page.Dispose();
            _pages.Clear();
            Icon?.Dispose();
        }
        base.Dispose(disposing);
    }
}
