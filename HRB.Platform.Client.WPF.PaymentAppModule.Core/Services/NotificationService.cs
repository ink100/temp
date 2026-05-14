using HRB.Platform.Client.WPF.PaymentAppModule.Core.Controls;
using System.Windows;
using Application = System.Windows.Application;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    public class NotificationService : INotificationService
    {
        private NotificationContainer? _container;

        public void SetContainer(NotificationContainer container) => _container = container;

        public void ShowSuccess(string message, int seconds = 3)
        {
            Add(message, NotificationType.Success, seconds);
        }

        public void ShowError(string message, int seconds = 5)
        {
            Add(message, NotificationType.Error, seconds);
        }

        public void ShowWarning(string message, int seconds = 4)
        {
            Add(message, NotificationType.Warning, seconds);
        }

        public void ShowInfo(string message, int seconds = 3)
        {
            Add(message, NotificationType.Info, seconds);
        }

        private void Add(string message, NotificationType notificationType, int seconds)
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                _container?.Add(message, notificationType, seconds);

            });
        }

    }
}
