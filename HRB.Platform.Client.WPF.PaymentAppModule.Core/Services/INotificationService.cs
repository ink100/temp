using HRB.Platform.Client.WPF.PaymentAppModule.Core.Controls;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    public interface INotificationService
    {
        void ShowSuccess(string message, int seconds = 3);
        void ShowError(string message, int seconds = 5);
        void ShowWarning(string message, int seconds = 4);
        void ShowInfo(string message, int seconds = 3);
        void SetContainer(NotificationContainer container);
    }
}
