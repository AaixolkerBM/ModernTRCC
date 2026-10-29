using System.Windows;
using System.Windows.Media.Animation;

namespace ModernTRCC.App.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();

        // 等布局完成后再启动动画，保证能拿到轨道的真实宽度
        Loaded += (_, _) => StartProgressAnimation();
    }

    /// <summary>
    /// 不确定进度条：金色滑块在灰色轨道内来回滑动，不会超出轨道范围。
    /// </summary>
    private void StartProgressAnimation()
    {
        double trackWidth = ProgressTrack.ActualWidth;
        double sliderWidth = ProgressFill.ActualWidth;
        double maxOffset = Math.Max(0, trackWidth - sliderWidth);

        var anim = new DoubleAnimation
        {
            From = 0,
            To = maxOffset,
            Duration = TimeSpan.FromMilliseconds(1200),
            AutoReverse = true,
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
