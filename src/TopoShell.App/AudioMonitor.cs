using System.Runtime.InteropServices;

namespace TopoShell.App;

internal sealed class AudioMonitor : IDisposable
{
    private IAudioMeterInformation? _meter;

    public AudioMonitor()
    {
        TryInitialize();
    }

    public double ReadPeak()
    {
        if (_meter is null)
        {
            TryInitialize();
        }

        if (_meter is null)
        {
            return 0;
        }

        return _meter.GetPeakValue(out var peak) == 0
            ? Math.Clamp(peak, 0, 1)
            : 0;
    }

    public void Dispose()
    {
        if (_meter is not null)
        {
            Marshal.ReleaseComObject(_meter);
            _meter = null;
        }
    }

    private void TryInitialize()
    {
        try
        {
            var enumerator = (IMMDeviceEnumerator)(object)new MMDeviceEnumerator();
            var result = enumerator.GetDefaultAudioEndpoint(EDataFlow.Render, ERole.Console, out var device);
            Marshal.ReleaseComObject(enumerator);

            if (result != 0 || device is null)
            {
                return;
            }

            var meterId = typeof(IAudioMeterInformation).GUID;
            result = device.Activate(ref meterId, ClsCtx.InprocServer, IntPtr.Zero, out var meter);
            Marshal.ReleaseComObject(device);

            if (result == 0 && meter is IAudioMeterInformation audioMeter)
            {
                _meter = audioMeter;
            }
        }
        catch
        {
            _meter = null;
        }
    }

    private enum EDataFlow
    {
        Render,
        Capture,
        All
    }

    private enum ERole
    {
        Console,
        Multimedia,
        Communications
    }

    private static class ClsCtx
    {
        public const int InprocServer = 0x1;
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private sealed class MMDeviceEnumerator
    {
    }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        int EnumAudioEndpoints(EDataFlow dataFlow, uint stateMask, out IntPtr devices);
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice endpoint);
        int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);
        int RegisterEndpointNotificationCallback(IntPtr client);
        int UnregisterEndpointNotificationCallback(IntPtr client);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        int Activate(ref Guid iid, int clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object interfacePointer);
        int OpenPropertyStore(int access, out IntPtr properties);
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
        int GetState(out int state);
    }

    [ComImport]
    [Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioMeterInformation
    {
        int GetPeakValue(out float peak);
        int GetMeteringChannelCount(out int channelCount);
        int GetChannelsPeakValues(int channelCount, [Out] float[] peakValues);
        int QueryHardwareSupport(out int hardwareSupportMask);
    }
}
