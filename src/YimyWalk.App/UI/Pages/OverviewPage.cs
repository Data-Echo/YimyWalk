using YimyWalk.App.Host;

namespace YimyWalk.App.UI.Pages;

internal sealed class OverviewPage : ManagerPage
{
    private readonly Label _active = UiTheme.CreateBodyLabel();
    private readonly Label _pending = UiTheme.CreateBodyLabel();
    private readonly Label _settings = UiTheme.CreateBodyLabel();
    private readonly Label _operation = UiTheme.CreateMutedLabel();
    private readonly Button _pause = UiTheme.CreateButton("暂停桌宠");

    public OverviewPage(AppHost host) : base(host)
    {
        var layout = CreateRootLayout();
        AddHeading(layout, "YimyWalk", "桌宠继续在桌面运行，主菜单负责管理当前状态和设置。");

        var summary = UiTheme.CreateCard();
        summary.Dock = DockStyle.Top;
        summary.Controls.Add(new Label { Text = "桌面状态", AutoSize = true, Font = UiTheme.SectionFont, ForeColor = UiTheme.Ink, Dock = DockStyle.Top });
        var status = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Padding = new Padding(0, UiTheme.Space12, 0, 0) };
        status.Controls.Add(_active);
        status.Controls.Add(_pending);
        status.Controls.Add(_settings);
        summary.Controls.Add(status);
        summary.Controls.Add(_operation);
        AddRow(layout, summary);

        var actions = UiTheme.CreateCard();
        actions.Dock = DockStyle.Top;
        actions.Controls.Add(new Label { Text = "快捷操作", AutoSize = true, Font = UiTheme.SectionFont, ForeColor = UiTheme.Ink, Dock = DockStyle.Top });
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, UiTheme.Space12, 0, 0) };
        foreach (var count in new[] { 1, 10, 50, 100 })
        {
            var button = UiTheme.CreateButton($"创建 {count}", primary: count == 1);
            button.AccessibleName = $"创建 {count} 只桌宠";
            button.Click += (_, _) => Host.QueueCreate(Host.SelectedRoleId, count);
            buttons.Controls.Add(button);
        }
        var clear = UiTheme.CreateButton("一键清空");
        clear.Click += (_, _) => Host.ClearPets();
        buttons.Controls.Add(clear);
        _pause.Click += (_, _) =>
        {
            if (Host.IsPaused) Host.ResumePets();
            else Host.PausePets();
            RefreshFromHost();
        };
        buttons.Controls.Add(_pause);
        var feed = UiTheme.CreateButton("喂食全部");
        feed.Click += (_, _) => Host.FeedAll();
        buttons.Controls.Add(feed);
        var food = UiTheme.CreateButton("生成虚拟食物");
        food.Click += (_, _) => Host.SpawnVirtualFood();
        buttons.Controls.Add(food);
        var hide = UiTheme.CreateButton("隐藏桌宠");
        hide.Click += (_, _) => Host.SetPetsVisible(false);
        buttons.Controls.Add(hide);
        var show = UiTheme.CreateButton("显示桌宠");
        show.Click += (_, _) => Host.SetPetsVisible(true);
        buttons.Controls.Add(show);
        var exit = UiTheme.CreateButton("退出 YimyWalk");
        exit.Click += (_, _) => Host.ExitApplication();
        buttons.Controls.Add(exit);
        actions.Controls.Add(buttons);
        AddRow(layout, actions);
    }

    public override string PageTitle => "总览";

    public override void RefreshFromHost()
    {
        _active.Text = $"活动实例：{Host.ActivePetCount}";
        _pending.Text = $"待创建：{Host.PendingCreationCount}";
        _settings.Text = $"全局设置保存状态：{Host.SettingsSaveState}";
        _pause.Text = Host.IsPaused ? "继续桌宠" : "暂停桌宠";
        _operation.Text = Host.PendingCreationCount > 0
            ? "创建队列正在处理；一键清空会先取消待创建，再删除当前实例。"
            : "单实例覆盖只在本次运行有效；总览的保存状态只描述全局设置。";
    }
}
