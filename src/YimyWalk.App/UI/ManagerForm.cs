using YimyWalk.App.Host;

namespace YimyWalk.App.UI;

internal sealed class ManagerForm : Form
{
    private readonly AppHost _host;
    private readonly ComboBox _characters = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly Label _count = new() { AutoSize = true };
    private readonly Label _status = new() { AutoSize = true };
    private readonly Label _reloadStatus = new() { AutoSize = true };
    private readonly Label _settingsStatus = new() { AutoSize = true, MaximumSize = new Size(390, 0) };
    private readonly CheckBox _windowLanding = new() { Text = "允许宠物落到可见窗口顶部", AutoSize = true };
    private readonly CheckBox _hungerEnabled = new() { Text = "启用饥饿增长", AutoSize = true, Checked = true };
    private bool _synchronizingSelection;

    public ManagerForm(AppHost host)
    {
        _host = host;
        Text = "YimyWalk";
        Width = 430;
        Height = 275;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;

        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), AutoScroll = true };
        layout.Controls.Add(new Label { Text = "\u89d2\u8272", AutoSize = true, Margin = new Padding(3, 8, 8, 3) });
        layout.Controls.Add(_characters);
        foreach (var count in new[] { 1, 10, 50, 100 })
        {
            var button = new Button { Text = $"\u521b\u5efa {count}", AutoSize = true, Tag = count };
            button.Click += (_, _) => CreateMany((int)button.Tag);
            layout.Controls.Add(button);
        }
        var clear = new Button { Text = "\u4e00\u952e\u6e05\u7a7a", AutoSize = true };
        clear.Click += (_, _) => _host.ClearPets();
        var feed = new Button { Text = "\u5582\u98df\u5168\u90e8", AutoSize = true };
        feed.Click += (_, _) => _host.FeedAll();
        // Keep the established manager command text; the desktop smoke uses
        // the visible label as its real entry-point assertion.
        var refresh = new Button { Text = "\u5237\u65B0\u89D2\u8272", AutoSize = true };
        refresh.Click += (_, _) => _host.ReloadCharacters();
        layout.Controls.Add(refresh);
        layout.Controls.Add(clear);
        layout.Controls.Add(feed);
        layout.Controls.Add(_windowLanding);
        layout.Controls.Add(_hungerEnabled);
        layout.Controls.Add(_count);
        layout.Controls.Add(_status);
        layout.Controls.Add(_reloadStatus);
        layout.Controls.Add(_settingsStatus);
        Controls.Add(layout);
        _host.Session.InstanceAdded += OnSessionChanged;
        _host.Session.InstanceRemoved += OnSessionChanged;
        _host.Batch.Changed += OnBatchChanged;
        _characters.SelectedIndexChanged += (_, _) =>
        {
            if (!_synchronizingSelection && _characters.SelectedItem is RoleChoice role)
                _host.SetSelectedRole(role.Id);
        };
        _windowLanding.CheckedChanged += (_, _) =>
        {
            if (_windowLanding.Checked != _host.WindowLandingEnabled)
                _host.WindowLandingEnabled = _windowLanding.Checked;
        };
        _hungerEnabled.CheckedChanged += (_, _) =>
        {
            if (_hungerEnabled.Checked != _host.HungerEnabled)
                _host.HungerEnabled = _hungerEnabled.Checked;
        };
        Shown += (_, _) => RefreshCharacters();
        FormClosing += (_, e) =>
        {
            if (!_host.IsExiting)
            {
                e.Cancel = true;
                Hide();
            }
        };
        FormClosed += (_, _) =>
        {
            _host.Session.InstanceAdded -= OnSessionChanged;
            _host.Session.InstanceRemoved -= OnSessionChanged;
            _host.Batch.Changed -= OnBatchChanged;
        };
    }

    public void RefreshCharacters()
    {
        _synchronizingSelection = true;
        try
        {
            _characters.Items.Clear();
            foreach (var definition in _host.Catalog.Definitions.Values)
                _characters.Items.Add(new RoleChoice(definition.Id, definition.DisplayName));

            for (var i = 0; i < _characters.Items.Count; i++)
                if (_characters.Items[i] is RoleChoice choice && choice.Id.Equals(_host.SelectedRoleId, StringComparison.OrdinalIgnoreCase))
                {
                    _characters.SelectedIndex = i;
                    break;
                }
            if (_characters.SelectedIndex < 0 && _characters.Items.Count > 0) _characters.SelectedIndex = 0;
        }
        finally
        {
            _synchronizingSelection = false;
        }

        // A saved role may have been removed. Persist the first usable role.
        if (_characters.SelectedItem is RoleChoice selected &&
            !selected.Id.Equals(_host.SelectedRoleId, StringComparison.OrdinalIgnoreCase))
            _host.SetSelectedRole(selected.Id);
        UpdateCount();
        _windowLanding.Checked = _host.WindowLandingEnabled;
        _hungerEnabled.Checked = _host.HungerEnabled;
    }

    public void UpdateCount()
    {
        _count.Text = $"\u6d3b\u52a8\u5b9e\u4f8b\uff1a{_host.Session.ActiveCount}";
        _status.Text = $"\u5f85\u521b\u5efa\uff1a{_host.Batch.PendingCount}";
    }

    public void SetReloadStatus(int loadedCount, int failureCount)
        => _reloadStatus.Text = $"\u89d2\u8272\uff1a{loadedCount}\uff0c\u5237\u65b0\u5931\u8d25\uff1a{failureCount}";

    public void SetSettingsStatus(string status) => _settingsStatus.Text = status;

    private void CreateMany(int count)
    {
        if (_characters.SelectedItem is RoleChoice role) _host.QueueCreate(role.Id, count);
        UpdateCount();
    }

    private void OnSessionChanged(YimyWalk.Core.Simulation.PetInstance _)
        => UpdateCount();

    private void OnBatchChanged() => UpdateCount();

    private sealed record RoleChoice(string Id, string DisplayName)
    {
        public override string ToString() => $"{Id} \u2014 {DisplayName}";
    }
}
