using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;

// 测试 WMI USB 扫描
var results = new List<string>();
try
{
    using var searcher = new ManagementObjectSearcher(
        "SELECT * FROM Win32_PnPEntity WHERE PNPDeviceID LIKE 'USB%'");
    foreach (var item in searcher.Get())
    {
        var name = item["Name"]?.ToString() ?? "";
        var deviceId = item["DeviceID"]?.ToString() ?? "";
        var status = item["Status"]?.ToString() ?? "";
        
        var vidMatch = Regex.Match(deviceId, @"VID_([0-9A-Fa-f]{4})", RegexOptions.IgnoreCase);
        var pidMatch = Regex.Match(deviceId, @"PID_([0-9A-Fa-f]{4})", RegexOptions.IgnoreCase);
        
        if (vidMatch.Success)
        {
            results.Add($"VID:0x{vidMatch.Groups[1].Value.ToUpper()} PID:0x{pidMatch.Groups[1].Value.ToUpper()} | {name} | {status}");
        }
    }
}
catch (Exception ex)
{
    results.Add($"ERROR: {ex.Message}");
}

Console.WriteLine($"Found {results.Count} USB devices:");
foreach (var r in results)
    Console.WriteLine(r);
