using YimyWalk.App.Host;

namespace YimyWalk.App.Windows;

internal sealed class BubbleWindow : Form
{
    private readonly Label _label;

    public BubbleWindow(string text, Point anchor)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        AutoSize = true;
        BackColor = Color.LemonChiffon;
        Padding = new Padding(7, 4, 7, 4);
        _label = new Label { AutoSize = true, MaximumSize = new Size(240, 0), Text = text, ForeColor = Color.Black };
        Controls.Add(_label);
        Location = anchor;
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

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == NativeMethods.WM_MOUSEACTIVATE || message.Msg == NativeMethods.WM_NCHITTEST)
        {
            message.Result = message.Msg == NativeMethods.WM_MOUSEACTIVATE
                ? (IntPtr)NativeMethods.MA_NOACTIVATE
                : (IntPtr)NativeMethods.HTTRANSPARENT;
            return;
        }
        base.WndProc(ref message);
    }

    public void MoveAbove(Rectangle petBounds)
        => Location = new Point(petBounds.Left, Math.Max(0, petBounds.Top - Height - 4));
}
