namespace YimyWalk.App.UI;

internal static class UiTheme
{
    public const int Space8 = 8;
    public const int Space12 = 12;
    public const int Space16 = 16;
    public const int Space24 = 24;
    public const int SidebarWidth = 200;

    public static readonly Color Ink = Color.FromArgb(32, 37, 52);
    public static readonly Color Cream = Color.FromArgb(246, 247, 251);
    public static readonly Color Sidebar = Color.FromArgb(255, 244, 214);
    public static readonly Color Yellow = Sidebar;
    public static readonly Color Coral = Color.FromArgb(255, 224, 213);
    public static readonly Color Teal = Color.FromArgb(19, 124, 116);
    public static readonly Color Muted = Color.FromArgb(89, 101, 121);
    public static readonly Color Panel = Color.White;
    public static readonly Color Border = Color.FromArgb(228, 232, 239);
    public static readonly Color NavInk = Color.FromArgb(142, 60, 40);

    private static readonly string UiFont = FontFamily.Families.Any(f => f.Name.Equals("Microsoft YaHei UI", StringComparison.OrdinalIgnoreCase))
        ? "Microsoft YaHei UI" : "Segoe UI";
    public static Font TitleFont => new(UiFont, 22f, FontStyle.Bold);
    public static Font BrandFont => new(UiFont, 16f, FontStyle.Bold);
    public static Font SectionFont => new(UiFont, 14f, FontStyle.Bold);
    public static Font BodyFont => new(UiFont, 10f, FontStyle.Regular);
    public static Font SmallFont => new(UiFont, 9f, FontStyle.Regular);
    public static Font NavFont => new(UiFont, 10f, FontStyle.Bold);

    public static void ConfigureAntd()
    {
        AntdUI.Config.Mode = AntdUI.TMode.Light;
        AntdUI.Style.SetPrimary(Teal);
        AntdUI.Config.Animation = true;
    }

    public static AntdUI.Button CreateButton(string text, bool primary = false)
        => new()
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AntdUI.TAutoSize.Auto,
            Type = primary ? AntdUI.TTypeMini.Primary : AntdUI.TTypeMini.Default,
            DefaultBack = primary ? Teal : Color.White,
            DefaultBorderColor = primary ? Teal : Border,
            ForeColor = Ink,
            Font = BodyFont,
            Padding = new Padding(12, 8, 12, 8),
            Radius = 8,
            Margin = new Padding(4),
            TabStop = true
        };

    public static AntdUI.Button CreateNavButton(string text)
        => new()
        {
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false,
            Height = 44,
            Dock = DockStyle.Top,
            Type = AntdUI.TTypeMini.Default,
            DefaultBack = Sidebar,
            DefaultBorderColor = Color.Transparent,
            ForeColor = Ink,
            Font = NavFont,
            Padding = new Padding(Space16, 0, Space8, 0),
            Radius = 8,
            Margin = Padding.Empty,
            TabStop = true
        };

    public static Label CreateBodyLabel(string text = "")
        => new()
        {
            Text = text,
            AutoSize = true,
            Font = BodyFont,
            ForeColor = Ink,
            Margin = new Padding(0, 3, 8, 3)
        };

    public static Label CreateMutedLabel(string text = "")
        => new()
        {
            Text = text,
            AutoSize = true,
            Font = SmallFont,
            ForeColor = Muted,
            Margin = new Padding(0, 3, Space8, 3)
        };

    public static AntdUI.Panel CreateCard()
        => new()
        {
            BackColor = Panel,
            BorderColor = Border,
            BorderWidth = 1,
            Radius = 12,
            InnerPadding = new Padding(20),
            Margin = new Padding(0, 0, 16, 16)
        };

    public static AntdUI.Switch CreateSwitch(bool isChecked = false)
        => new()
        {
            Checked = isChecked,
            AutoSize = false,
            Size = new Size(72, 28),
            Fill = Teal,
            CheckedText = "开",
            UnCheckedText = "关",
            ForeColor = Color.White,
            Font = SmallFont,
            TabStop = true,
            Margin = new Padding(4, 4, 0, 4)
        };

    public static AntdUI.Select CreateSelect(int width = 220)
        => new()
        {
            Width = width,
            Height = 36,
            DropDownArrow = true,
            Radius = 8,
            TabStop = true,
            Margin = new Padding(4, 2, 0, 2)
        };

    public static AntdUI.InputNumber CreateInputNumber(decimal minimum, decimal maximum, decimal increment, int decimals, int width = 120)
        => new()
        {
            Minimum = minimum,
            Maximum = maximum,
            Increment = increment,
            DecimalPlaces = decimals,
            Width = width,
            Height = 36,
            AlwaysShowControl = true,
            Radius = 8,
            TabStop = true,
            Margin = new Padding(4, 2, 0, 2)
        };

    public static Control WrapSwitch(Control editor)
    {
        if (editor is not AntdUI.Switch toggle) return editor;
        var state = CreateMutedLabel(toggle.Checked ? "开启" : "关闭");
        state.Margin = new Padding(4, 7, 0, 3);
        toggle.CheckedChanged += (_, e) => state.Text = e.Value ? "开启" : "关闭";
        var row = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        row.Controls.Add(toggle);
        row.Controls.Add(state);
        return row;
    }

    public static void StylePage(Control page)
    {
        page.BackColor = Cream;
        page.ForeColor = Ink;
        page.Font = BodyFont;
    }
}
