using System.Diagnostics;

namespace CyberMon;

public sealed class ProcessCollector : ICollector<ProcessData[]>
{
    private readonly Dictionary<int, (DateTime time, TimeSpan cpu)> _prevCpu = new();
    private readonly int _processorCount = Environment.ProcessorCount;

    public Task<ProcessData[]> CollectAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var current = new Dictionary<int, (DateTime time, TimeSpan cpu)>();
        var results = new List<ProcessData>();

        try
        {
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    int pid = proc.Id;
                    if (pid == 0) continue;

                    var cpuTime = proc.TotalProcessorTime;
                    float cpuPct = 0f;

                    if (_prevCpu.TryGetValue(pid, out var prev))
                    {
                        double elapsed = (now - prev.time).TotalMilliseconds;
                        if (elapsed > 0)
                        {
                            double cpuMs = (cpuTime - prev.cpu).TotalMilliseconds;
                            cpuPct = (float)(cpuMs / (elapsed * _processorCount) * 100.0);
                            cpuPct = Math.Clamp(cpuPct, 0f, 100f);
                        }
                    }

                    current[pid] = (now, cpuTime);
                    string name = proc.ProcessName;
                    if (name.Length > 20) name = name[..18] + "..";

                    results.Add(new ProcessData
                    {
                        Name = name,
                        Pid = pid,
                        CpuPercent = cpuPct,
                        MemoryBytes = proc.WorkingSet64
                    });
                }
                catch { }
                finally { proc.Dispose(); }
            }
        }
        catch { }

        _prevCpu.Clear();
        foreach (var kv in current)
            _prevCpu[kv.Key] = kv.Value;

        var top5 = results
            .OrderByDescending(p => p.CpuPercent)
            .ThenByDescending(p => p.MemoryBytes)
            .Take(5)
            .ToArray();

        return Task.FromResult(top5);
    }
}
