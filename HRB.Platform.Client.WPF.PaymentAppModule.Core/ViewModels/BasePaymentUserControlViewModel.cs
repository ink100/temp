using HRB.Platform.Client.WPF.Core.Models;
using HRB.Platform.Client.WPF.Core.Services.IServices;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.ViewModels
{

    public abstract class BasePaymentUserControlViewModel : BaseUserControlViewModel
    {
        protected BasePaymentUserControlViewModel(PaymentAppContext appContext, IEventAggregator eventAggregator, IRegionManager regionManager, IWpfDeviceRequestService deviceRequestService) : base(appContext, eventAggregator, regionManager, deviceRequestService)
        {

        }

    }

}
