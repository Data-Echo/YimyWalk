using YimyWalk.App.Host;

namespace YimyWalk.App.UI;

internal sealed class TrayIconHost : IDisposable
{
    private readonly AppHost _host;
    private readonly NotifyIcon _icon;

    public TrayIconHost(AppHost host)
    {
        _host = host;
        var menu = new ContextMenuStrip();
        menu.Items.Add("打开管理器", null, (_, _) => _host.ShowManager());
        menu.Items.Add("清空宠物", null, (_, _) => _host.ClearPets());
        menu.Items.Add("隐藏宠物", null, (_, _) => _host.SetPetsVisible(false));
        menu.Items.Add("显示宠物", null, (_, _) => _host.SetPetsVisible(true));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => _host.ExitApplication());
        _icon = new NotifyIcon { Icon = SystemIcons.Application, Text = "YimyWalk", ContextMenuStrip = menu, Visible = true };
        _icon.DoubleClick += (_, _) => _host.ShowManager();
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
