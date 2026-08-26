using System.Runtime.InteropServices;

namespace AutoClickerIA.Services;

public static class PhysicalMouseListener
{
    private const int WhMouseLl = 14;
    private const int WmLeftButtonDown = 0x0201;
    private const int WmLeftButtonUp = 0x0202;
    private const int WmRightButtonDown = 0x0204;
    private const int WmRightButtonUp = 0x0205;

    private const uint LlmhfInjected = 0x00000001;
    private const uint LlmhfLowerIlInjected = 0x00000002;

    private static volatile bool _leftPressed;
    private static volatile bool _rightPressed;

    private static readonly LowLevelMouseProc Callback = HookProcedure;
    private static readonly IntPtr HookHandle;

    static PhysicalMouseListener()
    {
        HookHandle = SetWindowsHookEx(
            WhMouseLl,
            Callback,
            GetModuleHandle(null),
            0);
    }

    public static void Initialize() => _ = HookHandle;

    public static bool IsPressed(int virtualKey)
    {
        return virtualKey switch
        {
            InputListener.VkLeftMouse => _leftPressed,
            InputListener.VkRightMouse => _rightPressed,
            _ => false
        };
    }

    private static IntPtr HookProcedure(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0)
        {
            MouseHookData data = Marshal.PtrToStructure<MouseHookData>(lParam);
            bool injected =
                (data.Flags & LlmhfInjected) != 0 ||
                (data.Flags & LlmhfLowerIlInjected) != 0;

            if (!injected)
            {
                switch (wParam.ToInt32())
                {
                    case WmLeftButtonDown:
                        _leftPressed = true;
                        break;
                    case WmLeftButtonUp:
                        _leftPressed = false;
                        break;
                    case WmRightButtonDown:
                        _rightPressed = true;
                        break;
                    case WmRightButtonUp:
                        _rightPressed = false;
                        break;
                }
            }
        }

        return CallNextHookEx(HookHandle, code, wParam, lParam);
    }

    private delegate IntPtr LowLevelMouseProc(int code, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseHookData
    {
        public Point Point;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public nuint ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
        int hookId,
        LowLevelMouseProc callback,
        IntPtr moduleHandle,
        uint threadId);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(
        IntPtr hookHandle,
        int code,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(string? moduleName);
}
