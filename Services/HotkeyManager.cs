using System.Windows.Threading;

namespace AutoClickerIA.Services;

public sealed class HotkeyManager : IDisposable
{
    private readonly DispatcherTimer _timer;

    private int _virtualKey;
    private bool _isMouseInput;
    private bool _holdMode;
    private bool _wasPressed;

    public event Action? ToggleRequested;
    public event Action<bool>? HoldStateChanged;

    public HotkeyManager(int virtualKey, bool isMouseInput, bool holdMode)
    {
        _virtualKey = virtualKey;
        _isMouseInput = isMouseInput;
        _holdMode = holdMode;

        PhysicalMouseListener.Initialize();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(5)
        };
        _timer.Tick += TimerTick;
    }

    public void Start()
    {
        _wasPressed = IsPressed();
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
        _wasPressed = false;
    }

    public void UpdateBinding(int virtualKey, bool isMouseInput, bool holdMode)
    {
        _virtualKey = virtualKey;
        _isMouseInput = isMouseInput;
        _holdMode = holdMode;
        _wasPressed = IsPressed();
    }

    private void TimerTick(object? sender, EventArgs e)
    {
        bool pressed = IsPressed();

        if (_holdMode)
        {
            if (pressed != _wasPressed)
                HoldStateChanged?.Invoke(pressed);
        }
        else if (pressed && !_wasPressed)
        {
            ToggleRequested?.Invoke();
        }

        _wasPressed = pressed;
    }

    private bool IsPressed()
    {
        return _isMouseInput
            ? PhysicalMouseListener.IsPressed(_virtualKey)
            : InputListener.IsKeyboardPressed(_virtualKey);
    }

    public void Dispose()
    {
        Stop();
        _timer.Tick -= TimerTick;
    }
}
