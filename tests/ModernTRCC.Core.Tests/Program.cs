using System.Buffers.Binary;
using ModernTRCC.Core.Models;
using ModernTRCC.Core.Services;
using System.Drawing;

int checks = 0;
void Check(bool condition, string label)
{
    if (!condition) throw new Exception(label);
    checks++;
    Console.WriteLine($"PASS {label}");
}

var request = HidLcdProtocol.CreateInfoRequest();
Check(request.Length == 512 && request[12] == 1 && request[..4].SequenceEqual(new byte[] { 0xDA, 0xDB, 0xDC, 0xDD }), "device info request");
// 本机 0416:5302 的握手回复布局（序列号已替换为模拟值，HID report ID 已去除）
var reply = Convert.FromHexString("DADBDCDD003A0000000000000100000010000000000102030405060708090A0B0C0D0E0F");
var device = new LcdDeviceInfo();
Check(HidLcdProtocol.TryApplyDeviceInfo(reply, device), "real 36-byte handshake accepted");
Check(device.Pm == 58 && device.Width == 240 && device.Height == 320, "PM 58 uses native 240x320 raster");
Check(device.SerialNumber?.Length == 32, "serial in exactly 36-byte reply");
Check(!HidLcdProtocol.TryApplyDeviceInfo(reply[..19], new LcdDeviceInfo()), "truncated reply rejected");
reply[0] = 0;
Check(!HidLcdProtocol.TryApplyDeviceInfo(reply, new LcdDeviceInfo()), "bad handshake magic rejected");
reply[0] = 0xDA;
reply[12] = 0;
Check(!HidLcdProtocol.TryApplyDeviceInfo(reply, new LcdDeviceInfo()), "failed handshake rejected");
var rgb = new byte[240 * 320 * 3];
rgb[0] = 255; rgb[4] = 255; rgb[8] = 255;
var frame = HidLcdProtocol.CreateFrame(rgb, device);
Check(frame.Length % 512 == 0 && frame.Length == 154112, "frame padded to HID packet boundary");
Check(frame[..4].SequenceEqual(new byte[] { 0xDA, 0xDB, 0xDC, 0xDD }) && frame[4] == 2 && frame[6] == 1 && frame[12] == 2, "complete RGB565 frame header");
Check(BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(8)) == 240 && BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(10)) == 320, "little-endian frame dimensions");
Check(BinaryPrimitives.ReadInt32LittleEndian(frame.AsSpan(16)) == 153600, "little-endian payload length");
Check(frame.AsSpan(20, 6).SequenceEqual(new byte[] { 0, 248, 224, 7, 31, 0 }), "red green blue RGB565 little-endian");
Check(frame.AsSpan(153620).ToArray().All(b => b == 0), "alignment padding is zero");
try { HidLcdProtocol.CreateFrame(new byte[3], device); throw new Exception("invalid frame accepted"); }
catch (ArgumentException) { Check(true, "incorrect frame size rejected"); }
var wideReply = Convert.FromHexString("DADBDCDD01800000000000000100000010000000000102030405060708090A0B0C0D0E0F");
var wide = new LcdDeviceInfo();
Check(HidLcdProtocol.TryApplyDeviceInfo(wideReply, wide) && wide.Width == 1280 && wide.Height == 480, "wide PM 128 geometry");
var jpegFrame = HidLcdProtocol.CreateFrame(new byte[1280 * 480 * 3], wide);
Check(jpegFrame[6] == 0 && jpegFrame[20] == 0xFF && jpegFrame[21] == 0xD8, "wide panel JPEG packet");
using (var controller = new LcdController())
    Check(!controller.IsConnected && !controller.SendFrameRgb888(rgb), "disconnected controller rejects frames");
if (args.Contains("--enumerate"))
{
    using var controller = new LcdController();
    controller.ScanDevices();
    foreach (var d in controller.Devices)
        Console.WriteLine($"DEVICE {d.VidPidText} {d.Name} HID={d.HidDevicePath != null}");
    Check(controller.Devices.Any(d => d.Vid == 0x0416 && d.Pid == 0x5302 && d.HidDevicePath != null), "local HID screen is discoverable without driver changes");
}
Console.WriteLine($"{checks} checks passed");

// 四个象限方向不对称，可检测裁切/旋转方向错误/宽高互换
foreach (int rotation in new[] { 0, 90, 180, 270 })
{
    bool quarterTurn = rotation is 90 or 270;
    int width = quarterTurn ? 320 : 240, height = quarterTurn ? 240 : 320;
    using var source = new Bitmap(width, height);
    using (var graphics = Graphics.FromImage(source))
    {
        graphics.FillRectangle(Brushes.Red, 0, 0, width / 2, height / 2);
        graphics.FillRectangle(Brushes.Lime, width / 2, 0, width / 2, height / 2);
        graphics.FillRectangle(Brushes.Blue, 0, height / 2, width / 2, height / 2);
        graphics.FillRectangle(Brushes.White, width / 2, height / 2, width / 2, height / 2);
    }
    var pixels = FramePrerenderService.RenderBitmapToRgb888(source, 240, 320, 100, 100, 100, rotation, "fill");
    Check(pixels.Length == 240 * 320 * 3, $"rotation {rotation} preserves native raster dimensions");
    var expected = rotation switch
    {
        90 => new[] { Color.Blue, Color.Red, Color.White, Color.Lime },
        180 => new[] { Color.White, Color.Blue, Color.Lime, Color.Red },
        270 => new[] { Color.Lime, Color.White, Color.Red, Color.Blue },
        _ => new[] { Color.Red, Color.Lime, Color.Blue, Color.White }
    };
    var positions = new[] { (60, 80), (180, 80), (60, 240), (180, 240) };
    Check(positions.Select((p, i) =>
    {
        int offset = (p.Item2 * 240 + p.Item1) * 3;
        return pixels[offset] == expected[i].R && pixels[offset + 1] == expected[i].G && pixels[offset + 2] == expected[i].B;
    }).All(match => match), $"rotation {rotation} keeps all four corners in order without clipping");
}
Console.WriteLine($"TOTAL {checks} checks passed");