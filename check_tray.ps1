Add-Type @'
using System;
using System.Runtime.InteropServices;
using System.Text;
public struct RECT2 { public int Left, Top, Right, Bottom; }
public class T {
  [DllImport("user32.dll")] static extern IntPtr FindWindow(string cls, string win);
  [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT2 lpRect);
  [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hWnd);
  public static string Info() {
    var sb = new StringBuilder();
    IntPtr tray = FindWindow("Shell_TrayWnd", null);
    if (tray != IntPtr.Zero) {
      RECT2 r; GetWindowRect(tray, out r);
      sb.AppendLine("Taskbar: " + r.Left + "," + r.Top + " " + (r.Right-r.Left) + "x" + (r.Bottom-r.Top) + " Visible: " + IsWindowVisible(tray));
    } else {
      sb.AppendLine("Taskbar: not found");
    }
    IntPtr tray2 = FindWindow("Shell_SecondaryTrayWnd", null);
    if (tray2 != IntPtr.Zero) {
      RECT2 r; GetWindowRect(tray2, out r);
      sb.AppendLine("SecondaryTray: " + r.Left + "," + r.Top + " " + (r.Right-r.Left) + "x" + (r.Bottom-r.Top));
    }
    return sb.ToString();
  }
}
'@
[T]::Info()
