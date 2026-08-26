using System.Runtime.InteropServices;
using System.Windows.Input;

namespace AutoClickerIA.Services;

public static class InputListener
{
    public const int VkLeftMouse = 0x01;
    public const int VkRightMouse = 0x02;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);

    public static bool IsKeyboardPressed(int virtualKey)
    {
        return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
    }

    public static string GetInputName(int virtualKey)
    {
        return virtualKey switch
        {
            VkLeftMouse => "MOUSE LEFT",
            VkRightMouse => "MOUSE RIGHT",
            _ => GetKeyboardName(virtualKey)
        };
    }

    private static string GetKeyboardName(int virtualKey)
    {
        Key key = KeyInterop.KeyFromVirtualKey(virtualKey);
        return key == Key.None ? $"KEY {virtualKey}" : key.ToString().ToUpperInvariant();
    }
}
