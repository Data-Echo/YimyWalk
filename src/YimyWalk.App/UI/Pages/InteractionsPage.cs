using YimyWalk.App.Host;

namespace YimyWalk.App.UI.Pages;

internal sealed class InteractionsPage : ManagerPage
{
    private readonly AntdUI.Switch _walk = UiTheme.CreateSwitch();
    private readonly AntdUI.Switch _defaultDialogue = UiTheme.CreateSwitch();
    private readonly AntdUI.Switch _dialogueGate = UiTheme.CreateSwitch();
    private readonly AntdUI.Switch _hunger = UiTheme.CreateSwitch();
    private readonly AntdUI.Switch _external = UiTheme.CreateSwitch();
    private readonly AntdUI.InputNumber _activity = UiTheme.CreateInputNumber(0.1m, 4m, 0.1m, 1, 110);
    private readonly Label _activityHelp = UiTheme.CreateMutedLabel("活跃度是自主行为的频率/倾向倍率，不改变移动速度、重力或动画帧率。");
    private readonly Label _status = UiTheme.CreateMutedLabel();
    private bool _syncing;

    public InteractionsPage(AppHost host) : base(host)
    {
        var layout = CreateRootLayout();
        AddHeading(layout, "互动", "全局默认供跟随实例继承；运行级限制优先于单只设置。");
        var card = UiTheme.CreateCard();
        card.Dock = DockStyle.Top;
        card.Height = 410;
        var cardTitle = new Label { Text = "全局默认", AutoSize = true, Font = UiTheme.SectionFont, Dock = DockStyle.Top };
        var checks = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(0, UiTheme.Space12, 0, 0) };
        checks.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 78));
        checks.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
        AddSettingRow(checks, "默认主动走动", "允许实例跟随此全局默认自主行动。", _walk);
        AddSettingRow(checks, "默认自动气泡", "仅作为继承来源；运行级限制优先。", _defaultDialogue);
        AddSettingRow(checks, "饥饿增长", "控制饥饿值随模拟时间增长。", _hunger);
        AddSettingRow(checks, "应用切换反馈", "关闭后停止专用的前台应用轮询。", _external);
        var activityRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, UiTheme.Space8, 0, 0) };
        activityRow.Controls.Add(UiTheme.CreateBodyLabel("默认活跃度倍率"));
        activityRow.Controls.Add(_activity);
        checks.Controls.Add(activityRow, 0, checks.RowCount++);
        checks.Controls.Add(_activityHelp, 0, checks.RowCount++);
        card.Controls.Add(checks);
        card.Controls.Add(cardTitle);

        var gate = UiTheme.CreateCard();
        gate.Dock = DockStyle.Top;
        gate.Height = 128;
        var gateTitle = new Label { Text = "运行级限制", AutoSize = true, Font = UiTheme.SectionFont, Dock = DockStyle.Top };
        _dialogueGate.Margin = new Padding(0, UiTheme.Space12, 0, 0);
        var gateRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false };
        gateRow.Controls.Add(UiTheme.CreateBodyLabel("允许显示自动气泡（运行级限制）"));
        gateRow.Controls.Add(UiTheme.WrapSwitch(_dialogueGate));
        gate.Controls.Add(gateRow);
        var gateNote = UiTheme.CreateMutedLabel("关闭后，单只实例的“强制开启”也不能绕过；恢复后会继续使用原有默认和覆盖。");
        gateNote.Dock = DockStyle.Top;
        gate.Controls.Add(gateNote);
        gate.Controls.Add(gateTitle);
        AddRow(layout, card);
        AddRow(layout, gate);
        AddRow(layout, _status);

        _walk.CheckedText = "开"; _walk.UnCheckedText = "关";
        _defaultDialogue.CheckedText = "开"; _defaultDialogue.UnCheckedText = "关";
        _dialogueGate.CheckedText = "开"; _dialogueGate.UnCheckedText = "关";
        _hunger.CheckedText = "开"; _hunger.UnCheckedText = "关";
        _external.CheckedText = "开"; _external.UnCheckedText = "关";
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

    private static void AddSettingRow(TableLayoutPanel table, string title, string description, Control editor)
    {
        var row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var text = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1, Margin = new Padding(0, 4, 0, 4) };
        text.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        text.Controls.Add(new Label { Text = title, AutoSize = true, Font = UiTheme.SectionFont, ForeColor = UiTheme.Ink }, 0, 0);
        text.Controls.Add(UiTheme.CreateMutedLabel(description), 0, 1);
        table.Controls.Add(text, 0, row);
        table.Controls.Add(UiTheme.WrapSwitch(editor), 1, row);
        table.ColumnStyles.Clear();
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 78));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
        table.ColumnCount = 2;
    }
}
