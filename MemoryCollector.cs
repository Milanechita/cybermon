using System.Runtime.InteropServices;

namespace CyberMon;

public sealed class MemoryCollector : ICollector<MemoryData>
{
    private readonly float[] _history = new float[30];
    private int _historyIndex;

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    public Task<MemoryData> CollectAsync(CancellationToken ct)
    {
        var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        GlobalMemoryStatusEx(ref mem);

        long total = (long)mem.ullTotalPhys;
        long available = (long)mem.ullAvailPhys;
        long used = total - available;
        long swapTotal = Math.Max(0, (long)mem.ullTotalPageFile - total);
        long swapAvail = Math.Max(0, (long)mem.ullAvailPageFile - available);
        long swapUsed = swapTotal - swapAvail;

        float usagePct = total > 0 ? (float)used / total * 100f : 0f;
        _history[_historyIndex % 30] = usagePct;
        _historyIndex++;

        var ordered = new float[30];
        for (int i = 0; i < 30; i++)
            ordered[i] = _history[(_historyIndex + i) % 30];

        return Task.FromResult(new MemoryData
        {
            TotalBytes = total,
            UsedBytes = used,
            AvailableBytes = available,
            SwapTotalBytes = swapTotal,
            SwapUsedBytes = swapUsed,
            History = ordered
        });
    }
}
