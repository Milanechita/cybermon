namespace CyberMon;

public sealed record SystemSnapshot
{
    public CpuData Cpu { get; init; } = CpuData.Empty;
    public MemoryData Memory { get; init; } = MemoryData.Empty;
    public DiskData Disk { get; init; } = DiskData.Empty;
    public NetworkData Network { get; init; } = NetworkData.Empty;
    public ProcessData[] TopProcesses { get; init; } = [];
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public TimeSpan Uptime { get; init; }
    public string HostName { get; init; } = Environment.MachineName;
}

public sealed record CpuData
{
    public float TotalUsage { get; init; }
    public float[] CoreUsages { get; init; } = [];
    public float Temperature { get; init; }
    public float[] History { get; init; } = [];
    public static CpuData Empty => new();
}

public sealed record MemoryData
{
    public long TotalBytes { get; init; }
    public long UsedBytes { get; init; }
    public long AvailableBytes { get; init; }
    public long CachedBytes { get; init; }
    public long SwapTotalBytes { get; init; }
    public long SwapUsedBytes { get; init; }
    public float[] History { get; init; } = [];
    public float UsagePercent => TotalBytes > 0 ? (float)UsedBytes / TotalBytes * 100f : 0f;
    public static MemoryData Empty => new();
}

public sealed record DiskData
{
    public DriveMetrics[] Drives { get; init; } = [];
    public float ReadBytesPerSec { get; init; }
    public float WriteBytesPerSec { get; init; }
    public float[] IoHistory { get; init; } = [];
    public static DiskData Empty => new();
}

public sealed record DriveMetrics
{
    public string Name { get; init; } = "";
    public long TotalBytes { get; init; }
    public long UsedBytes { get; init; }
    public float UsagePercent => TotalBytes > 0 ? (float)UsedBytes / TotalBytes * 100f : 0f;
}

public sealed record NetworkData
{
    public float BytesSentPerSec { get; init; }
    public float BytesReceivedPerSec { get; init; }
    public int ActiveConnections { get; init; }
    public string AdapterName { get; init; } = "";
    public float[] History { get; init; } = [];
    public static NetworkData Empty => new();
}

public sealed record ProcessData
{
    public string Name { get; init; } = "";
    public int Pid { get; init; }
    public float CpuPercent { get; init; }
    public long MemoryBytes { get; init; }
}
