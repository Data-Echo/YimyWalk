using YimyWalk.App.Host;
using YimyWalk.App.UI.Pages;

namespace YimyWalk.App.UI;

/// <summary>
/// The on-demand tray manager. Pages are cached so navigation never resets their
/// selection, scroll position, or an editor that is currently being used.
/// </summary>
internal sealed class ManagerForm : Form
{
    private readonly AppHost _host;
    private readonly Panel _pageHost = new() { Dock = DockStyle.Fill, BackColor = UiTheme.Cream };
    private readonly Dictionary<ManagerPageId, ManagerPage> _pages = new();
    private readonly Dictionary<ManagerPageId, AntdUI.Button> _navigation = new();
    private ManagerPageId _selectedPage;
    private bool _allowClose;
    private bool _refreshing;
    private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = 200 };

    public ManagerForm(AppHost host)
    {
        _host = host;
        UiTheme.ConfigureAntd();
        Text = "YimyWalk";
        Icon = AppIcon.Load();
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);
        ClientSize = new Size(1120, 720);
        MinimumSize = new Size(900, 580);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        ShowInTaskbar = true;
        KeyPreview = true;

        BuildShell();
        _refreshTimer.Tick += OnRefreshTick;
        VisibleChanged += (_, _) => UpdateRefreshSchedule();
        Resize += (_, _) => UpdateRefreshSchedule();
        FormClosing += OnFormClosing;
    }

    public ManagerPageId SelectedPage => _selectedPage;

    private void BuildShell()
    {
        var root = new TableLayoutPanel { Name = "ShellLayout", Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LogicalToDeviceUnits(UiTheme.SidebarWidth)));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);
        var sidebar = new Panel
        {
            Name = "Sidebar",
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            BackColor = UiTheme.Yellow,
            Padding = new Padding(UiTheme.Space12, UiTheme.Space16, UiTheme.Space12, LogicalToDeviceUnits(32))
        };
        root.Controls.Add(sidebar, 0, 0);

        var sidebarLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        sidebarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, LogicalToDeviceUnits(82)));
        sidebar.Controls.Add(sidebarLayout);

        var brand = new Label
        {
            Text = "YimyWalk",
            Height = 42,
            Font = UiTheme.BrandFont,
            ForeColor = UiTheme.Ink,
            TextAlign = ContentAlignment.MiddleLeft,
            TabStop = false,
            Dock = DockStyle.Bottom
        };

        var nav = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, UiTheme.Space8, 0, 0) };
        sidebarLayout.Controls.Add(nav, 0, 0);
        AddNavigation(nav, ManagerPageId.Settings, "设置");
        AddNavigation(nav, ManagerPageId.Interactions, "互动");
        AddNavigation(nav, ManagerPageId.Characters, "角色");
        AddNavigation(nav, ManagerPageId.Pets, "桌宠");
        AddNavigation(nav, ManagerPageId.Overview, "总览");

        var footer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, UiTheme.Space8, 0, 0) };
        sidebarLayout.Controls.Add(footer, 0, 1);
        var explanation = new Label
        {
            Text = "点击托盘图标打开主菜单",
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 44,
            Font = UiTheme.SmallFont,
            ForeColor = UiTheme.Ink
        };
        footer.Controls.Add(explanation);
        footer.Controls.Add(brand);

        var content = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Cream, Padding = Padding.Empty, Margin = Padding.Empty };
        root.Controls.Add(content, 1, 0);
        content.Controls.Add(_pageHost);

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
            if (page.Parent is null) _pageHost.Controls.Add(page);
            foreach (var cached in _pages.Values) cached.Visible = ReferenceEquals(cached, page);
            page.BringToFront();
            _selectedPage = pageId;
            foreach (var pair in _navigation)
            {
                pair.Value.DefaultBack = pair.Key == pageId ? UiTheme.Coral : UiTheme.Sidebar;
                pair.Value.ForeColor = pair.Key == pageId ? UiTheme.NavInk : UiTheme.Ink;
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
        if (_refreshing || IsDisposed || _host.IsExiting || !Visible || WindowState == FormWindowState.Minimized) return;
        _refreshing = true;
        try { if (_pages.TryGetValue(_selectedPage, out var page)) page.RefreshFromHost(); }
        finally { _refreshing = false; }
    }

    private void OnRefreshTick(object? sender, EventArgs e) => RefreshAllPages();

    private void UpdateRefreshSchedule()
    {
        if (IsDisposed || _host.IsExiting) return;
        _refreshTimer.Enabled = Visible && WindowState != FormWindowState.Minimized;
        if (_refreshTimer.Enabled) RefreshAllPages();
    }

    public void StopRefreshing() => _refreshTimer.Stop();

    public void HideToTray()
    {
        _refreshTimer.Stop();
        Hide();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason is CloseReason.WindowsShutDown or CloseReason.TaskManagerClosing)
        {
            _host.MarkExiting();
            _host.ExitThread();
            return;
        }
        if (_allowClose || _host.IsExiting)
        {
            _host.MarkExiting();
            return;
        }

        e.Cancel = true;
        _host.HideManagerToTray();
    }

    public void AllowClose() => _allowClose = true;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _refreshTimer.Stop();
            _refreshTimer.Tick -= OnRefreshTick;
            _refreshTimer.Dispose();
            foreach (var page in _pages.Values) page.Dispose();
            _pages.Clear();
            Icon?.Dispose();
        }
        base.Dispose(disposing);
    }
}
