using ModernTRCC.Core.Models;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace ModernTRCC.Core.Services;

public class SettingsService
{
    private readonly string _settingsPath;
    private readonly LogService _log;
    public AppSettings Settings { get; private set; }

    public event EventHandler? SettingsChanged;

    public SettingsService(LogService log)
    {
        _log = log;
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string appDir = Path.Combine(appData, "ModernTRCC");
        Directory.CreateDirectory(appDir);
        _settingsPath = Path.Combine(appDir, "settings.json");

        Settings = Load();
    }

    public AppSettings Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                string json = File.ReadAllText(_settingsPath);
                var settings = JsonConvert.DeserializeObject<AppSettings>(json);
                if (settings != null) return settings;
            }
        }
        catch
        {
            // 加载失败使用默认值
        }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            string json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
            File.WriteAllText(_settingsPath, json);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            // 保存失败忽略
        }
    }

    public void Initialize()
    {
        // 初始化时可以做一些启动逻辑
        // 例如：确保媒体库目录存在等
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string mediaDir = Path.Combine(appData, "ModernTRCC", "Media");
        Directory.CreateDirectory(mediaDir);
    }

    #region 自启动管理

    public bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue("ModernTRCC") != null;
        }
        catch
        {
            return false;
        }
    }

    public bool EnableAutoStart(bool startMinimized = false, bool highPriority = false)
    {
        try
        {
            string exePath = Process.GetCurrentProcess().MainModule?.FileName
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ModernTRCC.exe");

            string args = startMinimized ? " --minimized" : "";
            string command = $"\"{exePath}\"{args}";

            using var key = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);
            key?.SetValue("ModernTRCC", command);

            Settings.AutoStart.Enabled = true;
            Settings.AutoStart.StartMinimized = startMinimized;
            Settings.AutoStart.HighPriority = highPriority;
            Save();

            if (highPriority)
            {
                SetupHighPriorityTask(startMinimized);
            }

            _log.Success($"开机自启动已注册: {command}" + (highPriority ? "（含高优先级任务）" : ""));
            return true;
        }
        catch (Exception ex)
        {
            _log.Error($"开机自启动注册失败: {ex.Message}");
            return false;
        }
    }

    public bool DisableAutoStart()
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);
            key?.DeleteValue("ModernTRCC", false);

            Settings.AutoStart.Enabled = false;
            Save();

            // 移除任务计划
            RemoveHighPriorityTask();

            _log.Success("开机自启动已取消注册");
            return true;
        }
        catch (Exception ex)
        {
            _log.Error($"取消开机自启动失败: {ex.Message}");
            return false;
        }
    }

    private void SetupHighPriorityTask(bool startMinimized)
    {
        try
        {
            string exePath = Process.GetCurrentProcess().MainModule?.FileName
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ModernTRCC.exe");

            string taskName = "ModernTRCC High Priority";

            string xml = $@"<?xml version=""1.0"" encoding=""UTF-16""?>
<Task version=""1.4"" xmlns=""http://schemas.microsoft.com/windows/2004/02/mit/task"">
  <RegistrationInfo>
    <Description>Modern TRCC - High Priority Startup</Description>
    <URI>\ModernTRCC</URI>
  </RegistrationInfo>
  <Triggers>
    <LogonTrigger>
      <Enabled>true</Enabled>
      <Delay>PT0S</Delay>
    </LogonTrigger>
  </Triggers>
  <Settings>
    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
    <AllowHardTerminate>true</AllowHardTerminate>
    <StartWhenAvailable>true</StartWhenAvailable>
    <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
    <IdleSettings>
      <StopOnIdleEnd>false</StopOnIdleEnd>
      <RestartOnIdle>false</RestartOnIdle>
    </IdleSettings>
    <AllowStartOnDemand>true</AllowStartOnDemand>
    <Enabled>true</Enabled>
    <Hidden>false</Hidden>
    <RunOnlyIfIdle>false</RunOnlyIfIdle>
    <UseUnifiedSchedulingEngine>true</UseUnifiedSchedulingEngine>
    <WakeToRun>false</WakeToRun>
    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
    <Priority>0</Priority>
  </Settings>
  <Actions Context=""Author"">
    <Exec>
      <Command>""{exePath}""</Command>
      <Arguments>{(startMinimized ? "--minimized" : "")}</Arguments>
    </Exec>
  </Actions>
  <Principals>
    <Principal id=""Author"">
      <LogonType>InteractiveToken</LogonType>
      <RunLevel>HighestAvailable</RunLevel>
    </Principal>
  </Principals>
</Task>";

            string tmpFile = Path.GetTempFileName() + ".xml";
            File.WriteAllText(tmpFile, xml);

            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/Create /TN \"{taskName}\" /XML \"{tmpFile}\" /F",
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            Process.Start(psi)?.WaitForExit(5000);

            File.Delete(tmpFile);
            _log.Success($"高优先级启动任务已创建: {taskName}");
        }
        catch (Exception ex)
        {
            _log.Error($"高优先级启动任务创建失败: {ex.Message}");
        }
    }

    private void RemoveHighPriorityTask()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = "/Delete /TN \"ModernTRCC High Priority\" /F",
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            Process.Start(psi)?.WaitForExit(5000);
            _log.Success("高优先级启动任务已移除");
        }
        catch (Exception ex)
        {
            _log.Error($"高优先级启动任务移除失败: {ex.Message}");
        }
    }

    #endregion

    #region 进程优先级

    public static void SetProcessPriority(ProcessPriorityClass priority)
    {
        try
        {
            Process.GetCurrentProcess().PriorityClass = priority;
        }
        catch
        {
            // 权限不足时忽略
        }
    }

    #endregion
}
