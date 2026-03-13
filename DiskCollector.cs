using System.Diagnostics;

namespace CyberMon;

public sealed class DiskCollector : ICollector<DiskData>, IDisposable
{
    private PerformanceCounter? _readCounter;
    private PerformanceCounter? _writeCounter;
    private readonly float[] _ioHistory = new float[30];
    private int _historyIndex;

    public DiskCollector()
    {
        try
        {
            _readCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
            _writeCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
            _readCounter.NextValue();
            _writeCounter.NextValue();
        }
        catch
        {
            _readCounter = null;
            _writeCounter = null;
        }
    }

    public Task<DiskData> CollectAsync(CancellationToken ct)
    {
        var drives = DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
            .Select(d => new DriveMetrics
            {
                Name = d.Name,
                TotalBytes = d.TotalSize,
                UsedBytes = d.TotalSize - d.AvailableFreeSpace
            })
            .ToArray();

        float read = _readCounter?.NextValue() ?? 0f;
        float write = _writeCounter?.NextValue() ?? 0f;

        float ioTotal = (read + write) / (1024f * 1024f);
        _ioHistory[_historyIndex % 30] = ioTotal;
        _historyIndex++;

        var ordered = new float[30];
        for (int i = 0; i < 30; i++)
            ordered[i] = _ioHistory[(_historyIndex + i) % 30];

        return Task.FromResult(new DiskData
        {
            Drives = drives,
            ReadBytesPerSec = read,
            WriteBytesPerSec = write,
            IoHistory = ordered
        });
    }

    public void Dispose()
    {
        _readCounter?.Dispose();
        _writeCounter?.Dispose();
    }
}
