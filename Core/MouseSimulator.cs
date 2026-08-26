using System.Runtime.InteropServices;

namespace AutoClickerIA.Core;

public static class MouseSimulator
{
    private const uint LeftDown = 0x0002;
    private const uint LeftUp = 0x0004;
    private const uint RightDown = 0x0008;
    private const uint RightUp = 0x0010;

    [DllImport("user32.dll")]
    private static extern void mouse_event(
        uint dwFlags,
        uint dx,
        uint dy,
        uint dwData,
        nuint dwExtraInfo);

    public static void Click(bool leftButton)
    {
        if (leftButton)
        {
            mouse_event(LeftDown, 0, 0, 0, 0);
            mouse_event(LeftUp, 0, 0, 0, 0);
        }
        else
        {
            mouse_event(RightDown, 0, 0, 0, 0);
            mouse_event(RightUp, 0, 0, 0, 0);
        }
    }
}
