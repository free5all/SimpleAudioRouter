using System.Runtime.InteropServices;

namespace SimpleAudioRouter;

internal static class AppUserModelHelper
{
    public const string AppId = "SimpleAudioRouter.App";

    [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);

    public static void Register()
    {
        try
        {
            SetCurrentProcessExplicitAppUserModelID(AppId);
        }
        catch
        {
            // Non-fatal — notifications may fall back to the process name.
        }
    }
}
