using YimyWalk.App.Host;

namespace YimyWalk.App.UI;

internal enum ManagerPageId
{
    Overview,
    Pets,
    Characters,
    Interactions,
    Settings
}

internal abstract class ManagerPage : UserControl
{
    protected ManagerPage(AppHost host)
    {
        Host = host;
        UiTheme.StylePage(this);
        Dock = DockStyle.Fill;
        AutoScroll = true;
        Padding = new Padding(UiTheme.Space24);
        TabStop = true;
    }

    protected AppHost Host { get; }

    public abstract string PageTitle { get; }

    /// <summary>Refreshes values owned by AppHost without rebuilding the page.</summary>
    public virtual void RefreshFromHost() { }

    protected TableLayoutPanel CreateRootLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 0,
            Padding = Padding.Empty,
            GrowStyle = TableLayoutPanelGrowStyle.AddRows
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(layout);
        return layout;
    }

    protected Panel AddHeading(TableLayoutPanel layout, string title, string subtitle)
    {
        var heading = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, UiTheme.Space24),
            MinimumSize = new Size(0, 76)
        };
        var titleLabel = new Label
        {
            Text = title,
            AutoSize = true,
            Font = UiTheme.TitleFont,
            ForeColor = UiTheme.Ink,
            Location = new Point(0, 0)
        };
        var subtitleLabel = new Label
        {
            Text = subtitle,
            AutoSize = true,
            MaximumSize = new Size(760, 0),
            Font = UiTheme.BodyFont,
            ForeColor = UiTheme.Muted,
            Location = new Point(2, 0)
        };
        var titleHeight = titleLabel.GetPreferredSize(Size.Empty).Height;
        subtitleLabel.Location = new Point(2, titleHeight + UiTheme.Space8);
        var subtitleHeight = subtitleLabel.GetPreferredSize(new Size(760, 0)).Height;
        heading.MinimumSize = new Size(0, Math.Max(76, subtitleLabel.Top + subtitleHeight + UiTheme.Space8));
        heading.Controls.Add(subtitleLabel);
        heading.Controls.Add(titleLabel);
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(heading, 0, layout.RowCount++);
        return heading;
    }

    protected void AddRow(TableLayoutPanel layout, Control control)
    {
        control.Dock = DockStyle.Top;
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(control, 0, layout.RowCount++);
    }
}
