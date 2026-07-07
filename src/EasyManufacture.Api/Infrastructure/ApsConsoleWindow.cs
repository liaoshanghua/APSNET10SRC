using System.Runtime.InteropServices;

namespace EasyManufacture.Api.Infrastructure;

internal static class ApsConsoleWindow
{
    private const int SwHide = 0;
    private const int SwShow = 5;

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public static void Hide()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var handle = GetConsoleWindow();
        if (handle != IntPtr.Zero)
            ShowWindow(handle, SwHide);
    }

    public static void Show()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var handle = GetConsoleWindow();
        if (handle != IntPtr.Zero)
            ShowWindow(handle, SwShow);
    }
}
