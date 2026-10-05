namespace YimyWalk.App.UI;

internal static class UiTheme
{
    public const int Space8 = 8;
    public const int Space12 = 12;
    public const int Space16 = 16;
    public const int Space24 = 24;
    public const int SidebarWidth = 200;

    public static readonly Color Ink = Color.FromArgb(53, 45, 66);
    public static readonly Color Cream = Color.FromArgb(255, 250, 242);
    public static readonly Color Yellow = Color.FromArgb(255, 209, 102);
    public static readonly Color Coral = Color.FromArgb(244, 126, 95);
    public static readonly Color Teal = Color.FromArgb(78, 205, 196);
    public static readonly Color Muted = Color.FromArgb(120, 109, 126);
    public static readonly Color Panel = Color.White;
    public static readonly Color Border = Color.FromArgb(230, 223, 232);

    public static Font TitleFont => new("Segoe UI", 22f, FontStyle.Bold);
    public static Font SectionFont => new("Segoe UI", 14f, FontStyle.Bold);
    public static Font BodyFont => new("Segoe UI", 10f, FontStyle.Regular);
    public static Font SmallFont => new("Segoe UI", 9f, FontStyle.Regular);
    public static Font NavFont => new("Segoe UI", 10f, FontStyle.Bold);

    public static Button CreateButton(string text, bool primary = false)
        => new()
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = primary ? 0 : 1 },
            BackColor = primary ? Teal : Color.White,
            ForeColor = Ink,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Padding = new Padding(12, 7, 12, 7),
            Margin = new Padding(4)
        };

    public static Button CreateNavButton(string text)
        => new()
        {
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false,
            Height = 44,
            Dock = DockStyle.Top,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            BackColor = Yellow,
            ForeColor = Ink,
            Font = NavFont,
            Padding = new Padding(Space16, 0, Space8, 0),
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

    public static Panel CreateCard()
        => new()
        {
            BackColor = Panel,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(16),
            Margin = new Padding(0, 0, 12, 12)
        };

    public static void StylePage(Control page)
    {
        page.BackColor = Cream;
        page.ForeColor = Ink;
        page.Font = BodyFont;
    }
}
