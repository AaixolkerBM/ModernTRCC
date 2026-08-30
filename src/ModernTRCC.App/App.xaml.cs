using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using ModernTRCC.Core.Services;
using ModernTRCC.App.Views;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace ModernTRCC.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        try
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();

            var settings = Services.GetRequiredService<SettingsService>();
            settings.Initialize();

            // 启动时扫描 media 目录，加载已有素材（后台执行，不阻塞启动）
            var mediaLib = Services.GetRequiredService<MediaLibraryService>();
            _ = Task.Run(() => mediaLib.InitializeAsync());

            if (settings.Settings.AutoStart.HighPriority)
                SettingsService.SetProcessPriority(ProcessPriorityClass.High);

            bool startMinimized = e.Args.Contains("--minimized");

            // 启动画面：最小化启动（自启动/后台运行）时不显示
            SplashWindow? splash = null;
            if (!startMinimized)
            {
                splash = new SplashWindow();
                splash.Show();
                splash.SetStatus("正在加载界面...");
            }

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;

            // 有启动画面时，先把主窗口放到屏幕外初始化（WebView2 是独立子窗口，
            // WPF 的 Opacity 不能让它透明），就绪后再移回屏幕中央并淡入
            if (splash != null)
            {
                mainWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                mainWindow.Left = -10000;
                mainWindow.Top = -10000;
                mainWindow.ShowInTaskbar = false;
            }

            mainWindow.Ready += () =>
            {
                if (splash == null) return;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    splash.SetStatus("启动完成");
                    splash.Close();
                    splash = null;

                    // 先隐藏再移到屏幕中央，避免闪现，随后淡入
                    mainWindow.Opacity = 0;
                    CenterMainWindow(mainWindow);
                    mainWindow.ShowInTaskbar = true;

                    var fade = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromMilliseconds(220),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    mainWindow.BeginAnimation(UIElement.OpacityProperty, fade);
                }));
            };

            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"启动失败：{ex}\n\n{ex.StackTrace}", "Modern TRCC", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<LogService>();
        services.AddSingleton<LcdController>();
        services.AddSingleton<FFmpegService>();
        services.AddSingleton<FramePrerenderService>();
        services.AddSingleton<MediaLibraryService>();
        services.AddSingleton<SettingsService>();
    }

    /// <summary>
    /// 将窗口居中到主显示器工作区（多显示器取当前鼠标所在屏幕）
    /// </summary>
    private static void CenterMainWindow(Window window)
    {
        try
        {
            double sx = SystemParameters.PrimaryScreenWidth;
            double sy = SystemParameters.PrimaryScreenHeight;
            window.Left = (sx - window.Width) / 2;
            window.Top = (sy - window.Height) / 2;
        }
        catch
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        MessageBox.Show($"未处理异常：{e.Exception.Message}\n\n{e.Exception.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        MessageBox.Show($"致命错误：{ex?.Message ?? "未知"}", "致命错误", MessageBoxButton.OK, MessageBoxImage.Stop);
    }
}
