using System.Windows.Controls;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Controls;
using System.Windows;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Controls
{
    public partial class NotificationContainer : UserControl
    {
        public NotificationContainer()
        {
            InitializeComponent();
        }

        public void Add(string message, NotificationType type, int seconds)
        {
            var notification = new NotificationControl(message, type, seconds);
            notification.OnRemove = () => StackContainer.Children.Remove(notification);
            StackContainer.Children.Insert(0, notification);

        }
    }
}
