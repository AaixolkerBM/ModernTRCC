$ErrorActionPreference = 'SilentlyContinue'

Add-Type @'
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
public class Hittest {
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lp);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
    [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr parent, EnumWindowsProc cb, IntPtr lp);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr hWnd, StringBuilder sb, int max);
    [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lp);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    public static IntPtr FindMain(uint targetPid) {
        IntPtr found = IntPtr.Zero;
        EnumWindows((h, l) => { uint pid; GetWindowThreadProcessId(h, out pid); if (pid == targetPid) { RECT r; GetWindowRect(h, out r); if ((r.Right-r.Left) > 800 && (r.Bottom-r.Top) > 500 && r.Left > -5000) { found = h; return false; } } return true; }, IntPtr.Zero);
        return found;
    }
    public static string Children(IntPtr parent) {
        string s = "";
        EnumChildWindows(parent, (h, l) => { var sb = new StringBuilder(256); GetClassName(h, sb, 256); RECT r; GetWindowRect(h, out r); s += sb + " " + (r.Right-r.Left) + "x" + (r.Bottom-r.Top) + " | "; return true; }, IntPtr.Zero);
        return s.Trim(' ', '|');
    }
    public static IntPtr FindClass(IntPtr parent, string clsPrefix) {
        IntPtr found = IntPtr.Zero;
        EnumChildWindows(parent, (h, l) => { var sb = new StringBuilder(256); GetClassName(h, sb, 256); if (sb.ToString().StartsWith(clsPrefix)) { found = h; return false; } return true; }, IntPtr.Zero);
        return found;
    }
}
'@

$p = Get-Process -Name "ModernTRCC" -ErrorAction SilentlyContinue
if (-not $p) { "APP NOT RUNNING"; exit }
$pid2 = [uint32]$p.Id
$main = [Hittest]::FindMain($pid2)
"Main hwnd: 0x$($main.ToString('X'))"
"Child windows: " + [Hittest]::Children($main)

$chrome = [Hittest]::FindClass($main, "Chrome_WidgetWin")
"Chrome_WidgetWin: 0x$($chrome.ToString('X'))"

# Get main window rect
$rect = New-Object Hittest+RECT
[Hittest]::GetWindowRect($main, [ref]$rect)

# WM_NCHITTEST (0x84), lParam = screen coords
# Bottom-right corner (inside by 3px)
$bx = $rect.Right - 3
$by = $rect.Bottom - 3
$lp1 = ($by -shl 16) -bor ($bx -band 0xFFFF)
$r1 = [Hittest]::SendMessage($main, 0x84, [IntPtr]::Zero, [IntPtr]$lp1)
"HT at bottom-right(edge): $r1 (expect 17=HTBOTTOMRIGHT)"

# Center of window
$cx = $rect.Left + ($rect.Right - $rect.Left) / 2
$cy = $rect.Top + ($rect.Bottom - $rect.Top) / 2
$lp2 = ([int]$cy -shl 16) -bor ([int]$cx -band 0xFFFF)
$r2 = [Hittest]::SendMessage($main, 0x84, [IntPtr]::Zero, [IntPtr]$lp2)
"HT at center: $r2 (expect 1=HTCLIENT)"

# Right edge middle
$rx = $rect.Right - 3
$ry = $rect.Top + ($rect.Bottom - $rect.Top) / 2
$lp3 = ([int]$ry -shl 16) -bor ([int]$rx -band 0xFFFF)
$r3 = [Hittest]::SendMessage($main, 0x84, [IntPtr]::Zero, [IntPtr]$lp3)
"HT at right edge: $r3 (expect 11=HTRIGHT)"

# Now test the CHROME child (WebView2 host): send WM_NCHITTEST to it at bottom-right
if ($chrome -ne [IntPtr]::Zero) {
    $cr = New-Object Hittest+RECT
    [Hittest]::GetWindowRect($chrome, [ref]$cr)
    "Chrome rect: $($cr.Left),$($cr.Top) $($cr.Right-$cr.Left)x$($cr.Bottom-$cr.Top)"
    $cbx = $cr.Right - 3
    $cby = $cr.Bottom - 3
    $clp = ($cby -shl 16) -bor ($cbx -band 0xFFFF)
    $rc = [Hittest]::SendMessage($chrome, 0x84, [IntPtr]::Zero, [IntPtr]$clp)
    "HT on Chrome child bottom-right: $rc (expect 17 = forwarded resize)"
}
