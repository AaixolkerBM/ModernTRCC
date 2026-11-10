using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ModernTRCC.Core.Services;

/// <summary>Windows HID transport. Keeps the vendor HID driver installed and preserves report boundaries.</summary>
internal sealed class HidLcdTransport : IDisposable
{
    private readonly FileStream _stream;
    private readonly int _inputLength;
    private readonly int _outputLength;

    private HidLcdTransport(SafeFileHandle handle, int inputLength, int outputLength)
    {
        _stream = new FileStream(handle, FileAccess.ReadWrite, 1, isAsync: true);
        _inputLength = inputLength;
        _outputLength = outputLength;
    }

    public static IEnumerable<string> FindDevices(int vid, int pid)
    {
        HidD_GetHidGuid(out var guid);
        var set = SetupDiGetClassDevs(ref guid, null, IntPtr.Zero, 0x12);
        if (set == new IntPtr(-1)) throw new Win32Exception();
        var paths = new List<string>();
        try
        {
            for (uint index = 0; ; index++)
            {
                var data = new DeviceInterfaceData { Size = Marshal.SizeOf<DeviceInterfaceData>() };
                if (!SetupDiEnumDeviceInterfaces(set, IntPtr.Zero, ref guid, index, ref data))
                {
                    if (Marshal.GetLastWin32Error() == 259) break;
                    throw new Win32Exception();
                }
                SetupDiGetDeviceInterfaceDetail(set, ref data, IntPtr.Zero, 0, out var size, IntPtr.Zero);
                var detail = Marshal.AllocHGlobal((int)size);
                try
                {
                    Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
                    if (!SetupDiGetDeviceInterfaceDetail(set, ref data, detail, size, out _, IntPtr.Zero))
                        throw new Win32Exception();
                    var path = Marshal.PtrToStringUni(detail + 4)!;
                    if (!path.Contains($"vid_{vid:x4}&pid_{pid:x4}", StringComparison.OrdinalIgnoreCase)) continue;
                    using var handle = OpenHandle(path, 0, false);
                    if (!handle.IsInvalid && GetReportLengths(handle, out var input, out var output)
                        && input >= 21 && output == 513)
                        paths.Add(path);
                }
                finally { Marshal.FreeHGlobal(detail); }
            }
        }
        finally { SetupDiDestroyDeviceInfoList(set); }
        return paths;
    }

    public static HidLcdTransport Open(string path)
    {
        var handle = OpenHandle(path, 0xC0000000, true);
        try
        {
            if (handle.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error(), "无法打开 HID 屏幕");
            if (!GetReportLengths(handle, out var input, out var output) || input < 21 || output != 513)
                throw new IOException("HID 屏幕报告长度不兼容");
            return new HidLcdTransport(handle, input, output);
        }
        catch { handle.Dispose(); throw; }
    }

    public byte[] Exchange(byte[] command)
    {
        Write(command);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var report = new byte[_inputLength];
        var count = _stream.ReadAsync(report.AsMemory(), timeout.Token).AsTask().GetAwaiter().GetResult();
        // HIDClass includes the unnumbered report's ID byte; it is not part of the TRCC packet.
        if (count < 2 || report[0] != 0) throw new IOException("HID 握手报告无效");
        return report[1..count];
    }

    public void Write(byte[] packet)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var report = new byte[_outputLength];
        int payloadLength = _outputLength - 1;
        for (int offset = 0; offset < packet.Length; offset += payloadLength)
        {
            Array.Clear(report);
            packet.AsSpan(offset, Math.Min(payloadLength, packet.Length - offset)).CopyTo(report.AsSpan(1));
            _stream.WriteAsync(report.AsMemory(), timeout.Token).AsTask().GetAwaiter().GetResult();
        }
    }

    public void Dispose() => _stream.Dispose();

    private static SafeFileHandle OpenHandle(string path, uint access, bool overlapped) =>
        CreateFile(path, access, 3, IntPtr.Zero, 3, overlapped ? 0x40000000u : 0, IntPtr.Zero);

    private static bool GetReportLengths(SafeFileHandle handle, out int input, out int output)
    {
        input = output = 0;
        if (!HidD_GetPreparsedData(handle, out var preparsed)) return false;
        var caps = Marshal.AllocHGlobal(64); // sizeof(HIDP_CAPS)
        try
        {
            if (HidP_GetCaps(preparsed, caps) != 0x110000) return false;
            input = (ushort)Marshal.ReadInt16(caps, 4);
            output = (ushort)Marshal.ReadInt16(caps, 6);
            return true;
        }
        finally { Marshal.FreeHGlobal(caps); HidD_FreePreparsedData(preparsed); }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DeviceInterfaceData { public int Size; public Guid ClassGuid; public int Flags; public UIntPtr Reserved; }
    [DllImport("hid.dll")] private static extern void HidD_GetHidGuid(out Guid guid);
    [DllImport("hid.dll")] private static extern bool HidD_GetPreparsedData(SafeFileHandle handle, out IntPtr data);
    [DllImport("hid.dll")] private static extern bool HidD_FreePreparsedData(IntPtr data);
    [DllImport("hid.dll")] private static extern int HidP_GetCaps(IntPtr data, IntPtr caps);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SetupDiGetClassDevs(ref Guid guid, string? enumerator, IntPtr parent, uint flags);
    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInterfaces(IntPtr set, IntPtr info, ref Guid guid, uint index, ref DeviceInterfaceData data);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set, ref DeviceInterfaceData data, IntPtr detail, uint size, out uint required, IntPtr info);
    [DllImport("setupapi.dll")] private static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
}
