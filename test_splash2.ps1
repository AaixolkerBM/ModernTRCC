$ErrorActionPreference = 'SilentlyContinue'

Add-Type @'
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
public class WinEnum3 {
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lp);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT r);
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lp);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    public static string WinInfo(uint targetPid) {
        string s = "";
        EnumWindows((h, l) => { uint pid; GetWindowThreadProcessId(h, out pid); if (pid == targetPid && IsWindowVisible(h)) { RECT r; GetWindowRect(h, out r); s += (r.Right-r.Left) + "x" + (r.Bottom-r.Top) + "@" + r.Left + "," + r.Top + "  "; } return true; }, IntPtr.Zero);
        return s.Trim();
    }
}
'@

function Measure-Splash([string]$args2, [string]$label) {
    Stop-Process -Name "ModernTRCC" -Force
    Start-Sleep -Milliseconds 600
    $exe = "C:\Users\Administrator\AppData\Roaming\TRAE SOLO CN\ModularData\ai-agent\work-mode-projects\6a92a3d0c2fbe1b9b3b124af\ModernTRCC.WPF\publish\ModernTRCC.exe"
    Start-Process $exe $args2
    "=== $label ==="
    for ($i = 1; $i -le 20; $i++) {
        Start-Sleep -Milliseconds 500
        $p = Get-Process -Name "ModernTRCC" -ErrorAction SilentlyContinue
        if (-not $p) { "  [$($i*0.5)s] EXITED"; break }
        "  [$($i*0.5)s] " + [WinEnum3]::WinInfo([uint32]$p.Id)
    }
}

Measure-Splash "" "NORMAL LAUNCH"
Measure-Splash "--minimized" "MINIMIZED LAUNCH"
