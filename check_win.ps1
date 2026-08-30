Add-Type @'
using System;
using System.Runtime.InteropServices;
using System.Text;
public struct RECT { public int Left, Top, Right, Bottom; }
public class W2 {
  [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
  [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
  [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hWnd, int nIndex);
  [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr hWndParent, EnumProc lpEnumFunc, IntPtr lParam);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
  delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);
  public static string Info(IntPtr h) {
    RECT wr, cr; GetWindowRect(h, out wr); GetClientRect(h, out cr);
    int style = GetWindowLong(h, -16);
    int exstyle = GetWindowLong(h, -20);
    var sb = new StringBuilder();
    sb.AppendLine("Window: " + wr.Left + "," + wr.Top + " " + (wr.Right-wr.Left) + "x" + (wr.Bottom-wr.Top));
    sb.AppendLine("Client: " + (cr.Right-cr.Left) + "x" + (cr.Bottom-cr.Top));
    sb.AppendLine("Style: 0x" + style.ToString("X8") + " ExStyle: 0x" + exstyle.ToString("X8"));
    sb.AppendLine("--- Children ---");
    EnumChildWindows(h, (c, l) => {
      var cn = new StringBuilder(256); GetClassName(c, cn, cn.Capacity);
      RECT cr2; GetWindowRect(c, out cr2);
      sb.AppendLine(cn + " : " + (cr2.Right-cr2.Left) + "x" + (cr2.Bottom-cr2.Top) + " at " + cr2.Left + "," + cr2.Top);
      return true;
    }, IntPtr.Zero);
    return sb.ToString();
  }
}
'@
$p = Get-Process -Name "ModernTRCC" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($p) { [W2]::Info($p.MainWindowHandle) } else { Write-Output "Not running" }
