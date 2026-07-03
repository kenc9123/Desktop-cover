using System.Runtime.InteropServices;

namespace TopoShell.App;

internal sealed class SystemTelemetry
{
    private ulong? _previousIdle;
    private ulong? _previousKernel;
    private ulong? _previousUser;

    public TelemetrySnapshot Read()
    {
        var cpu = ReadCpuUsage();
        var memory = ReadMemory();

        return new TelemetrySnapshot(
            cpu,
            memory.MemoryUsagePercent,
            memory.UsedMemoryGb,
            memory.TotalMemoryGb,
            null);
    }

    private double? ReadCpuUsage()
    {
        if (!GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
        {
            return null;
        }

        var idle = idleTime.ToUInt64();
        var kernel = kernelTime.ToUInt64();
        var user = userTime.ToUInt64();

        if (_previousIdle is null || _previousKernel is null || _previousUser is null)
        {
            _previousIdle = idle;
            _previousKernel = kernel;
            _previousUser = user;
            return null;
        }

        var idleDelta = idle - _previousIdle.Value;
        var kernelDelta = kernel - _previousKernel.Value;
        var userDelta = user - _previousUser.Value;
        var total = kernelDelta + userDelta;

        _previousIdle = idle;
        _previousKernel = kernel;
        _previousUser = user;

        if (total == 0)
        {
            return 0;
        }

        return Math.Clamp((1.0 - idleDelta / (double)total) * 100.0, 0, 100);
    }

    private static MemorySnapshot ReadMemory()
    {
        var status = new MemoryStatusEx();
        status.dwLength = (uint)Marshal.SizeOf<MemoryStatusEx>();

        if (!GlobalMemoryStatusEx(ref status) || status.ullTotalPhys == 0)
        {
            return new MemorySnapshot(0, 0, 0);
        }

        var totalGb = status.ullTotalPhys / 1024d / 1024d / 1024d;
        var availableGb = status.ullAvailPhys / 1024d / 1024d / 1024d;
        var usedGb = Math.Max(0, totalGb - availableGb);

        return new MemorySnapshot(status.dwMemoryLoad, usedGb, totalGb);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;

        public readonly ulong ToUInt64() => ((ulong)dwHighDateTime << 32) | dwLowDateTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
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

    private readonly record struct MemorySnapshot(double MemoryUsagePercent, double UsedMemoryGb, double TotalMemoryGb);
}

internal readonly record struct TelemetrySnapshot(
    double? CpuUsagePercent,
    double MemoryUsagePercent,
    double UsedMemoryGb,
    double TotalMemoryGb,
    double? GpuUsagePercent);
