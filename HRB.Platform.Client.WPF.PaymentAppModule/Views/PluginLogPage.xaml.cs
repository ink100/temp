using HRB.Platform.Client.WPF.Core.CustomAttributes;
using HRB.Platform.Client.WPF.Core.Models;
using HRB.Platform.Client.WPF.PaymentAppModule.Core;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.ViewModels;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Views
{
    /// <summary>
    /// PluginLogPage.xaml 的交互逻辑
    /// </summary>
    [IocBindViewModelForNavigation(typeof(PluginLogPageViewModel), GlobalSettings.APP_MODULE_NAME)]
    public partial class PluginLogPage : BaseRegionUserControl
    {
        public PluginLogPage()
        {
            InitializeComponent();
        }
    }
}
