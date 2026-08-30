namespace ModernTRCC.Core.Models;

public class LcdDeviceInfo
{
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public int Vid { get; set; }
    public int Pid { get; set; }
    public LcdDeviceType Type { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsInitialized { get; set; }
    public string? SerialNumber { get; set; }
    public byte Fbl { get; set; }
    public byte Pm { get; set; }

    public string ResolutionText => $"{Width}×{Height}";
    public string VidPidText => $"0x{Vid:X4}:0x{Pid:X4}";
}

public enum LcdDeviceType
{
    Unknown = 0,
    Scsi = 1,      // SCSI / USBLCD (0x87CD, 0x87AD)
    Bulk = 2,      // Bulk USB (unused, kept for compat)
    Hid = 3,       // HID Type 2/3 (0x0416:0x5302)
    Ali = 4,       // ALi Corp (0x0416:0x5406)
    Ly = 5,        // LY (0x0416:0x5408)
    Ly1 = 6,       // LY1 (0x0416:0x5409)
    Led = 7        // LED (not LCD)
}
