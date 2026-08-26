using System.Diagnostics;

namespace AutoClickerIA.Utils;

public static class HighPrecisionTimer
{
    public static void Sleep(double milliseconds)
    {
        if (milliseconds <= 0)
            return;

        Stopwatch stopwatch = Stopwatch.StartNew();

        if (milliseconds >= 14)
        {
            int sleepTime = Math.Max(1, (int)milliseconds - 8);
            Thread.Sleep(sleepTime);
        }

        while (stopwatch.Elapsed.TotalMilliseconds < milliseconds)
            Thread.SpinWait(20);
    }
}
