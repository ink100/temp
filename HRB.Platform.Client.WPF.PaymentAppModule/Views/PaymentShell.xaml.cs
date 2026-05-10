using HRB.Platform.Client.WPF.Core.CustomAttributes;
using HRB.Platform.Client.WPF.Core.Instruments.Abstractions;
using HRB.Platform.Client.WPF.PaymentAppModule.Core;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Services;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Views
{
    /// <summary>
    /// PaymentShell.xaml 的交互逻辑
    /// </summary>
    [IocBindViewModelForNavigation(typeof(PaymentShellViewModel), GlobalSettings.APP_MODULE_NAME)]
    public partial class PaymentShell : IEntryView
    {
        public PaymentShell(INotificationService notificationService, ILoadingOverlayService loadingOverlayService)
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                notificationService.SetContainer(NotificationContainer);
                loadingOverlayService.SetContainer(LoadingOverlayContainer);
            };
        }
    }
}
