using YimyWalk.App.Host;
using YimyWalk.App.UI.Controls;
using YimyWalk.Core.Simulation;

namespace YimyWalk.App.UI.Pages;

internal sealed class PetsPage : ManagerPage
{
    private readonly PetListProjection _projection = new();
    private readonly ListView _list = new()
    {
        Dock = DockStyle.Fill,
        View = View.Details,
        FullRowSelect = true,
        HideSelection = false,
        MultiSelect = false,
        HeaderStyle = ColumnHeaderStyle.Nonclickable,
        BorderStyle = BorderStyle.FixedSingle,
        TabStop = true
    };
    private readonly Label _empty = UiTheme.CreateBodyLabel("当前没有运行中的桌宠。");
    private readonly Label _detailTitle = UiTheme.CreateBodyLabel();
    private readonly Label _detailMeta = UiTheme.CreateMutedLabel();
    private readonly Label _saveNote = UiTheme.CreateMutedLabel("单只设置仅在本次运行中有效，重启后恢复默认。");
    private readonly Label _walkSource = UiTheme.CreateBodyLabel();
    private readonly Label _dialogueSource = UiTheme.CreateBodyLabel();
    private readonly Label _activitySource = UiTheme.CreateBodyLabel();
    private readonly ComboBox _walkOverride = CreateOverrideCombo();
    private readonly ComboBox _dialogueOverride = CreateOverrideCombo();
    private readonly ComboBox _activityMode = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
    private readonly NumericUpDown _activityRate = new() { Minimum = 0.1m, Maximum = 4m, DecimalPlaces = 1, Increment = 0.1m, Width = 90 };
    private readonly Button _remove = UiTheme.CreateButton("删除选中");
    private readonly Button _feed = UiTheme.CreateButton("喂食");
    private readonly Button _reset = UiTheme.CreateButton("恢复跟随互动默认值");
    private readonly CharacterFramePreview _preview;
    private Guid? _editingId;
    private bool _syncing;

    public PetsPage(AppHost host) : base(host)
    {
        _preview = new CharacterFramePreview(host.SurfaceCache) { Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _list.Columns.Add("实例", 210);
        _list.Columns.Add("角色", 140);
        _list.Columns.Add("状态", 90);
        _activityMode.Items.AddRange(new object[] { "跟随全局", "自定义" });
        _activityMode.SelectedIndex = 0;
        _walkOverride.Items.AddRange(new object[] { "跟随全局", "开启", "关闭" });
        _dialogueOverride.Items.AddRange(new object[] { "跟随全局", "开启", "关闭" });

        var layout = CreateRootLayout();
        AddHeading(layout, "桌宠", "管理当前运行实例；这里的单只设置不会改写全局默认。");
        var split = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, MinimumSize = new Size(0, 430), ColumnCount = 2, RowCount = 1 };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));

        var listCard = UiTheme.CreateCard();
        listCard.Dock = DockStyle.Fill;
        listCard.MinimumSize = new Size(300, 380);
        listCard.Controls.Add(_list);
        listCard.Controls.Add(_empty);
        _empty.Dock = DockStyle.Bottom;
        split.Controls.Add(listCard, 0, 0);

        var detail = BuildDetailPanel();
        split.Controls.Add(detail, 1, 0);
        AddRow(layout, split);

        _list.SelectedIndexChanged += (_, _) => SelectFromList();
        _walkOverride.SelectedIndexChanged += (_, _) => ApplyBooleanOverride(isWalk: true);
        _dialogueOverride.SelectedIndexChanged += (_, _) => ApplyBooleanOverride(isWalk: false);
        _activityMode.SelectedIndexChanged += (_, _) => ApplyActivityOverride();
        _activityRate.ValueChanged += (_, _) => ApplyActivityOverride();
        _remove.Click += (_, _) => { if (_editingId is { } id) Host.RemovePet(id); };
        _feed.Click += (_, _) => { if (_editingId is { } id) Host.FeedPet(id); };
        _reset.Click += (_, _) => { if (_editingId is { } id) Host.ResetPetBehaviorOverrides(id); RefreshFromHost(); };
        _projection.Changed += ApplyProjectionToList;
    }

    public override string PageTitle => "桌宠";

    public override void RefreshFromHost()
    {
        _projection.Refresh(Host.ActivePets);
        ApplyProjectionToList();
        ShowSelectedPet();
    }

    private Panel BuildDetailPanel()
    {
        var panel = UiTheme.CreateCard();
        panel.Dock = DockStyle.Fill;
        panel.MinimumSize = new Size(300, 380);

        var header = new Panel { Dock = DockStyle.Top, Height = 78 };
        _detailTitle.Dock = DockStyle.Top;
        _detailTitle.Font = UiTheme.SectionFont;
        _detailMeta.Dock = DockStyle.Top;
        header.Controls.Add(_detailMeta);
        header.Controls.Add(_detailTitle);
        panel.Controls.Add(header);

        var previewRow = new Panel { Dock = DockStyle.Top, Height = 184, Padding = new Padding(0, 0, 0, UiTheme.Space8) };
        _preview.Dock = DockStyle.Left;
        previewRow.Controls.Add(_preview);
        panel.Controls.Add(previewRow);

        var settings = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(0, UiTheme.Space8, 0, 0) };
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        AddSettingRow(settings, "主动走动", _walkOverride, _walkSource);
        AddSettingRow(settings, "自动气泡", _dialogueOverride, _dialogueSource);
        AddSettingRow(settings, "活跃度", _activityMode, _activitySource);
        settings.Controls.Add(_activityRate, 1, 4);
        panel.Controls.Add(settings);
        panel.Controls.Add(_saveNote);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, UiTheme.Space12, 0, 0) };
        actions.Controls.Add(_feed);
        actions.Controls.Add(_reset);
        actions.Controls.Add(_remove);
        panel.Controls.Add(actions);
        return panel;
    }

    private static void AddSettingRow(TableLayoutPanel table, string label, Control editor, Label source)
    {
        var row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(UiTheme.CreateBodyLabel(label), 0, row);
        table.Controls.Add(editor, 1, row);
        table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(source, 1, row + 1);
    }

    private void ApplyProjectionToList()
    {
        var topId = _list.TopItem?.Tag is Guid top ? top : (Guid?)null;
        _list.BeginUpdate();
        try
        {
            var byId = _list.Items.Cast<ListViewItem>().ToDictionary(item => (Guid)item.Tag!);
            var desired = _projection.Items.OrderBy(item => item.InstanceId).ToArray();
            foreach (var stale in byId.Keys.Where(id => desired.All(item => item.InstanceId != id)).ToArray())
                _list.Items.Remove(byId[stale]);
            foreach (var item in desired)
            {
                if (!byId.TryGetValue(item.InstanceId, out var row))
                {
                    row = new ListViewItem(item.InstanceId.ToString("N")[..8]) { Tag = item.InstanceId };
                    row.SubItems.Add(item.RoleName);
                    row.SubItems.Add(item.State.ToString());
                    _list.Items.Add(row);
                }
                else
                {
                    row.SubItems[1].Text = item.RoleName;
                    row.SubItems[2].Text = item.State.ToString();
                }
            }
            _empty.Visible = desired.Length == 0;
            SelectRow(_projection.SelectedInstanceId);
        }
        finally
        {
            _list.EndUpdate();
            if (topId is { } id)
                _list.Items.Cast<ListViewItem>().FirstOrDefault(item => (Guid)item.Tag! == id)?.EnsureVisible();
        }
    }

    private void SelectFromList()
    {
        if (_syncing) return;
        var id = _list.SelectedItems.Count == 0 ? (Guid?)null : (Guid)_list.SelectedItems[0].Tag!;
        _projection.SetSelected(id);
        ShowSelectedPet();
    }

    private void SelectRow(Guid? id)
    {
        _syncing = true;
        try
        {
            foreach (ListViewItem item in _list.Items)
                item.Selected = id is not null && (Guid)item.Tag! == id.Value;
        }
        finally { _syncing = false; }
    }

    private void ShowSelectedPet()
    {
        var pet = _projection.SelectedInstanceId is { } id ? Host.FindPet(id) : null;
        _editingId = pet?.InstanceId;
        _detailTitle.Text = pet is null ? "未选择桌宠" : pet.Definition.DisplayName;
        _detailMeta.Text = pet is null ? "从左侧列表选择运行实例。" : $"实例 {pet.InstanceId:N}";
        _preview.ShowIdleFrame(pet?.Definition);
        _remove.Enabled = _feed.Enabled = _reset.Enabled = pet is not null;
        _walkOverride.Enabled = _dialogueOverride.Enabled = _activityMode.Enabled = _activityRate.Enabled = pet is not null;
        if (pet is null)
        {
            _walkSource.Text = _dialogueSource.Text = _activitySource.Text = string.Empty;
            return;
        }

        _syncing = true;
        try
        {
            _walkOverride.SelectedIndex = (int)pet.BehaviorOverrides.WalkOverride;
            _dialogueOverride.SelectedIndex = (int)pet.BehaviorOverrides.DialogueOverride;
            _activityMode.SelectedIndex = pet.BehaviorOverrides.ActivityRateOverride is null ? 0 : 1;
            _activityRate.Value = (decimal)(pet.BehaviorOverrides.ActivityRateOverride ?? Host.ActivityRate);
        }
        finally { _syncing = false; }
        RefreshSources(pet);
    }

    private void RefreshSources(PetInstance pet)
    {
        _walkSource.Text = InstanceSettingState.WalkSource(pet.BehaviorOverrides, Host.DefaultWalkEnabled, Host.IsPaused);
        _dialogueSource.Text = InstanceSettingState.DialogueSource(pet.BehaviorOverrides, Host.DefaultDialogueEnabled, Host.DialogueEnabled);
        _activitySource.Text = InstanceSettingState.ActivitySource(pet.BehaviorOverrides, Host.ActivityRate);
    }

    private void ApplyBooleanOverride(bool isWalk)
    {
        if (_syncing || _editingId is not { } id || Host.FindPet(id) is not { } pet) return;
        var value = (isWalk ? _walkOverride : _dialogueOverride).SelectedIndex switch
        {
            1 => BooleanOverride.Enabled,
            2 => BooleanOverride.Disabled,
            _ => BooleanOverride.Inherit
        };
        var current = pet.BehaviorOverrides;
        Host.SetPetBehaviorOverrides(id, isWalk ? current with { WalkOverride = value } : current with { DialogueOverride = value });
        RefreshSources(pet);
    }

    private void ApplyActivityOverride()
    {
        if (_syncing || _editingId is not { } id || Host.FindPet(id) is not { } pet) return;
        var current = pet.BehaviorOverrides;
        var overrideValue = _activityMode.SelectedIndex == 0 ? null : (float?)_activityRate.Value;
        Host.SetPetBehaviorOverrides(id, current with { ActivityRateOverride = overrideValue });
        RefreshSources(pet);
    }

    private static ComboBox CreateOverrideCombo()
        => new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
}
