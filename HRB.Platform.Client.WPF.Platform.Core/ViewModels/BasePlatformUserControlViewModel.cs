using HRB.Platform.Client.WPF.Core.Instruments;
using HRB.Platform.Client.WPF.Core.Models;
using HRB.Platform.Client.WPF.Core.Services.IServices;

namespace HRB.Platform.Client.WPF.Platform.Core.ViewModels
{

    public abstract class BasePlatformUserControlViewModel : BaseUserControlViewModel
    {
        protected BasePlatformUserControlViewModel(PlatformWpfAppContext appContext, IEventAggregator eventAggregator, IRegionManager regionManager, IWpfDeviceRequestService deviceRequestService) : base(appContext, eventAggregator, regionManager, deviceRequestService)
        {

        }

    }

}
