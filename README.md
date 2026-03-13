# ⚡ CyberMon

**A cyberpunk-themed system monitor TUI for Windows.**

Built with C# and .NET 8. Zero external UI dependencies. Pure ANSI escape codes.

```
╔══ CYBERMON v1.0 ══╗
  System Monitor │ Host: DESKTOP-X7K │ Uptime: 3d 14h 22m │ Refresh: 1s
───────────────────────────────────────────────────────────────────────────
┌─ CPU ─────────────────────────┐  ┌─ MEMORY ────────────────────────┐
│ Core 0 ████████████░░░░ 72.3% │  │ RAM  ██████████████░░ 12.4/16 G│
│ Core 1 ██████░░░░░░░░░░ 38.1% │  │ Swap ███░░░░░░░░░░░░  1.2/8 G │
│ Core 2 ██████████████░░ 89.4% │  │ Used: 77% │ Avail: 3.6 GB      │
│ Core 3 ████░░░░░░░░░░░░ 25.0% │  │ History ▅▅▆▆▇▇█▇▇▆▅▅▆▆▇▇█▇▇  │
│ Total: 56.2% │ Temp: 67°C     │  └─────────────────────────────────┘
│ History ▁▂▃▅▇█▇▅▆▇█▇▅▃▂▃▅▆▇  │
└───────────────────────────────┘  ┌─ NETWORK ───────────────────────┐
┌─ DISK ────────────────────────┐  │ ▲ Up   ████░░░░░░░░░░ 2.4 MB/s │
│ C:\ ████████████░░░░ 186/256G │  │ ▼ Down ██████████░░░░ 18.7 MB/s│
│ D:\ ██████░░░░░░░░░░ 420/1.0T │  │ Conns: 47 │ Adapter: Ethernet  │
│ Read: 145 MB/s │ Write: 52 MB/s│  │ Traffic ▂▃▅▇█▇▅▃▂▃▅▇█▇▅▃▂▃▅  │
│ I/O ▁▂▁▃▇█▃▁▂▁▃▅▂▁▁▂▇█▅▂▁▁  │  └─────────────────────────────────┘
└───────────────────────────────┘
┌─ TOP PROCESSES ──────────────────────────────────────────────────────┐
│ PROCESS                   CPU%        MEM       PID                  │
│ chrome.exe               24.3%    1.8 GB     14220                  │
│ devenv.exe               12.1%    980 MB      8844                  │
│ node.exe                  8.7%    340 MB      5512                  │
└──────────────────────────────────────────────────────────────────────┘
              Press [Q] Quit │ [S] Sort │ [R] Refresh │ [H] Help
```

## Features

- **Real-time metrics** — CPU per-core, RAM, Swap, Disk I/O, Network traffic
- **Sparkline history** — 30-second rolling graph for every metric
- **Top processes** — Top 5 by CPU with memory usage and PID
- **Cyberpunk aesthetic** — Neon green, cyan, magenta, pink on black
- **Lightweight** — ~600 lines of code, ~15MB RAM footprint
- **No UI frameworks** — Pure ANSI escape codes, no Spectre.Console, no curses

## Requirements

- **Windows 10/11**
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (or newer)
- **Windows Terminal** recommended (for proper 24-bit color support)

## Quick start

```bash
git clone https://github.com/YOUR_USERNAME/cybermon.git
cd cybermon
dotnet run
```

## Build standalone .exe

```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

Binary output: `bin/Release/net8.0-windows/win-x64/publish/CyberMon.exe`

## Controls

| Key   | Action        |
|-------|---------------|
| `Q`   | Quit          |
| `Esc` | Quit          |
| `S`   | Sort processes |
| `R`   | Force refresh |
| `H`   | Show help     |

## Architecture

```
Collectors (async)         Shared State           Renderer
┌────────────────┐        ┌────────────────┐     ┌────────────────┐
│ CpuCollector   │──┐     │                │     │                │
│ MemCollector   │──┼────▶│ SystemSnapshot │────▶│  TuiRenderer   │──▶ Terminal
│ DiskCollector  │──┤     │  (immutable)   │     │  (ANSI codes)  │
│ NetCollector   │──┘     │                │     │                │
│ ProcCollector  │──┘     └────────────────┘     └────────────────┘
└────────────────┘
         ▲ Task.WhenAll         Atomic swap              StringBuilder
         │ (parallel)           (no locks)               (single flush)
```

**Design principles:**

- `SystemSnapshot` is an immutable record — collectors write, renderer reads, no locks needed
- Each collector implements `ICollector<T>` — one interface, one method, no ceremony
- All formatting lives in `Theme.cs` — one file controls the entire visual identity
- The renderer writes to a `StringBuilder` and flushes once per frame — no flickering

## Project structure

```
CyberMon.csproj          # Project file with 2 NuGet deps
LICENSE                   # MIT
src/
  Models.cs              # Immutable data records
  ICollector.cs          # Collector interface (4 lines)
  CpuCollector.cs        # PerformanceCounter per-core
  MemoryCollector.cs     # P/Invoke GlobalMemoryStatusEx
  DiskCollector.cs       # DriveInfo + PerformanceCounter
  NetworkCollector.cs    # NetworkInterface stats
  ProcessCollector.cs    # Process enumeration + CPU delta
  Theme.cs               # ANSI colors, bars, sparklines
  TuiRenderer.cs         # Full-screen rendering engine
  Program.cs             # Main loop orchestrator
```

## Notes

- **Temperature** may show `N/A` — depends on BIOS exposing data via WMI
- **Run as Administrator** for full process visibility
- **CMD.exe** may not render colors correctly — use Windows Terminal

## License

MIT — do whatever you want with it.
