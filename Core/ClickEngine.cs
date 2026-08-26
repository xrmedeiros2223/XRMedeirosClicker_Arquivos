using AutoClickerIA.Config;
using AutoClickerIA.Utils;

namespace AutoClickerIA.Core;

public sealed class ClickEngine
{
    private readonly object _sync = new();
    private readonly Random _random = new();

    private Thread? _clickThread;
    private volatile bool _running;
    private volatile int _currentCps;

    public AppSettings Settings { get; set; } = new();

    public bool IsRunning => _running;
    public int CurrentCps => _running ? _currentCps : 0;

    public void Start()
    {
        lock (_sync)
        {
            if (_running)
                return;

            _running = true;
            _clickThread = new Thread(ClickLoop)
            {
                IsBackground = true,
                Name = "XRMedeirosClickThread"
            };
            _clickThread.Start();
        }
    }

    public void Stop()
    {
        _running = false;
        _currentCps = 0;
    }

    private void ClickLoop()
    {
        while (_running)
        {
            int minCps = Math.Clamp(Settings.MinCps, 1, 30);
            int maxCps = Math.Clamp(Settings.MaxCps, minCps, 30);

            int selectedCps;
            lock (_random)
                selectedCps = _random.Next(minCps, maxCps + 1);

            _currentCps = selectedCps;

            MouseSimulator.Click(Settings.IsLeftClick);
            HighPrecisionTimer.Sleep(1000.0 / selectedCps);
        }

        _currentCps = 0;
    }
}
