Add-Type @'
using System;
using System.Runtime.InteropServices;
using System.Text;
public struct RECT { public int Left, Top, Right, Bottom; }
public struct MONITORINFO { public uint cbSize; public RECT rcMonitor; public RECT rcWork; public uint dwFlags; }
public class Mon {
  [DllImport("user32.dll")] static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
  [DllImport("user32.dll")] static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);
  [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
  [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
  [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hWnd);
  delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, IntPtr lprcClip, IntPtr dwData);
  public static string GetAll() {
    var sb = new StringBuilder();
    EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (h, dc, clip, d) => {
      var mi = new MONITORINFO(); mi.cbSize = (uint)Marshal.SizeOf<MONITORINFO>();
      GetMonitorInfo(h, ref mi);
      sb.AppendLine("Monitor: " + mi.rcMonitor.Left + "," + mi.rcMonitor.Top + " " + (mi.rcMonitor.Right-mi.rcMonitor.Left) + "x" + (mi.rcMonitor.Bottom-mi.rcMonitor.Top) + " | Work: " + mi.rcWork.Left + "," + mi.rcWork.Top + " " + (mi.rcWork.Right-mi.rcWork.Left) + "x" + (mi.rcWork.Bottom-mi.rcWork.Top));
      return true;
    }, IntPtr.Zero);
    return sb.ToString();
  }
  public static string WinInfo(IntPtr h) {
    RECT r; GetWindowRect(h, out r);
    int style = GetWindowLong(h, -16);
    return "Rect: " + r.Left + "," + r.Top + " " + (r.Right-r.Left) + "x" + (r.Bottom-r.Top) + " Visible: " + IsWindowVisible(h) + " Style: 0x" + style.ToString("X8");
  }
}
'@
Write-Output "=== Monitors ==="
[Mon]::GetAll()
Write-Output "=== ModernTRCC Window ==="
$p = Get-Process -Name "ModernTRCC" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($p) {
  [Mon]::WinInfo($p.MainWindowHandle)
} else {
  Write-Output "Not running"
}
