$ErrorActionPreference = 'SilentlyContinue'

Stop-Process -Name "ModernTRCC" -Force
Start-Sleep -Milliseconds 500

$exe = "C:\Users\Administrator\AppData\Roaming\TRAE SOLO CN\ModularData\ai-agent\work-mode-projects\6a92a3d0c2fbe1b9b3b124af\ModernTRCC.WPF\publish\ModernTRCC.exe"
Start-Process $exe

Add-Type @'
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
public class WinEnum {
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lp);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr hWnd, StringBuilder sb, int max);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr hWnd, StringBuilder sb, int max);
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lp);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    public static List<string> ListForPid(uint targetPid) {
        var list = new List<string>();
        EnumWindows((h, l) => {
            uint pid; GetWindowThreadProcessId(h, out pid);
            if (pid == targetPid && IsWindowVisible(h)) {
                RECT r; GetWindowRect(h, out r);
                var t = new StringBuilder(256); GetWindowText(h, t, 256);
                var c = new StringBuilder(256); GetClassName(h, c, 256);
                list.Add(c + " | '" + t + "' | " + r.Left + "," + r.Top + " " + (r.Right-r.Left) + "x" + (r.Bottom-r.Top));
            }
            return true;
        }, IntPtr.Zero);
        return list;
    }
}
'@

Start-Sleep -Milliseconds 400
$p = Get-Process -Name "ModernTRCC" -ErrorAction SilentlyContinue
if ($p) {
    "PID=" + $p.Id
    "--- WINDOWS AT ~400ms ---"
    [WinEnum]::ListForPid([uint32]$p.Id)
} else { "NOT RUNNING" }

Start-Sleep -Milliseconds 2500
$p = Get-Process -Name "ModernTRCC" -ErrorAction SilentlyContinue
if ($p) {
    "--- WINDOWS AT ~2.9s ---"
    [WinEnum]::ListForPid([uint32]$p.Id)
} else { "EXITED" }
