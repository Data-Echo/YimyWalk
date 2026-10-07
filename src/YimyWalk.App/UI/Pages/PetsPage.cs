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
    private readonly Label _runtime = UiTheme.CreateBodyLabel();
    private readonly Label _operation = UiTheme.CreateMutedLabel();
    private readonly Label _saveNote = UiTheme.CreateMutedLabel("单只设置仅在本次运行中有效，重启后恢复默认。");
    private readonly Label _walkSource = UiTheme.CreateBodyLabel();
    private readonly Label _dialogueSource = UiTheme.CreateBodyLabel();
    private readonly Label _activitySource = UiTheme.CreateBodyLabel();
    private readonly Label _scaleSource = UiTheme.CreateBodyLabel();
    private readonly Label _jumpDesireSource = UiTheme.CreateBodyLabel();
    private readonly Label _climbDesireSource = UiTheme.CreateBodyLabel();
    private readonly Label _windowJumpDesireSource = UiTheme.CreateBodyLabel();
    private readonly AntdUI.Select _walkOverride = CreateOverrideCombo();
    private readonly AntdUI.Select _dialogueOverride = CreateOverrideCombo();
    private readonly AntdUI.Select _activityMode = UiTheme.CreateSelect(120);
    private readonly AntdUI.InputNumber _activityRate = UiTheme.CreateInputNumber(0.1m, 4m, 0.1m, 1, 90);
    private readonly AntdUI.Select _scaleMode = UiTheme.CreateSelect(120);
    private readonly AntdUI.InputNumber _scale = UiTheme.CreateInputNumber(0.1m, 8m, 0.1m, 1, 90);
    private readonly AntdUI.Select _jumpDesireMode = UiTheme.CreateSelect(120);
    private readonly AntdUI.InputNumber _jumpDesire = UiTheme.CreateInputNumber(0m, 100m, 1m, 0, 90);
    private readonly AntdUI.Select _climbDesireMode = UiTheme.CreateSelect(120);
    private readonly AntdUI.InputNumber _climbDesire = UiTheme.CreateInputNumber(0m, 100m, 1m, 0, 90);
    private readonly AntdUI.Select _windowJumpDesireMode = UiTheme.CreateSelect(120);
    private readonly AntdUI.InputNumber _windowJumpDesire = UiTheme.CreateInputNumber(0m, 100m, 1m, 0, 90);
    private readonly AntdUI.Button _remove = UiTheme.CreateButton("删除选中");
    private readonly AntdUI.Button _feed = UiTheme.CreateButton("喂食");
    private readonly AntdUI.Button _reset = UiTheme.CreateButton("恢复跟随互动默认值");
    private readonly CharacterFramePreview _preview;
    private Guid? _editingId;
    private bool _syncing;
    private bool _bindingList;

    public PetsPage(AppHost host) : base(host)
    {
        _preview = new CharacterFramePreview(host.SurfaceCache) { Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _list.Columns.Add("实例", 90);
        _list.Columns.Add("角色", 110);
        _list.Columns.Add("状态", 80);
        _activityMode.Items.AddRange(new object[] { "跟随全局", "自定义" });
        _activityMode.SelectedIndex = 0;
        _scaleMode.Items.AddRange(new object[] { "跟随全局", "自定义" });
        _scaleMode.SelectedIndex = 0;
        _jumpDesireMode.Items.AddRange(new object[] { "跟随全局", "自定义" });
        _jumpDesireMode.SelectedIndex = 0;
        _climbDesireMode.Items.AddRange(new object[] { "跟随全局", "自定义" });
        _climbDesireMode.SelectedIndex = 0;
        _windowJumpDesireMode.Items.AddRange(new object[] { "跟随全局", "自定义" });
        _windowJumpDesireMode.SelectedIndex = 0;
        _walkOverride.Items.AddRange(new object[] { "跟随全局", "开启", "关闭" });
        _dialogueOverride.Items.AddRange(new object[] { "跟随全局", "开启", "关闭" });

        var layout = CreateRootLayout();
        // The page heading is content-sized; the list/detail split must take
        // the remaining page height instead of staying at its 430px minimum.
        layout.Dock = DockStyle.Fill;
        layout.AutoSize = false;
        AddHeading(layout, "桌宠", "管理当前运行实例；这里的单只设置不会改写全局默认。");
        var split = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = false, MinimumSize = new Size(0, 430), ColumnCount = 2, RowCount = 1 };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));

        var listCard = UiTheme.CreateCard();
        listCard.Dock = DockStyle.Fill;
        listCard.MinimumSize = new Size(0, 380);
        listCard.AutoSize = false;
        listCard.Controls.Add(_list);
        listCard.Controls.Add(_empty);
        _empty.Dock = DockStyle.Bottom;
        split.Controls.Add(listCard, 0, 0);

        var detail = BuildDetailPanel();
        split.Controls.Add(detail, 1, 0);
        AddRow(layout, split);
        split.Dock = DockStyle.Fill;
        layout.RowStyles[layout.RowCount - 1] = new RowStyle(SizeType.Percent, 100f);

        _list.SelectedIndexChanged += (_, _) => SelectFromList();
        _walkOverride.SelectedIndexChanged += (_, _) => ApplyBooleanOverride(isWalk: true);
        _dialogueOverride.SelectedIndexChanged += (_, _) => ApplyBooleanOverride(isWalk: false);
        _activityMode.SelectedIndexChanged += (_, _) => ApplyActivityOverride();
        _activityRate.ValueChanged += (_, _) => ApplyActivityOverride();
        _scaleMode.SelectedIndexChanged += (_, _) => ApplyScaleOverride();
        _scale.ValueChanged += (_, _) => ApplyScaleOverride();
        _jumpDesireMode.SelectedIndexChanged += (_, _) => ApplyDesireOverride(isJump: true);
        _jumpDesire.ValueChanged += (_, _) => ApplyDesireOverride(isJump: true);
        _climbDesireMode.SelectedIndexChanged += (_, _) => ApplyDesireOverride(isJump: false);
        _climbDesire.ValueChanged += (_, _) => ApplyDesireOverride(isJump: false);
        _windowJumpDesireMode.SelectedIndexChanged += (_, _) => ApplyWindowJumpDesireOverride();
        _windowJumpDesire.ValueChanged += (_, _) => ApplyWindowJumpDesireOverride();
        _remove.Click += (_, _) =>
        {
            if (_editingId is not { } id) return;
            _operation.Text = Host.RemovePet(id) ? "已删除选中实例。" : "目标已不存在，已同步列表。";
            RefreshFromHost();
        };
        _feed.Click += (_, _) => { if (_editingId is { } id) Host.FeedPet(id); };
        _reset.Click += (_, _) => { if (_editingId is { } id) Host.ResetPetBehaviorOverrides(id); RefreshFromHost(); };
    }

    public override string PageTitle => "桌宠";

    public override void RefreshFromHost()
    {
        if (_bindingList) return;
        _projection.Refresh(Host.CapturePets());
        ApplyProjectionToList();
        ShowSelectedPet();
    }

    private AntdUI.Panel BuildDetailPanel()
    {
        var panel = UiTheme.CreateCard();
        panel.Dock = DockStyle.Fill;
        panel.AutoScroll = true;
        panel.MinimumSize = new Size(0, 380);

        _detailTitle.Dock = DockStyle.Top;
        _detailTitle.Font = UiTheme.SectionFont;
        _detailMeta.Dock = DockStyle.Top;
        _runtime.Dock = DockStyle.Top;
        _saveNote.Dock = DockStyle.Top;
        _operation.Dock = DockStyle.Top;
        // Keep the idle1 preview useful while leaving the first instance-size
        // row visible at the normal 1120x720 window size. Smaller windows can
        // still scroll the card for the action buttons below the settings.
        var previewRow = new Panel { Dock = DockStyle.Top, Height = 128, Padding = new Padding(0, 0, 0, UiTheme.Space8) };
        _preview.Dock = DockStyle.Left;
        previewRow.Controls.Add(_preview);
        var settings = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, RowCount = 0, Padding = new Padding(0, UiTheme.Space8, 0, 0) };
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        AddSettingRow(settings, "主动走动", _walkOverride, _walkSource);
        AddSettingRow(settings, "自动气泡", _dialogueOverride, _dialogueSource);
        var activityEditors = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Margin = Padding.Empty };
        activityEditors.Controls.Add(_activityMode);
        activityEditors.Controls.Add(_activityRate);
        AddSettingRow(settings, "活跃度倍率", activityEditors, _activitySource);
        var scaleEditors = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Margin = Padding.Empty };
        scaleEditors.Controls.Add(_scaleMode);
        scaleEditors.Controls.Add(_scale);
        AddSettingRow(settings, "实例大小", scaleEditors, _scaleSource);
        var jumpDesireEditors = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Margin = Padding.Empty };
        jumpDesireEditors.Controls.Add(_jumpDesireMode);
        jumpDesireEditors.Controls.Add(_jumpDesire);
        AddSettingRow(settings, "跳跃欲望 (%)", jumpDesireEditors, _jumpDesireSource);
        var climbDesireEditors = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Margin = Padding.Empty };
        climbDesireEditors.Controls.Add(_climbDesireMode);
        climbDesireEditors.Controls.Add(_climbDesire);
        AddSettingRow(settings, "攀爬欲望 (%)", climbDesireEditors, _climbDesireSource);
        var windowJumpDesireEditors = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Margin = Padding.Empty };
        windowJumpDesireEditors.Controls.Add(_windowJumpDesireMode);
        windowJumpDesireEditors.Controls.Add(_windowJumpDesire);
        AddSettingRow(settings, "窗口跳跃欲望 (%)", windowJumpDesireEditors, _windowJumpDesireSource);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, UiTheme.Space12, 0, 0) };
        actions.Controls.Add(_feed);
        actions.Controls.Add(_reset);
        actions.Controls.Add(_remove);
        // Dock.Top lays out the most recently added control first. Add the
        // detail sections in reverse visual order so the title remains at the
        // top and actions stay below the settings.
        panel.Controls.Add(_operation);
        panel.Controls.Add(actions);
        panel.Controls.Add(_saveNote);
        panel.Controls.Add(settings);
        panel.Controls.Add(previewRow);
        panel.Controls.Add(_runtime);
        panel.Controls.Add(_detailMeta);
        panel.Controls.Add(_detailTitle);
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
        if (_bindingList) return;
        // Selection notifications are synchronous, including during native row removal.
        // BeginUpdate only stops painting; it does not suppress those notifications.
        _bindingList = true;
        var topId = _list.TopItem?.Tag is Guid top ? top : (Guid?)null;
        var oldOrder = _list.Items.Cast<ListViewItem>().Select(row => (Guid)row.Tag!).ToArray();
        _list.BeginUpdate();
        try
        {
            var byId = _list.Items.Cast<ListViewItem>().ToDictionary(item => (Guid)item.Tag!);
            var desired = _projection.Items.OrderBy(item => item.InstanceId).ToArray();
            var desiredIds = desired.Select(item => item.InstanceId).ToHashSet();
            foreach (var stale in byId.Keys.Where(id => !desiredIds.Contains(id)).ToArray())
                _list.Items.Remove(byId[stale]);
            for (var index = 0; index < desired.Length; index++)
            {
                var item = desired[index];
                if (!byId.TryGetValue(item.InstanceId, out var row))
                {
                    row = new ListViewItem(item.InstanceId.ToString("N")[..8]) { Tag = item.InstanceId };
                    row.SubItems.Add(item.RoleName);
                    row.SubItems.Add(item.State.ToString());
                    _list.Items.Insert(index, row);
                }
                else
                {
                    if (row.SubItems[1].Text != item.RoleName) row.SubItems[1].Text = item.RoleName;
                    if (row.SubItems[2].Text != item.State.ToString()) row.SubItems[2].Text = item.State.ToString();
                }
            }
            _empty.Visible = desired.Length == 0;
            SelectRow(_projection.SelectedInstanceId);
        }
        finally
        {
            try
            {
                _list.EndUpdate();
                // Resolve the old top row or the nearest surviving neighbour by identity.
                if (topId is { } id && _list.Items.Count > 0)
                {
                    var live = _list.Items.Cast<ListViewItem>().ToDictionary(row => (Guid)row.Tag!);
                    var neighbours = oldOrder.Skip(Array.IndexOf(oldOrder, id))
                        .Concat(oldOrder.Take(Array.IndexOf(oldOrder, id)).Reverse());
                    var nextTop = neighbours.FirstOrDefault(live.ContainsKey);
                    if (live.TryGetValue(nextTop, out var topRow) && _list.TopItem != topRow) _list.TopItem = topRow;
                }
            }
            finally { _bindingList = false; }
        }
    }

    private void SelectFromList()
    {
        if (_syncing || _bindingList) return;
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
            {
                var selected = id is not null && (Guid)item.Tag! == id.Value;
                if (item.Selected != selected) item.Selected = selected;
            }
        }
        finally { _syncing = false; }
    }

    private void ShowSelectedPet()
    {
        var pet = _projection.Items.FirstOrDefault(item => item.InstanceId == _projection.SelectedInstanceId);
        var changedTarget = _editingId != pet?.InstanceId;
        _editingId = pet?.InstanceId;
        _detailTitle.Text = pet is null ? "未选择桌宠" : pet.Definition.DisplayName;
        _detailMeta.Text = pet is null ? "从左侧列表选择运行实例。" : $"实例 {pet.InstanceId.ToString("N")[..8]}";
        _runtime.Text = pet is null ? string.Empty : $"状态：{pet.State} · 饥饿：{pet.Hunger:0.0}\n位置：{pet.Position.X:0}, {pet.Position.Y:0}";
        _preview.ShowIdleFrame(pet?.Definition);
        _remove.Enabled = _feed.Enabled = _reset.Enabled = pet is not null;
        _walkOverride.Enabled = _dialogueOverride.Enabled = _activityMode.Enabled = pet is not null;
        _scaleMode.Enabled = _jumpDesireMode.Enabled = _climbDesireMode.Enabled = _windowJumpDesireMode.Enabled = pet is not null;
        if (pet is null)
        {
            _walkSource.Text = _dialogueSource.Text = _activitySource.Text = _scaleSource.Text = string.Empty;
            _jumpDesireSource.Text = _climbDesireSource.Text = _windowJumpDesireSource.Text = string.Empty;
            _activityRate.Enabled = _scale.Enabled = _jumpDesire.Enabled = _climbDesire.Enabled = _windowJumpDesire.Enabled = false;
            return;
        }

        _syncing = true;
        try
        {
            if (changedTarget || !_walkOverride.ContainsFocus) _walkOverride.SelectedIndex = (int)pet.BehaviorOverrides.WalkOverride;
            if (changedTarget || !_dialogueOverride.ContainsFocus) _dialogueOverride.SelectedIndex = (int)pet.BehaviorOverrides.DialogueOverride;
            if (changedTarget || !_activityMode.ContainsFocus) _activityMode.SelectedIndex = pet.BehaviorOverrides.ActivityRateOverride is null ? 0 : 1;
            if (changedTarget || !_activityRate.ContainsFocus) _activityRate.Value = (decimal)(pet.BehaviorOverrides.ActivityRateOverride ?? Host.ActivityRate);
            if (changedTarget || !_scaleMode.ContainsFocus) _scaleMode.SelectedIndex = pet.ScaleOverride is null ? 0 : 1;
            if (changedTarget || !_scale.ContainsFocus) _scale.Value = (decimal)(pet.ScaleOverride ?? Host.GlobalScale);
            if (changedTarget || !_jumpDesireMode.ContainsFocus) _jumpDesireMode.SelectedIndex = pet.BehaviorOverrides.JumpDesireOverride is null ? 0 : 1;
            if (changedTarget || !_jumpDesire.ContainsFocus) _jumpDesire.Value = (decimal)((pet.BehaviorOverrides.JumpDesireOverride ?? Host.JumpDesire) * 100f);
            if (changedTarget || !_climbDesireMode.ContainsFocus) _climbDesireMode.SelectedIndex = pet.BehaviorOverrides.ClimbDesireOverride is null ? 0 : 1;
            if (changedTarget || !_climbDesire.ContainsFocus) _climbDesire.Value = (decimal)((pet.BehaviorOverrides.ClimbDesireOverride ?? Host.ClimbDesire) * 100f);
            if (changedTarget || !_windowJumpDesireMode.ContainsFocus) _windowJumpDesireMode.SelectedIndex = pet.BehaviorOverrides.WindowJumpDesireOverride is null ? 0 : 1;
            if (changedTarget || !_windowJumpDesire.ContainsFocus) _windowJumpDesire.Value = (decimal)((pet.BehaviorOverrides.WindowJumpDesireOverride ?? Host.WindowJumpDesire) * 100f);
        }
        finally { _syncing = false; }
        RefreshSources(pet.BehaviorOverrides);
        RefreshScaleSource(pet.ScaleOverride);
    }

    private void RefreshSources(PetBehaviorOverrides overrides)
    {
        _walkSource.Text = InstanceSettingState.WalkSource(overrides, Host.DefaultWalkEnabled, Host.IsPaused);
        _dialogueSource.Text = InstanceSettingState.DialogueSource(overrides, Host.DefaultDialogueEnabled, Host.DialogueEnabled);
        _jumpDesireSource.Text = InstanceSettingState.JumpDesireSource(overrides, Host.JumpDesire);
        _climbDesireSource.Text = InstanceSettingState.ClimbDesireSource(overrides, Host.ClimbDesire);
        _windowJumpDesireSource.Text = InstanceSettingState.WindowJumpDesireSource(overrides, Host.WindowJumpDesire);
        _activityRate.Enabled = _editingId is not null && overrides.ActivityRateOverride is not null;
        _jumpDesire.Enabled = _editingId is not null && overrides.JumpDesireOverride is not null;
        _climbDesire.Enabled = _editingId is not null && overrides.ClimbDesireOverride is not null;
        _windowJumpDesire.Enabled = _editingId is not null && overrides.WindowJumpDesireOverride is not null;
    }

    private void RefreshScaleSource(float? scaleOverride)
    {
        _scaleSource.Text = InstanceSettingState.ScaleSource(scaleOverride, Host.GlobalScale);
        _scale.Enabled = _editingId is not null && scaleOverride is not null;
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
        RefreshSources(pet.BehaviorOverrides);
    }

    private void ApplyActivityOverride()
    {
        if (_syncing || _editingId is not { } id || Host.FindPet(id) is not { } pet) return;
        var current = pet.BehaviorOverrides;
        var overrideValue = _activityMode.SelectedIndex == 0 ? null : (float?)_activityRate.Value;
        Host.SetPetBehaviorOverrides(id, current with { ActivityRateOverride = overrideValue });
        RefreshSources(pet.BehaviorOverrides);
    }

    private void ApplyScaleOverride()
    {
        if (_syncing || _editingId is not { } id || Host.FindPet(id) is not { } pet) return;
        var overrideValue = _scaleMode.SelectedIndex == 0 ? null : (float?)_scale.Value;
        Host.SetPetScaleOverride(id, overrideValue);
        RefreshScaleSource(pet.ScaleOverride);
    }

    private void ApplyDesireOverride(bool isJump)
    {
        if (_syncing || _editingId is not { } id || Host.FindPet(id) is not { } pet) return;
        var current = pet.BehaviorOverrides;
        var mode = isJump ? _jumpDesireMode : _climbDesireMode;
        var editor = isJump ? _jumpDesire : _climbDesire;
        var overrideValue = mode.SelectedIndex == 0
            ? null
            : (float?)Math.Clamp((float)editor.Value / 100f, 0f, 1f);
        Host.SetPetBehaviorOverrides(id, isJump
            ? current with { JumpDesireOverride = overrideValue }
            : current with { ClimbDesireOverride = overrideValue });
        RefreshSources(pet.BehaviorOverrides);
    }

    private void ApplyWindowJumpDesireOverride()
    {
        if (_syncing || _editingId is not { } id || Host.FindPet(id) is not { } pet) return;
        var overrideValue = _windowJumpDesireMode.SelectedIndex == 0
            ? null
            : (float?)Math.Clamp((float)_windowJumpDesire.Value / 100f, 0f, 1f);
        Host.SetPetBehaviorOverrides(id, pet.BehaviorOverrides with { WindowJumpDesireOverride = overrideValue });
        RefreshSources(pet.BehaviorOverrides);
    }

    private static AntdUI.Select CreateOverrideCombo()
        => UiTheme.CreateSelect(150);
}
