using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Controls
{
    public enum NotificationType { Success, Error, Warning, Info }

    public partial class NotificationControl : UserControl
    {
        public Action? OnRemove;
        private DispatcherTimer? _timer;

        public NotificationControl(string message, NotificationType type, int seconds)
        {
            InitializeComponent();

            MessageText.Text = message;
            ApplyStyle(type);

            Loaded += (s, e) =>
            {
                AnimateIn();
                _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
                _timer.Tick += (_, __) => { _timer.Stop(); AnimateOut(); };
                _timer.Start();
            };
        }

        private void ApplyStyle(NotificationType type)
        {
            var (bg, border, icon) = type switch
            {
                NotificationType.Success => ("#4CAF50", "#388E3C", "✓"),
                NotificationType.Error => ("#F44336", "#D32F2F", "✕"),
                NotificationType.Warning => ("#FF9800", "#F57C00", "⚠"),
                _ => ("#2196F3", "#1976D2", "ℹ")
            };

            NotificationBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg));
            NotificationBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(border));
            IconText.Text = icon;
        }

        private void AnimateIn()
        {
            var opacity = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            var translate = new DoubleAnimation(-50, 0, TimeSpan.FromMilliseconds(300));
            NotificationBorder.BeginAnimation(OpacityProperty, opacity);
            ((TranslateTransform)NotificationBorder.RenderTransform).BeginAnimation(TranslateTransform.YProperty, translate);
        }

        private void AnimateOut()
        {
            var opacity = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
            var translate = new DoubleAnimation(0, -50, TimeSpan.FromMilliseconds(300));
            opacity.Completed += (s, e) => OnRemove?.Invoke();
            NotificationBorder.BeginAnimation(OpacityProperty, opacity);
            ((TranslateTransform)NotificationBorder.RenderTransform).BeginAnimation(TranslateTransform.YProperty, translate);
        }
    }
}
