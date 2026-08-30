using System.Windows;
using System.Windows.Media.Animation;

namespace ModernTRCC.App.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();

        // 不确定进度条：金色滑块在轨道内来回滑动。
        // 轨道宽度 = 窗口宽度 - 左右边距(36*2)；滑块宽 140，完全滑出右侧时 X = 轨道宽度
        double trackWidth = Math.Max(100, Width - 72);
        var anim = new DoubleAnimation
        {
            From = -140,
            To = trackWidth,
            Duration = TimeSpan.FromMilliseconds(1500),
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };
        ProgressTranslate.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, anim);
    }

    public void SetStatus(string text)
    {
        StatusText.Text = text;
    }
}
