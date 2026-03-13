using System.Net.NetworkInformation;

namespace CyberMon;

public sealed class NetworkCollector : ICollector<NetworkData>
{
    private long _lastBytesSent;
    private long _lastBytesReceived;
    private DateTime _lastSample = DateTime.MinValue;
    private string _adapterName = "";
    private readonly float[] _history = new float[30];
    private int _historyIndex;

    public Task<NetworkData> CollectAsync(CancellationToken ct)
    {
        var iface = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up
                     && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                     && n.GetIPv4Statistics().BytesReceived > 0)
            .OrderByDescending(n => n.GetIPv4Statistics().BytesReceived)
            .FirstOrDefault();

        if (iface == null)
            return Task.FromResult(NetworkData.Empty);

        var stats = iface.GetIPv4Statistics();
        var now = DateTime.UtcNow;
        _adapterName = iface.Name;

        float sentPerSec = 0f, recvPerSec = 0f;

        if (_lastSample != DateTime.MinValue)
        {
            double elapsed = (now - _lastSample).TotalSeconds;
            if (elapsed > 0)
            {
                sentPerSec = (float)((stats.BytesSent - _lastBytesSent) / elapsed);
                recvPerSec = (float)((stats.BytesReceived - _lastBytesReceived) / elapsed);
            }
        }

        _lastBytesSent = stats.BytesSent;
        _lastBytesReceived = stats.BytesReceived;
        _lastSample = now;

        float totalMbps = (sentPerSec + recvPerSec) / (1024f * 1024f);
        _history[_historyIndex % 30] = totalMbps;
        _historyIndex++;

        var ordered = new float[30];
        for (int i = 0; i < 30; i++)
            ordered[i] = _history[(_historyIndex + i) % 30];

        int connections = 0;
        try { connections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections().Length; }
        catch { }

        return Task.FromResult(new NetworkData
        {
            BytesSentPerSec = sentPerSec,
            BytesReceivedPerSec = recvPerSec,
            ActiveConnections = connections,
            AdapterName = _adapterName.Length > 16 ? _adapterName[..14] + ".." : _adapterName,
            History = ordered
        });
    }
}
