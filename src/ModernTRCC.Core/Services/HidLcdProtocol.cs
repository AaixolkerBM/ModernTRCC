using System.Buffers.Binary;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ModernTRCC.Core.Models;
using DrawPixelFormat = System.Drawing.Imaging.PixelFormat;

namespace ModernTRCC.Core.Services;

internal static class HidLcdProtocol
{
    public static byte[] CreateInfoRequest()
    {
        var request = new byte[512];
        WriteMagic(request);
        request[12] = 1;
        return request;
    }

    public static bool TryApplyDeviceInfo(ReadOnlySpan<byte> response, LcdDeviceInfo device)
    {
        if (response.Length < 20 || !response[..4].SequenceEqual(new byte[] { 0xDA, 0xDB, 0xDC, 0xDD })
            || response[12] != 1 || response[16] != 16) return false;
        device.Pm = response[5];
        device.Fbl = response[5];
        (device.Width, device.Height) = device.Pm switch
        {
            // PM 58 is the locally observed Frozen Warframe SE. The wire raster is portrait.
            5 or 50 or 58 => (240, 320),
            32 or 100 or 101 or 102 => (320, 320),
            36 or 37 => (240, 240),
            54 => (360, 360),
            7 => (640, 480),
            9 or 11 => (854, 480),
            10 => (960, 540),
            12 => (800, 480),
            64 => (1600, 720),
            65 => (1920, 462),
            128 => (1280, 480),
            _ => (480, 480)
        };
        if (response.Length >= 36) device.SerialNumber = Convert.ToHexString(response[20..36]);
        return true;
    }

    public static byte[] CreateFrame(byte[] rgb, LcdDeviceInfo device)
    {
        if (rgb.Length != checked(device.Width * device.Height * 3))
            throw new ArgumentException("像素帧大小与屏幕分辨率不一致", nameof(rgb));
        bool raw = device.Pm is 5 or 50 or 58;
        byte[] payload;
        if (raw)
        {
            payload = new byte[device.Width * device.Height * 2];
            for (int i = 0, j = 0; i < rgb.Length; i += 3, j += 2)
            {
                ushort pixel = (ushort)((rgb[i] >> 3) << 11 | (rgb[i + 1] >> 2) << 5 | rgb[i + 2] >> 3);
                BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(j, 2), pixel);
            }
        }
        else
        {
            using var bitmap = new Bitmap(device.Width, device.Height, DrawPixelFormat.Format24bppRgb);
            var bits = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, DrawPixelFormat.Format24bppRgb);
            try
            {
                var row = new byte[device.Width * 3];
                for (int y = 0; y < device.Height; y++)
                {
                    for (int x = 0; x < row.Length; x += 3)
                    {
                        int i = y * row.Length + x;
                        row[x] = rgb[i + 2]; row[x + 1] = rgb[i + 1]; row[x + 2] = rgb[i];
                    }
                    Marshal.Copy(row, 0, bits.Scan0 + y * bits.Stride, row.Length);
                }
            }
            finally { bitmap.UnlockBits(bits); }
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Jpeg);
            payload = stream.ToArray();
        }
        var frame = new byte[(20 + payload.Length + 511) / 512 * 512];
        WriteMagic(frame);
        frame[4] = 2;
        frame[6] = raw ? (byte)1 : (byte)0;
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(8, 2), (ushort)device.Width);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(10, 2), (ushort)device.Height);
        frame[12] = 2;
        BinaryPrimitives.WriteInt32LittleEndian(frame.AsSpan(16, 4), payload.Length);
        payload.CopyTo(frame, 20);
        return frame;
    }

    private static void WriteMagic(byte[] packet)
    {
        packet[0] = 0xDA; packet[1] = 0xDB; packet[2] = 0xDC; packet[3] = 0xDD;
    }
}
