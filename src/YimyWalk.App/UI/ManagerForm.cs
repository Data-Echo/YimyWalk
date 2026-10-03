using YimyWalk.App.Host;

namespace YimyWalk.App.UI;

internal sealed class ManagerForm : Form
{
    private readonly AppHost _host;
    private readonly ComboBox _characters = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly Label _count = new() { AutoSize = true };
    private readonly Label _status = new() { AutoSize = true };

    public ManagerForm(AppHost host)
    {
        _host = host;
        Text = "YimyWalk";
        Width = 430;
        Height = 210;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;

        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), AutoScroll = true };
        layout.Controls.Add(new Label { Text = "角色", AutoSize = true, Margin = new Padding(3, 8, 8, 3) });
        layout.Controls.Add(_characters);
        foreach (var count in new[] { 1, 10, 50, 100 })
        {
            var button = new Button { Text = $"创建 {count}", AutoSize = true, Tag = count };
            button.Click += (_, _) => CreateMany((int)button.Tag);
            layout.Controls.Add(button);
        }
        var clear = new Button { Text = "一键清空", AutoSize = true };
        clear.Click += (_, _) => _host.ClearPets();
        layout.Controls.Add(clear);
        layout.Controls.Add(_count);
        layout.Controls.Add(_status);
        Controls.Add(layout);
        _host.Session.InstanceAdded += OnSessionChanged;
        _host.Session.InstanceRemoved += OnSessionChanged;
        _host.Batch.Changed += OnBatchChanged;
        Shown += (_, _) => RefreshCharacters();
        FormClosed += (_, _) =>
        {
            _host.Session.InstanceAdded -= OnSessionChanged;
            _host.Session.InstanceRemoved -= OnSessionChanged;
            _host.Batch.Changed -= OnBatchChanged;
            if (!_host.IsExiting) Hide();
        };
    }

    public void RefreshCharacters()
    {
        var selected = _characters.SelectedItem as RoleChoice;
        _characters.Items.Clear();
        foreach (var definition in _host.Catalog.Definitions.Values)
            _characters.Items.Add(new RoleChoice(definition.Id, definition.DisplayName));
        if (selected is not null)
            for (var i = 0; i < _characters.Items.Count; i++)
                if (_characters.Items[i] is RoleChoice choice && choice.Id.Equals(selected.Id, StringComparison.OrdinalIgnoreCase)) _characters.SelectedIndex = i;
        if (_characters.SelectedIndex < 0 && _characters.Items.Count > 0) _characters.SelectedIndex = 0;
        UpdateCount();
    }

    public void UpdateCount()
    {
        _count.Text = $"活动实例：{_host.Session.ActiveCount}";
        _status.Text = $"待创建：{_host.Batch.PendingCount}";
    }

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
        public override string ToString() => $"{Id} — {DisplayName}";
    }
}
