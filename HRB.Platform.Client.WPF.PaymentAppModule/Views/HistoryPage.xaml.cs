using HRB.Platform.Client.WPF.Core.CustomAttributes;
using HRB.Platform.Client.WPF.PaymentAppModule.Core;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.ViewModels;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Views
{
    /// <summary>
    /// Interaction logic for HistoryView.xaml
    /// </summary>
    [IocBindViewModelForNavigation(typeof(HistoryPageModel), GlobalSettings.APP_MODULE_NAME)]
    public partial class HistoryPage
    {
        public HistoryPage()
        {
            InitializeComponent();
        }
    }
}

