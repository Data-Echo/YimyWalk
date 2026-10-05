using YimyWalk.App.Host;
using YimyWalk.App.UI.Controls;

namespace YimyWalk.App.UI.Pages;

internal sealed class CharactersPage : ManagerPage
{
    private readonly ComboBox _characters = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 280 };
    private readonly Label _roleId = UiTheme.CreateMutedLabel();
    private readonly Label _roleInfo = UiTheme.CreateBodyLabel();
    private readonly Label _reloadStatus = UiTheme.CreateBodyLabel();
    private readonly CharacterFramePreview _preview;
    private readonly Button _setDefault = UiTheme.CreateButton("设为下次创建角色", primary: true);
    private bool _synchronizing;

    public CharactersPage(AppHost host) : base(host)
    {
        _preview = new CharacterFramePreview(host.SurfaceCache) { Dock = DockStyle.Fill, MinimumSize = new Size(180, 180) };
        var layout = CreateRootLayout();
        AddHeading(layout, "角色", "管理角色包和下次创建的默认角色。查看角色不会改变当前运行实例的选择。");

        var content = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, MinimumSize = new Size(0, 260), ColumnCount = 2, RowCount = 1 };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        var card = UiTheme.CreateCard();
        card.Dock = DockStyle.Fill;
        card.Controls.Add(new Label { Text = "下次创建默认角色", AutoSize = true, Font = UiTheme.SectionFont, Dock = DockStyle.Top });
        var row = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, UiTheme.Space12, 0, 0) };
        row.Controls.Add(_characters);
        var refresh = UiTheme.CreateButton("刷新角色");
        refresh.Click += (_, _) => ReloadCharacters();
        row.Controls.Add(refresh);
        _setDefault.Click += (_, _) =>
        {
            if (_characters.SelectedItem is RoleChoice role)
            {
                Host.SetSelectedRole(role.Id);
                _reloadStatus.Text = $"已将“{role.DisplayName}”设为下次创建默认角色。";
                ShowSelectedRole();
            }
        };
        row.Controls.Add(_setDefault);
        card.Controls.Add(row);
        _roleId.Dock = DockStyle.Top;
        _roleInfo.Dock = DockStyle.Top;
        _roleInfo.MaximumSize = new Size(520, 0);
        card.Controls.Add(_roleInfo);
        card.Controls.Add(_roleId);
        card.Controls.Add(_reloadStatus);
        content.Controls.Add(card, 0, 0);

        var previewCard = UiTheme.CreateCard();
        previewCard.Dock = DockStyle.Fill;
        previewCard.Controls.Add(_preview);
        content.Controls.Add(previewCard, 1, 0);
        AddRow(layout, content);

        _characters.SelectedIndexChanged += (_, _) =>
        {
            if (_synchronizing || _characters.SelectedItem is not RoleChoice role) return;
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
            _characters.Items.Clear();
            foreach (var definition in Host.Catalog.Definitions.Values.OrderBy(definition => definition.Id))
                _characters.Items.Add(new RoleChoice(definition.Id, definition.DisplayName));
            for (var index = 0; index < _characters.Items.Count; index++)
                if (_characters.Items[index] is RoleChoice role && role.Id.Equals(current, StringComparison.OrdinalIgnoreCase))
                {
                    _characters.SelectedIndex = index;
                    break;
                }
            if (_characters.SelectedIndex < 0 && _characters.Items.Count > 0)
                _characters.SelectedIndex = 0;
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
        if (_characters.SelectedItem is not RoleChoice role || !Host.Catalog.TryGet(role.Id, out var definition))
        {
            _roleId.Text = "没有可用角色定义";
            _roleInfo.Text = string.Empty;
            _setDefault.Enabled = false;
            _preview.ShowIdleFrame(null);
            return;
        }

        _roleId.Text = $"角色 ID：{definition.Id}";
        var isDefault = definition.Id.Equals(Host.SelectedRoleId, StringComparison.OrdinalIgnoreCase);
        _roleInfo.Text = $"{(isDefault ? "当前为下次创建默认角色。" : "预览角色，不会改变下次创建默认值。")}{Environment.NewLine}角色基础行为：自主走动速度 {definition.Movement.WalkSpeed:0.#} px/s；活跃度倍率在互动页统一设置。";
        _setDefault.Enabled = !isDefault;
        _preview.ShowIdleFrame(definition);
    }

    private bool NeedsCharacterRefresh()
    {
        var ids = Host.Catalog.Definitions.Keys.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
        var current = _characters.Items.Cast<RoleChoice>().Select(role => role.Id).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
        return !ids.SequenceEqual(current, StringComparer.OrdinalIgnoreCase);
    }

    private sealed record RoleChoice(string Id, string DisplayName)
    {
        public override string ToString() => $"{Id} — {DisplayName}";
    }
}
