using YimyWalk.App.Host;
using YimyWalk.App.Settings;
using YimyWalk.Core.Display;

namespace YimyWalk.App.UI.Pages;

internal sealed class SettingsPage : ManagerPage
{
    private readonly NumericUpDown _scale = new() { Minimum = 0.1m, Maximum = 8m, DecimalPlaces = 1, Increment = 0.1m, Width = 90 };
    private readonly ComboBox _display = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
    private readonly CheckBox _landing = new() { Text = "允许宠物落到可见窗口顶部", AutoSize = true };
    private readonly Label _status = UiTheme.CreateBodyLabel();
    private readonly Label _diagnostic = UiTheme.CreateMutedLabel();
    private readonly Button _retry = UiTheme.CreateButton("重试保存");
    private bool _syncing;

    public SettingsPage(AppHost host) : base(host)
    {
        var layout = CreateRootLayout();
        AddHeading(layout, "设置", "应用级选项自动保存；单只桌宠覆盖不会出现在这里。");
        var card = UiTheme.CreateCard();
        card.Dock = DockStyle.Top;
        card.Controls.Add(new Label { Text = "应用外观与启动行为", AutoSize = true, Font = UiTheme.SectionFont, Dock = DockStyle.Top });
        var grid = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(0, UiTheme.Space12, 0, 0) };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        AddGridRow(grid, "桌宠显示缩放", _scale, "0.1–8.0；修改后立即应用，连续调整会合并保存。");
        AddGridRow(grid, "新建桌宠所在显示器", _display, "跟随系统或选择当前显示器；只影响后续创建位置。");
        card.Controls.Add(grid);
        card.Controls.Add(_landing);
        AddRow(layout, card);

        var saveCard = UiTheme.CreateCard();
        saveCard.Dock = DockStyle.Top;
        saveCard.Controls.Add(new Label { Text = "保存状态", AutoSize = true, Font = UiTheme.SectionFont, Dock = DockStyle.Top });
        _status.Dock = DockStyle.Top;
        _retry.Dock = DockStyle.Top;
        _retry.Margin = new Padding(0, UiTheme.Space8, 0, 0);
        _retry.Click += (_, _) => Host.RetrySettingsSave();
        saveCard.Controls.Add(_retry);
        saveCard.Controls.Add(_status);
        saveCard.Controls.Add(_diagnostic);
        AddRow(layout, saveCard);

        _scale.ValueChanged += (_, _) => { if (!_syncing) Host.GlobalScale = (float)_scale.Value; };
        _display.SelectedIndexChanged += (_, _) =>
        {
            if (_syncing || _display.SelectedItem is not DisplayChoice choice) return;
            Host.SetPreferredDisplay(choice.Id);
        };
        _landing.CheckedChanged += (_, _) => { if (!_syncing) Host.WindowLandingEnabled = _landing.Checked; };
    }

    public override string PageTitle => "设置";

    public override void RefreshFromHost()
    {
        _syncing = true;
        try
        {
            _scale.Value = (decimal)Host.GlobalScale;
            _landing.Checked = Host.WindowLandingEnabled;
            RefreshDisplaysIfNeeded();
            var status = Host.SettingsSaveState switch
            {
                SettingsSaveState.Applied => "已应用，保存中",
                SettingsSaveState.Saving => "保存中",
                SettingsSaveState.Saved => "已保存",
                SettingsSaveState.Failed => "已应用于本次运行，保存失败",
                _ => Host.SettingsSaveState.ToString()
            };
            _status.Text = $"全局设置：{status}";
            _retry.Enabled = Host.SettingsSaveState == SettingsSaveState.Failed;
            _diagnostic.Text = Host.SettingsSaveError is { Length: > 0 } error
                ? $"保存错误：{error}"
                : Host.SettingsLoad.Diagnostic ?? string.Empty;
        }
        finally { _syncing = false; }
    }

    public void RefreshStatus(string status)
    {
        _status.Text = status;
        _retry.Enabled = Host.SettingsSaveState == SettingsSaveState.Failed;
    }

    private void RefreshDisplaysIfNeeded()
    {
        var expected = new[] { (string?)null }.Concat(Host.Displays.Select(display => (string?)display.Id.Value)).ToArray();
        var current = _display.Items.Cast<DisplayChoice>().Select(choice => choice.Id).ToArray();
        if (expected.SequenceEqual(current, StringComparer.OrdinalIgnoreCase))
        {
            SelectDisplay();
            return;
        }

        _display.Items.Clear();
        _display.Items.Add(new DisplayChoice(null, "跟随系统"));
        foreach (var display in Host.Displays)
            _display.Items.Add(new DisplayChoice(display.Id.Value, DisplayLabel(display)));
        SelectDisplay();
    }

    private void SelectDisplay()
    {
        var selected = Host.PreferredDisplayId;
        for (var index = 0; index < _display.Items.Count; index++)
            if (_display.Items[index] is DisplayChoice choice && string.Equals(choice.Id, selected, StringComparison.OrdinalIgnoreCase))
            {
                _display.SelectedIndex = index;
                return;
            }
        if (_display.Items.Count > 0) _display.SelectedIndex = 0;
    }

    private static void AddGridRow(TableLayoutPanel grid, string label, Control editor, string help)
    {
        var row = grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.Controls.Add(UiTheme.CreateBodyLabel(label), 0, row);
        grid.Controls.Add(editor, 1, row);
        var helpLabel = UiTheme.CreateMutedLabel(help);
        grid.RowCount++;
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.Controls.Add(helpLabel, 1, row + 1);
    }

    private static string DisplayLabel(DisplayInfo display)
        => $"{display.Id.Value} {(display.IsPrimary ? "（主显示器）" : string.Empty)}";

    private sealed record DisplayChoice(string? Id, string Label)
    {
        public override string ToString() => Label;
    }
}
