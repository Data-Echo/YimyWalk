namespace YimyWalk.App.Windows;

internal sealed class FoodWindow : Form
{
    private float _scale = 1f;

    public FoodWindow(Point location)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = false;
        ClientSize = new Size(20, 20);
        Location = location;
        BackColor = Color.LemonChiffon;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE;
            return parameters;
        }
    }

    public void UpdateTarget(Point location, float scale)
    {
        _scale = Math.Clamp(scale, 0f, 1f);
        Location = location;
        Invalidate();
        if (_scale <= 0) Hide();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var size = Math.Max(1, (int)Math.Round(16 * _scale));
        var rect = new Rectangle((ClientSize.Width - size) / 2, (ClientSize.Height - size) / 2, size, size);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(Color.Gold);
        using var outline = new Pen(Color.DarkOrange, 2);
        e.Graphics.FillEllipse(brush, rect);
        e.Graphics.DrawEllipse(outline, rect);
    }
}
