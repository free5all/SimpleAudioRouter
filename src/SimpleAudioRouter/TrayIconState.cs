using System.Drawing;
using System.Drawing.Drawing2D;

namespace SimpleAudioRouter;

internal enum TrayIconState
{
    Idle,
    Routing,
    Warning,
    Error,
}
