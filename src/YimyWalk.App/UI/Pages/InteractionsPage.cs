using YimyWalk.App.Host;

namespace YimyWalk.App.UI.Pages;

internal sealed class InteractionsPage : ManagerPage
{
    private readonly CheckBox _walk = new() { Text = "默认允许主动走动", AutoSize = true };
    private readonly CheckBox _defaultDialogue = new() { Text = "默认允许自动气泡", AutoSize = true };
    private readonly CheckBox _dialogueGate = new() { Text = "允许显示自动气泡（运行级限制）", AutoSize = true };
    private readonly CheckBox _hunger = new() { Text = "启用饥饿增长", AutoSize = true };
    private readonly CheckBox _external = new() { Text = "启用应用切换反馈", AutoSize = true };
    private readonly NumericUpDown _activity = new() { Minimum = 0.1m, Maximum = 4m, DecimalPlaces = 1, Increment = 0.1m, Width = 90 };
    private readonly Label _activityHelp = UiTheme.CreateMutedLabel("活跃度是自主行为的频率/倾向倍率，不改变移动速度、重力或动画帧率。");
    private readonly Label _status = UiTheme.CreateMutedLabel();
    private bool _syncing;

    public InteractionsPage(AppHost host) : base(host)
    {
        var layout = CreateRootLayout();
        AddHeading(layout, "互动", "全局默认供跟随实例继承；运行级限制优先于单只设置。");
        var card = UiTheme.CreateCard();
        card.Dock = DockStyle.Top;
        card.Controls.Add(new Label { Text = "全局默认", AutoSize = true, Font = UiTheme.SectionFont, Dock = DockStyle.Top });
        var checks = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, Padding = new Padding(0, UiTheme.Space12, 0, 0) };
        checks.Controls.Add(_walk, 0, checks.RowCount++);
        checks.Controls.Add(_defaultDialogue, 0, checks.RowCount++);
        checks.Controls.Add(_hunger, 0, checks.RowCount++);
        checks.Controls.Add(_external, 0, checks.RowCount++);
        var activityRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, UiTheme.Space8, 0, 0) };
        activityRow.Controls.Add(UiTheme.CreateBodyLabel("默认活跃度倍率"));
        activityRow.Controls.Add(_activity);
        checks.Controls.Add(activityRow, 0, checks.RowCount++);
        checks.Controls.Add(_activityHelp, 0, checks.RowCount++);
        card.Controls.Add(checks);

        var gate = UiTheme.CreateCard();
        gate.Dock = DockStyle.Top;
        gate.Controls.Add(new Label { Text = "运行级限制", AutoSize = true, Font = UiTheme.SectionFont, Dock = DockStyle.Top });
        _dialogueGate.Margin = new Padding(0, UiTheme.Space12, 0, 0);
        gate.Controls.Add(_dialogueGate);
        gate.Controls.Add(UiTheme.CreateMutedLabel("关闭后，单只实例的“强制开启”也不能绕过；恢复后会继续使用原有默认和覆盖。"));
        AddRow(layout, card);
        AddRow(layout, gate);
        AddRow(layout, _status);

        _walk.CheckedChanged += (_, _) => { if (!_syncing) Host.DefaultWalkEnabled = _walk.Checked; };
        _defaultDialogue.CheckedChanged += (_, _) => { if (!_syncing) Host.DefaultDialogueEnabled = _defaultDialogue.Checked; };
        _dialogueGate.CheckedChanged += (_, _) => { if (!_syncing) Host.DialogueEnabled = _dialogueGate.Checked; };
        _hunger.CheckedChanged += (_, _) => { if (!_syncing) Host.HungerEnabled = _hunger.Checked; };
        _external.CheckedChanged += (_, _) => { if (!_syncing) Host.ExternalEventsEnabled = _external.Checked; };
        _activity.ValueChanged += (_, _) => { if (!_syncing) Host.ActivityRate = (float)_activity.Value; };
    }

    public override string PageTitle => "互动";

    public override void RefreshFromHost()
    {
        _syncing = true;
        try
        {
            _walk.Checked = Host.DefaultWalkEnabled;
            _defaultDialogue.Checked = Host.DefaultDialogueEnabled;
            _dialogueGate.Checked = Host.DialogueEnabled;
            _hunger.Checked = Host.HungerEnabled;
            _external.Checked = Host.ExternalEventsEnabled;
            _activity.Value = (decimal)Host.ActivityRate;
            _status.Text = $"全局设置保存状态：{Host.SettingsSaveState}";
        }
        finally { _syncing = false; }
    }
}
