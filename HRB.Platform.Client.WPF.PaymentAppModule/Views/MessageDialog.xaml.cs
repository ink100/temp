using HRB.Platform.Client.WPF.Core.CustomAttributes;
using HRB.Platform.Client.WPF.PaymentAppModule.Core;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.ViewModels;
using System.Windows.Controls;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Views
{
    /// <summary>
    /// MessageDialog.xaml 的交互逻辑
    /// </summary>
    [IocBindViewModelForNavigation(typeof(MessageDialogViewModel), GlobalSettings.APP_MODULE_NAME)]
    public partial class MessageDialog : UserControl
    {
        public MessageDialog()
        {
            InitializeComponent();
        }
    }
}
