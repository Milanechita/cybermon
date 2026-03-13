namespace CyberMon;

public static class Theme
{
    public const string Green   = "\x1b[38;2;0;255;159m";
    public const string Cyan    = "\x1b[38;2;0;255;255m";
    public const string Magenta = "\x1b[38;2;176;96;255m";
    public const string Pink    = "\x1b[38;2;255;96;144m";
    public const string Dim     = "\x1b[38;2;85;85;85m";
    public const string Text    = "\x1b[38;2;176;176;176m";
    public const string White   = "\x1b[38;2;220;220;220m";
    public const string BgBlack = "\x1b[48;2;10;10;15m";
    public const string Reset   = "\x1b[0m";
    public const string Bold    = "\x1b[1m";

    public const string TopLeft     = "┌";
    public const string TopRight    = "┐";
    public const string BottomLeft  = "└";
    public const string BottomRight = "┘";
    public const string Horizontal  = "─";
    public const string Vertical    = "│";
    public const string Separator   = "│";

    public static readonly char[] SparkChars = ['▁', '▂', '▃', '▄', '▅', '▆', '▇', '█'];

    public const char BarFull  = '█';
    public const char BarEmpty = '░';

    public static string ProgressBar(float percent, int width, string color)
    {
        int filled = (int)(percent / 100f * width);
        filled = Math.Clamp(filled, 0, width);
        string bar = new string(BarFull, filled) + new string(BarEmpty, width - filled);
        return color + bar + Reset;
    }

    public static string Sparkline(float[] values, int width, string color)
    {
        if (values.Length == 0) return new string(SparkChars[0], width);
        float max = values.Max();
        if (max <= 0) max = 1f;

        int start = Math.Max(0, values.Length - width);
        var chars = new char[width];
        for (int i = 0; i < width; i++)
        {
            int idx = start + i;
            float val = idx < values.Length ? values[idx] : 0f;
            int level = (int)(val / max * 7f);
            level = Math.Clamp(level, 0, 7);
            chars[i] = SparkChars[level];
        }
        return color + new string(chars) + Reset;
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes >= 1L << 30) return (bytes / (1024.0 * 1024.0 * 1024.0)).ToString("F1") + " GB";
        if (bytes >= 1L << 20) return (bytes / (1024.0 * 1024.0)).ToString("F0") + " MB";
        if (bytes >= 1L << 10) return (bytes / 1024.0).ToString("F0") + " KB";
        return bytes + " B";
    }

    public static string FormatBytesPerSec(float bps)
    {
        if (bps >= 1024f * 1024f) return (bps / (1024f * 1024f)).ToString("F1") + " MB/s";
        if (bps >= 1024f) return (bps / 1024f).ToString("F1") + " KB/s";
        return bps.ToString("F0") + " B/s";
    }
}
