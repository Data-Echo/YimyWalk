using System.Drawing;
using YimyWalk.App.Windows;

namespace YimyWalk.App.Rendering;

internal static class LayeredWindowRenderer
{
    public static void Present(IntPtr hwnd, Bitmap bitmap, Point screenLocation)
    {
        var screenDc = NativeMethods.GetDC(IntPtr.Zero);
        var memoryDc = NativeMethods.CreateCompatibleDC(screenDc);
        var bitmapHandle = bitmap.GetHbitmap(Color.FromArgb(0));
        var previous = NativeMethods.SelectObject(memoryDc, bitmapHandle);
        try
        {
            var destination = new NativeMethods.Point(screenLocation.X, screenLocation.Y);
            var source = new NativeMethods.Point(0, 0);
            var size = new NativeMethods.Size(bitmap.Width, bitmap.Height);
            var blend = new NativeMethods.BlendFunction
            {
                BlendOp = NativeMethods.AC_SRC_OVER,
                BlendFlags = 0,
                SourceConstantAlpha = 255,
                AlphaFormat = NativeMethods.AC_SRC_ALPHA
            };
            NativeMethods.UpdateLayeredWindow(hwnd, screenDc, ref destination, ref size, memoryDc, ref source, 0, ref blend, NativeMethods.ULW_ALPHA);
        }
        finally
        {
            NativeMethods.SelectObject(memoryDc, previous);
            NativeMethods.DeleteObject(bitmapHandle);
            NativeMethods.DeleteDC(memoryDc);
            NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
        }
    }
}
