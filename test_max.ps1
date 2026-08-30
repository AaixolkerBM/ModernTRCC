Add-Type @'
using System;
using System.Runtime.InteropServices;
public class Win {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int c);
  [DllImport("user32.dll")] public static extern IntPtr MonitorFromWindow(IntPtr h, int f);
  [DllImport("user32.dll")] public static extern bool GetMonitorInfo(IntPtr h, ref MONITORINFO i);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L,T,R,B; }
  [StructLayout(LayoutKind.Sequential)] public struct MONITORINFO { public uint cbSize; public RECT rcMonitor; public RECT rcWork; public uint flags; }
}
'@

$p = Get-Process -Name 'ModernTRCC' -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
if (-not $p) { 'PROCESS/MAIN WINDOW NOT FOUND'; exit }
$h = $p.MainWindowHandle

$mi = New-Object Win+MONITORINFO
$mi.cbSize = [System.Runtime.InteropServices.Marshal]::SizeOf($mi)
[Win]::GetMonitorInfo([Win]::MonitorFromWindow($h, 2), [ref]$mi) | Out-Null

[Win]::ShowWindow($h, 3) | Out-Null
Start-Sleep -Milliseconds 900
$r = New-Object Win+RECT
[Win]::GetWindowRect($h, [ref]$r) | Out-Null
'Maximize via ShowWindow:'
'  rect   : ' + $r.L + ',' + $r.T + ' -> ' + $r.R + ',' + $r.B + '  size ' + ($r.R-$r.L) + 'x' + ($r.B-$r.T)
'  work   : ' + $mi.rcWork.L + ',' + $mi.rcWork.T + ' -> ' + $mi.rcWork.R + ',' + $mi.rcWork.B + '  size ' + ($mi.rcWork.R-$mi.rcWork.L) + 'x' + ($mi.rcWork.B-$mi.rcWork.T)
'  monitor: ' + $mi.rcMonitor.L + ',' + $mi.rcMonitor.T + ' -> ' + $mi.rcMonitor.R + ',' + $mi.rcMonitor.B

[Win]::ShowWindow($h, 9) | Out-Null
Start-Sleep -Milliseconds 600
[Win]::ShowWindow($h, 3) | Out-Null
Start-Sleep -Milliseconds 900
$r2 = New-Object Win+RECT
[Win]::GetWindowRect($h, [ref]$r2) | Out-Null
'Re-maximize: size ' + ($r2.R-$r2.L) + 'x' + ($r2.B-$r2.T)
