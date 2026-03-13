using System.Diagnostics;

namespace CyberMon;

public sealed class CpuCollector : ICollector<CpuData>, IDisposable
{
    private readonly PerformanceCounter _totalCounter;
    private readonly PerformanceCounter[] _coreCounters;
    private readonly int _coreCount;
    private readonly float[] _history;
    private int _historyIndex;
    private bool _primed;

    public CpuCollector()
    {
        _coreCount = Environment.ProcessorCount;
        _totalCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _coreCounters = new PerformanceCounter[_coreCount];

        for (int i = 0; i < _coreCount; i++)
            _coreCounters[i] = new PerformanceCounter("Processor", "% Processor Time", i.ToString());

        _history = new float[30];
        _totalCounter.NextValue();
        foreach (var c in _coreCounters) c.NextValue();
        _primed = false;
    }

    public Task<CpuData> CollectAsync(CancellationToken ct)
    {
        float total = _totalCounter.NextValue();
        var cores = new float[_coreCount];
        for (int i = 0; i < _coreCount; i++)
            cores[i] = _coreCounters[i].NextValue();

        if (!_primed)
        {
            _primed = true;
            return Task.FromResult(new CpuData
            {
                TotalUsage = 0,
                CoreUsages = new float[_coreCount],
                History = (float[])_history.Clone()
            });
        }

        _history[_historyIndex % 30] = total;
        _historyIndex++;

        var ordered = new float[30];
        for (int i = 0; i < 30; i++)
            ordered[i] = _history[(_historyIndex + i) % 30];

        return Task.FromResult(new CpuData
        {
            TotalUsage = total,
            CoreUsages = cores,
            Temperature = GetTemperature(),
            History = ordered
        });
    }

    private static float GetTemperature()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                @"root\WMI",
                "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
            foreach (var obj in searcher.Get())
            {
                var kelvin = Convert.ToDouble(obj["CurrentTemperature"]) / 10.0;
                return (float)(kelvin - 273.15);
            }
        }
        catch { }
        return -1f;
    }

    public void Dispose()
    {
        _totalCounter.Dispose();
        foreach (var c in _coreCounters) c.Dispose();
    }
}
