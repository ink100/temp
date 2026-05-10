using HRB.Platform.Client.WPF.Core.CustomAttributes;
using HRB.Platform.Client.WPF.PaymentAppModule.Core;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.ViewModels;
using System.Windows.Controls;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Views
{
    /// <summary>
    /// SettingsPage.xaml 的交互逻辑
    /// </summary>
    [IocBindViewModelForNavigation(typeof(SettingsPageViewModel), GlobalSettings.APP_MODULE_NAME)]
    public partial class SettingsPage 
    {
        public SettingsPage()
        {
            InitializeComponent();
        }
    }
}
