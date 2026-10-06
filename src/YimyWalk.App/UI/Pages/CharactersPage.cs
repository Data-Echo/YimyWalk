using YimyWalk.App.Host;
using YimyWalk.App.UI.Controls;

namespace YimyWalk.App.UI.Pages;

internal sealed class CharactersPage : ManagerPage
{
    private readonly ListView _characters = new()
    {
        View = View.Details,
        FullRowSelect = true,
        HideSelection = false,
        MultiSelect = false,
        HeaderStyle = ColumnHeaderStyle.Nonclickable,
        BorderStyle = BorderStyle.FixedSingle,
        Dock = DockStyle.Fill,
        TabStop = true
    };
    private readonly Label _roleName = UiTheme.CreateBodyLabel();
    private readonly Label _roleId = UiTheme.CreateMutedLabel();
    private readonly Label _roleInfo = UiTheme.CreateBodyLabel();
    private readonly Label _reloadStatus = UiTheme.CreateMutedLabel();
    private readonly CharacterFramePreview _preview;
    private readonly AntdUI.Button _refresh = UiTheme.CreateButton("刷新角色");
    private readonly AntdUI.Button _setDefault = UiTheme.CreateButton("设为默认角色", primary: true);
    private bool _synchronizing;

    public CharactersPage(AppHost host) : base(host)
    {
        _preview = new CharacterFramePreview(host.SurfaceCache) { Dock = DockStyle.Fill, MinimumSize = new Size(220, 220) };
        _characters.Columns.Add("名称", 100);
        _characters.Columns.Add("角色 ID", 80);
        _characters.Columns.Add("状态", 50);
        _characters.Columns.Add("默认", 50);

        var layout = CreateRootLayout();
        AddHeading(layout, "角色", "管理角色包和下次创建的默认角色。查看角色不会改变当前运行实例的选择。");

        var content = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, MinimumSize = new Size(0, 430), ColumnCount = 2, RowCount = 1 };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));

        var listCard = UiTheme.CreateCard();
        listCard.Dock = DockStyle.Fill;
        listCard.MinimumSize = new Size(0, 430);
        var listTitle = new Label { Text = "角色列表", AutoSize = true, Font = UiTheme.SectionFont, ForeColor = UiTheme.Ink, Dock = DockStyle.Top };
        listCard.Controls.Add(_characters);
        listCard.Controls.Add(listTitle);
        content.Controls.Add(listCard, 0, 0);

        var detailCard = UiTheme.CreateCard();
        detailCard.Dock = DockStyle.Fill;
        detailCard.MinimumSize = new Size(0, 430);
        detailCard.AutoScroll = true;
        var details = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, Padding = Padding.Empty, Margin = Padding.Empty };
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Padding = new Padding(0, UiTheme.Space8, 0, 0) };
        _refresh.Click += (_, _) => ReloadCharacters();
        _setDefault.Click += (_, _) =>
        {
            if (_characters.SelectedItems.Count == 0 || _characters.SelectedItems[0].Tag is not RoleChoice role) return;
            Host.SetSelectedRole(role.Id);
            _reloadStatus.Text = $"已将“{role.DisplayName}”设为下次创建默认角色。";
            RefreshCharacters();
            ShowSelectedRole();
        };
        actions.Controls.Add(_refresh);
        actions.Controls.Add(_setDefault);
        AddDetailRow(details, actions);
        var previewRow = new Panel { Dock = DockStyle.Top, Height = 210, Padding = new Padding(0, UiTheme.Space8, 0, UiTheme.Space8) };
        previewRow.Controls.Add(_preview);
        AddDetailRow(details, previewRow);
        _roleName.Font = UiTheme.SectionFont;
        AddDetailRow(details, _roleName);
        AddDetailRow(details, _roleId);
        _roleInfo.MaximumSize = new Size(430, 0);
        AddDetailRow(details, _roleInfo);
        AddDetailRow(details, _reloadStatus);
        detailCard.Controls.Add(details);
        content.Controls.Add(detailCard, 1, 0);
        AddRow(layout, content);

        _characters.SelectedIndexChanged += (_, _) =>
        {
            if (_synchronizing) return;
            ShowSelectedRole();
        };
    }

    public override string PageTitle => "角色";

    public override void RefreshFromHost()
    {
        if (NeedsCharacterRefresh()) RefreshCharacters();
        ShowSelectedRole();
    }

    public void RefreshCharacters()
    {
        _synchronizing = true;
        try
        {
            var current = Host.SelectedRoleId;
            _characters.BeginUpdate();
            try
            {
                _characters.Items.Clear();
                foreach (var definition in Host.Catalog.Definitions.Values.OrderBy(definition => definition.Id))
                {
                    var choice = new RoleChoice(definition.Id, definition.DisplayName);
                    var row = new ListViewItem(choice.DisplayName) { Tag = choice };
                    row.SubItems.Add(choice.Id);
                    row.SubItems.Add("有效");
                    row.SubItems.Add(choice.Id.Equals(current, StringComparison.OrdinalIgnoreCase) ? "当前" : string.Empty);
                    _characters.Items.Add(row);
                }
                var selected = _characters.Items.Cast<ListViewItem>().FirstOrDefault(item => item.Tag is RoleChoice role && role.Id.Equals(current, StringComparison.OrdinalIgnoreCase));
                if (selected is not null) selected.Selected = true;
                else if (_characters.Items.Count > 0) _characters.Items[0].Selected = true;
            }
            finally { _characters.EndUpdate(); }
        }
        finally { _synchronizing = false; }
    }

    public void SetReloadStatus(int loadedCount, int failureCount)
        => _reloadStatus.Text = failureCount == 0
            ? $"已加载 {loadedCount} 个角色定义。"
            : $"已保留可用角色；本次刷新失败 {failureCount} 个。";

    private void ReloadCharacters()
    {
        try
        {
            var failures = Host.ReloadCharacters();
            SetReloadStatus(Host.Catalog.Definitions.Count, failures.Count);
            RefreshCharacters();
            ShowSelectedRole();
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException)
        {
            _reloadStatus.Text = $"刷新失败：{exception.Message}";
        }
    }

    private void ShowSelectedRole()
    {
        var role = _characters.SelectedItems.Count > 0 ? _characters.SelectedItems[0].Tag as RoleChoice : null;
        if (role is null || !Host.Catalog.TryGet(role.Id, out var definition))
        {
            _roleName.Text = "没有可用角色定义";
            _roleId.Text = string.Empty;
            _roleInfo.Text = string.Empty;
            _setDefault.Enabled = false;
            _preview.ShowIdleFrame(null);
            return;
        }

        _roleName.Text = definition.DisplayName;
        _roleId.Text = $"角色 ID：{definition.Id} · 有效定义";
        var isDefault = definition.Id.Equals(Host.SelectedRoleId, StringComparison.OrdinalIgnoreCase);
        _roleInfo.Text = $"{(isDefault ? "当前为下次创建默认角色。" : "预览角色，不会改变下次创建默认值。")}{Environment.NewLine}角色基础行为：自主走动速度 {definition.Movement.WalkSpeed:0.#} px/s；活跃度倍率在互动页统一设置。";
        _setDefault.Text = isDefault ? "当前默认" : "设为默认角色";
        _setDefault.Enabled = !isDefault;
        _preview.ShowIdleFrame(definition);
    }

    private bool NeedsCharacterRefresh()
    {
        var ids = Host.Catalog.Definitions.Keys.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
        var current = _characters.Items.Cast<ListViewItem>()
            .Select(item => item.Tag as RoleChoice)
            .Where(role => role is not null)
            .Select(role => role!.Id)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return !ids.SequenceEqual(current, StringComparer.OrdinalIgnoreCase);
    }

    private static void AddDetailRow(TableLayoutPanel details, Control control)
    {
        control.Margin = new Padding(0, 4, 0, 4);
        details.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        details.Controls.Add(control, 0, details.RowCount++);
    }

    private sealed record RoleChoice(string Id, string DisplayName);
}
