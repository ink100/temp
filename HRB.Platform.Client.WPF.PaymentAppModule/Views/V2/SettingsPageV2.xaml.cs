using HRB.Platform.Client.WPF.Core.CustomAttributes;
using HRB.Platform.Client.WPF.Core.Models;
using HRB.Platform.Client.WPF.PaymentAppModule.Core;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.V2;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Views.V2
{
    [IocBindViewModelForNavigation(typeof(SettingsPageV2ViewModel), GlobalSettings.APP_MODULE_NAME)]
    public partial class SettingsPageV2 : BaseRegionUserControl
    {
        public SettingsPageV2()
        {
            InitializeComponent();
        }
    }
}
