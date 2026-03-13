namespace CyberMon;

public static class Program
{
    public static async Task Main(string[] args)
    {
        EnableVirtualTerminal();

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        using var cpuCollector = new CpuCollector();
        var memCollector = new MemoryCollector();
        using var diskCollector = new DiskCollector();
        var netCollector = new NetworkCollector();
        var procCollector = new ProcessCollector();

        var renderer = new TuiRenderer();
        renderer.Init();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            await Task.Delay(1100, ct);

            while (!ct.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key is ConsoleKey.Q or ConsoleKey.Escape)
                        break;
                }

                var cpuTask = cpuCollector.CollectAsync(ct);
                var memTask = memCollector.CollectAsync(ct);
                var diskTask = diskCollector.CollectAsync(ct);
                var netTask = netCollector.CollectAsync(ct);
                var procTask = procCollector.CollectAsync(ct);

                await Task.WhenAll(cpuTask, memTask, diskTask, netTask, procTask);

                var snapshot = new SystemSnapshot
                {
                    Cpu = cpuTask.Result,
                    Memory = memTask.Result,
                    Disk = diskTask.Result,
                    Network = netTask.Result,
                    TopProcesses = procTask.Result,
                    Uptime = TimeSpan.FromMilliseconds(Environment.TickCount64),
                    Timestamp = DateTime.Now
                };

                renderer.Render(snapshot);
                await Task.Delay(1000, ct);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            renderer.Cleanup();
            Console.Clear();
            Console.WriteLine(Theme.Green + "CyberMon terminated." + Theme.Reset);
        }
    }

    private static void EnableVirtualTerminal()
    {
        try
        {
            var handle = GetStdHandle(-11);
            GetConsoleMode(handle, out uint mode);
            SetConsoleMode(handle, mode | 0x0004);
        }
        catch { }
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern nint GetStdHandle(int nStdHandle);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool GetConsoleMode(nint hConsoleHandle, out uint lpMode);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool SetConsoleMode(nint hConsoleHandle, uint dwMode);
}
