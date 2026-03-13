using System.Text;

namespace CyberMon;

public sealed class TuiRenderer
{
    private readonly StringBuilder _buffer = new(8192);
    private int _width;

    private const string CursorHome = "\x1b[H";
    private const string ClearScreen = "\x1b[2J";
    private const string HideCursor = "\x1b[?25l";
    private const string ShowCursor = "\x1b[?25h";

    public void Init()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Write(HideCursor);
        Console.Write(ClearScreen);
    }

    public void Cleanup()
    {
        Console.Write(ShowCursor);
        Console.Write(Theme.Reset);
    }

    public void Render(SystemSnapshot snap)
    {
        _width = Math.Max(Console.WindowWidth, 80);

        _buffer.Clear();
        _buffer.Append(CursorHome);
        _buffer.Append(Theme.BgBlack);

        RenderHeader(snap);
        RenderCpuPanel(snap.Cpu);
        RenderMemoryPanel(snap.Memory);
        RenderDiskPanel(snap.Disk);
        RenderNetworkPanel(snap.Network);
        RenderProcessTable(snap.TopProcesses);
        RenderFooter();

        string clearLine = new(' ', _width);
        for (int i = 0; i < 5; i++)
            _buffer.AppendLine(clearLine);

        Console.Write(_buffer.ToString());
    }

    private void RenderHeader(SystemSnapshot snap)
    {
        string title = "╔══ CYBERMON v1.0 ══╗";
        int pad = (_width - title.Length) / 2;

        _buffer.AppendLine();
        _buffer.Append(' ', Math.Max(0, pad));
        _buffer.AppendLine(Theme.Green + Theme.Bold + title + Theme.Reset);

        string host = snap.HostName;
        string uptime = FormatUptime(snap.Uptime);
        string sep = " " + Theme.Dim + "│" + Theme.Reset + " ";
        string info = Theme.Cyan + "System Monitor" + Theme.Reset + sep
            + Theme.Magenta + "Host: " + host + Theme.Reset + sep
            + Theme.Pink + "Uptime: " + uptime + Theme.Reset + sep
            + Theme.Green + "Refresh: 1s" + Theme.Reset;

        int infoPad = (_width - StripAnsiLen(info)) / 2;
        _buffer.Append(' ', Math.Max(0, infoPad));
        _buffer.AppendLine(info);
        _buffer.Append(Theme.Dim);
        _buffer.AppendLine(new string('─', _width));
        _buffer.Append(Theme.Reset);
    }

    private void RenderCpuPanel(CpuData cpu)
    {
        int panelW = _width / 2 - 2;
        int barW = Math.Max(8, panelW - 22);

        string border = new('─', panelW - 7);
        _buffer.AppendLine(" " + Theme.Green + "┌─ CPU " + border + "┐" + Theme.Reset);

        int maxCores = Math.Min(cpu.CoreUsages.Length, 8);
        for (int i = 0; i < maxCores; i++)
        {
            float pct = cpu.CoreUsages[i];
            string bar = Theme.ProgressBar(pct, barW, Theme.Green);
            string pctStr = pct.ToString("F1").PadLeft(5);
            string coreLabel = ("Core " + i).PadRight(7);
            _buffer.Append(" " + Theme.Dim + "│" + Theme.Reset + " ");
            _buffer.Append(Theme.Cyan + coreLabel + Theme.Reset + " ");
            _buffer.Append(bar + " ");
            _buffer.Append(Theme.Pink + pctStr + "%" + Theme.Reset);
            _buffer.AppendLine(" " + Theme.Dim + "│" + Theme.Reset);
        }

        string totalStr = cpu.TotalUsage.ToString("F1").PadLeft(5);
        string tempStr = cpu.Temperature > 0 ? cpu.Temperature.ToString("F0") + "°C" : "N/A";
        _buffer.Append(" " + Theme.Dim + "│" + Theme.Reset + " ");
        _buffer.Append(Theme.Magenta + "Total:" + Theme.Reset + " " + Theme.Pink + totalStr + "%" + Theme.Reset);
        _buffer.Append(" " + Theme.Dim + "│" + Theme.Reset + " ");
        _buffer.AppendLine(Theme.Magenta + "Temp:" + Theme.Reset + " " + Theme.Pink + tempStr + Theme.Reset);

        string spark = Theme.Sparkline(cpu.History, Math.Min(30, barW), Theme.Green);
        _buffer.Append(" " + Theme.Dim + "│" + Theme.Reset + " ");
        _buffer.AppendLine(Theme.Cyan + "History" + Theme.Reset + " " + spark);

        string bottomBorder = new('─', panelW - 1);
        _buffer.AppendLine(" " + Theme.Green + "└" + bottomBorder + "┘" + Theme.Reset);
    }

    private void RenderMemoryPanel(MemoryData mem)
    {
        int panelW = _width / 2 - 2;
        int barW = Math.Max(8, panelW - 28);

        string border = new('─', panelW - 10);
        _buffer.AppendLine(" " + Theme.Magenta + "┌─ MEMORY " + border + "┐" + Theme.Reset);

        string ramBar = Theme.ProgressBar(mem.UsagePercent, barW, Theme.Magenta);
        string ramUsed = Theme.FormatBytes(mem.UsedBytes).PadLeft(8);
        string ramTotal = Theme.FormatBytes(mem.TotalBytes);
        _buffer.Append(" " + Theme.Dim + "│" + Theme.Reset + " ");
        _buffer.Append(Theme.Cyan + "RAM " + Theme.Reset + " " + ramBar + " ");
        _buffer.AppendLine(Theme.Pink + ramUsed + " / " + ramTotal + Theme.Reset);

        float swapPct = mem.SwapTotalBytes > 0 ? (float)mem.SwapUsedBytes / mem.SwapTotalBytes * 100f : 0f;
        string swapBar = Theme.ProgressBar(swapPct, barW, Theme.Magenta);
        string swapUsed = Theme.FormatBytes(mem.SwapUsedBytes).PadLeft(8);
        string swapTotal = Theme.FormatBytes(mem.SwapTotalBytes);
        _buffer.Append(" " + Theme.Dim + "│" + Theme.Reset + " ");
        _buffer.Append(Theme.Cyan + "Swap" + Theme.Reset + " " + swapBar + " ");
        _buffer.AppendLine(Theme.Pink + swapUsed + " / " + swapTotal + Theme.Reset);

        string usagePctStr = mem.UsagePercent.ToString("F0");
        string availStr = Theme.FormatBytes(mem.AvailableBytes);
        _buffer.Append(" " + Theme.Dim + "│" + Theme.Reset + " ");
        _buffer.Append(Theme.Magenta + "Used:" + Theme.Reset + " " + Theme.Pink + usagePctStr + "%" + Theme.Reset);
        _buffer.Append(" " + Theme.Dim + "│" + Theme.Reset + " ");
        _buffer.AppendLine(Theme.Magenta + "Avail:" + Theme.Reset + " " + Theme.Green + availStr + Theme.Reset);

        string spark = Theme.Sparkline(mem.History, Math.Min(30, barW), Theme.Magenta);
        _buffer.Append(" " + Theme.Dim + "│" + Theme.Reset + " ");
        _buffer.AppendLine(Theme.Magenta + "History" + Theme.Reset + " " + spark);

        string bottomBorder = new('─', panelW - 1);
        _buffer.AppendLine(" " + Theme.Magenta + "└" + bottomBorder + "┘" + Theme.Reset);
    }

    private void RenderDiskPanel(DiskData disk)
    {
        int panelW = _width / 2 - 2;
        int barW = Math.Max(8, panelW - 28);

        string border = new('─', panelW - 8);
        _buffer.AppendLine(" " + Theme.Cyan + "┌─ DISK " + border + "┐" + Theme.Reset);

        foreach (var drive in disk.Drives.Take(4))
        {
            string bar = Theme.ProgressBar(drive.UsagePercent, barW, Theme.Cyan);
            string name = drive.Name.TrimEnd('\\').PadRight(3);
            string used = Theme.FormatBytes(drive.UsedBytes).PadLeft(8);
            string total = Theme.FormatBytes(drive.TotalBytes);
            _buffer.Append(" " + Theme.Dim + "│" + Theme.Reset + " ");
            _buffer.Append(Theme.Green + name + Theme.Reset + " " + bar + " ");
            _buffer.AppendLine(Theme.Pink + used + " / " + total + Theme.Reset);
        }

        string readRate = Theme.FormatBytesPerSec(disk.ReadBytesPerSec);
        string writeRate = Theme.FormatBytesPerSec(disk.WriteBytesPerSec);
        _buffer.Append(" " + Theme.Dim + "│" + Theme.Reset + " ");
        _buffer.Append(Theme.Cyan + "Read:" + Theme.Reset + " " + Theme.Pink + readRate + Theme.Reset);
        _buffer.Append(" " + Theme.Dim + "│" + Theme.Reset + " ");
        _buffer.AppendLine(Theme.Cyan + "Write:" + Theme.Reset + " " + Theme.Pink + writeRate + Theme.Reset);

        string spark = Theme.Sparkline(disk.IoHistory, Math.Min(30, barW), Theme.Cyan);
        _buffer.Append(" " + Theme.Dim + "│" + Theme.Reset + " ");
        _buffer.AppendLine(Theme.Cyan + "I/O" + Theme.Reset + "    " + spark);

        string bottomBorder = new('─', panelW - 1);
        _buffer.AppendLine(" " + Theme.Cyan + "└" + bottomBorder + "┘" + Theme.Reset);
    }

    private void RenderNetworkPanel(NetworkData net)
    {
        int panelW = _width / 2 - 2;
        int barW = Math.Max(8, panelW - 26);

        string border = new('─', panelW - 11);
        _buffer.AppendLine(" " + Theme.Pink + "┌─ NETWORK " + border + "┐" + Theme.Reset);

        float maxRate = Math.Max(net.BytesSentPerSec, net.BytesReceivedPerSec);
        if (maxRate <= 0) maxRate = 1f;

        float upPct = net.BytesSentPerSec / maxRate * 100f;
        float downPct = net.BytesReceivedPerSec / maxRate * 100f;

        string upBar = Theme.ProgressBar(upPct, barW, Theme.Pink);
        string downBar = Theme.ProgressBar(downPct, barW, Theme.Pink);
        string upRate = Theme.FormatBytesPerSec(net.BytesSentPerSec);
        string downRate = Theme.FormatBytesPerSec(net.BytesReceivedPerSec);

        _buffer.Append(" " + Theme.Dim + "│" + Theme.Reset + " ");
        _buffer.AppendLine(Theme.Green + "▲ Up  " + Theme.Reset + " " + upBar + " " + Theme.Cyan + upRate + Theme.Reset);
        _buffer.Append(" " + Theme.Dim + "│" + Theme.Reset + " ");
        _buffer.AppendLine(Theme.Green + "▼ Down" + Theme.Reset + " " + downBar + " " + Theme.Cyan + downRate + Theme.Reset);

        string conns = net.ActiveConnections.ToString();
        string adapter = net.AdapterName;
        _buffer.Append(" " + Theme.Dim + "│" + Theme.Reset + " ");
        _buffer.Append(Theme.Pink + "Conns:" + Theme.Reset + " " + Theme.Cyan + conns + Theme.Reset);
        _buffer.Append(" " + Theme.Dim + "│" + Theme.Reset + " ");
        _buffer.AppendLine(Theme.Pink + "Adapter:" + Theme.Reset + " " + Theme.Cyan + adapter + Theme.Reset);

        string spark = Theme.Sparkline(net.History, Math.Min(30, barW), Theme.Pink);
        _buffer.Append(" " + Theme.Dim + "│" + Theme.Reset + " ");
        _buffer.AppendLine(Theme.Pink + "Traffic" + Theme.Reset + " " + spark);

        string bottomBorder = new('─', panelW - 1);
        _buffer.AppendLine(" " + Theme.Pink + "└" + bottomBorder + "┘" + Theme.Reset);
    }

    private void RenderProcessTable(ProcessData[] procs)
    {
        string border = new('─', _width - 19);
        _buffer.AppendLine(" " + Theme.Green + "┌─ TOP PROCESSES " + border + "┐" + Theme.Reset);

        string hdr = "PROCESS".PadRight(22) + " " + "CPU%".PadLeft(8) + " " + "MEM".PadLeft(10) + " " + "PID".PadLeft(8);
        _buffer.AppendLine(" " + Theme.Dim + "│" + Theme.Reset + " " + Theme.Magenta + hdr + Theme.Reset);

        foreach (var p in procs)
        {
            string cpuColor = p.CpuPercent > 50 ? Theme.Pink : p.CpuPercent > 20 ? Theme.Magenta : Theme.Text;
            string name = p.Name.PadRight(22);
            string cpuStr = p.CpuPercent.ToString("F1").PadLeft(7) + "%";
            string memStr = Theme.FormatBytes(p.MemoryBytes).PadLeft(10);
            string pidStr = p.Pid.ToString().PadLeft(8);

            _buffer.Append(" " + Theme.Dim + "│" + Theme.Reset + " ");
            _buffer.Append(Theme.Cyan + name + Theme.Reset + " ");
            _buffer.Append(cpuColor + cpuStr + Theme.Reset + " ");
            _buffer.Append(Theme.Text + memStr + Theme.Reset + " ");
            _buffer.AppendLine(Theme.Dim + pidStr + Theme.Reset);
        }

        string bottomBorder = new('─', _width - 3);
        _buffer.AppendLine(" " + Theme.Green + "└" + bottomBorder + "┘" + Theme.Reset);
    }

    private void RenderFooter()
    {
        string sep = " " + Theme.Dim + "│" + Theme.Reset + " ";
        string footer = "Press " + Theme.Green + "[Q]" + Theme.Reset + Theme.Dim + " Quit" + sep
            + Theme.Cyan + "[S]" + Theme.Reset + Theme.Dim + " Sort" + sep
            + Theme.Magenta + "[R]" + Theme.Reset + Theme.Dim + " Refresh" + sep
            + Theme.Pink + "[H]" + Theme.Reset + Theme.Dim + " Help";

        int pad = (_width - StripAnsiLen(footer)) / 2;
        _buffer.Append(' ', Math.Max(0, pad));
        _buffer.AppendLine(footer + Theme.Reset);
    }

    private static string FormatUptime(TimeSpan ts) =>
        ts.Days > 0
            ? ts.Days + "d " + ts.Hours + "h " + ts.Minutes + "m"
            : ts.Hours + "h " + ts.Minutes + "m " + ts.Seconds + "s";

    private static int StripAnsiLen(string s)
    {
        int len = 0;
        bool inEsc = false;
        foreach (char c in s)
        {
            if (c == '\x1b') { inEsc = true; continue; }
            if (inEsc) { if (char.IsLetter(c)) inEsc = false; continue; }
            len++;
        }
        return len;
    }
}
