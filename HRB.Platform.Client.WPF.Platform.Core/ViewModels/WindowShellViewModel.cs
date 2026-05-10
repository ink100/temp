using HRB.Platform.Client.WPF.Core.Instruments;
using HRB.Platform.Client.WPF.Core.Services.IServices;

namespace HRB.Platform.Client.WPF.Platform.Core.ViewModels
{

    public class WindowShellViewModel : BaseShellViewModel
    {



        public WindowShellViewModel(PlatformWpfAppContext appContext, IEventAggregator eventAggregator, IRegionManager regionManager, IWpfDeviceRequestService deviceRequestService) : base(appContext, eventAggregator, regionManager, deviceRequestService)
        {

        }


    }

}