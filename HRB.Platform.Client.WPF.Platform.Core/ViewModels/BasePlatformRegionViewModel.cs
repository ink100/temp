using HRB.Platform.Client.WPF.Core.Instruments;
using HRB.Platform.Client.WPF.Core.Models;
using HRB.Platform.Client.WPF.Core.Services.IServices;

namespace HRB.Platform.Client.WPF.Platform.Core.ViewModels
{
    public abstract class BasePlatformRegionViewModel : BaseRegionViewModel
    {
        protected BasePlatformRegionViewModel(PlatformWpfAppContext appContext, IEventAggregator eventAggregator,
            IRegionManager regionManager, IWpfDeviceRequestService deviceRequestService) : base(
            appContext, eventAggregator, regionManager, deviceRequestService)
        {
        }


        protected override void OnNavigatedToEvent(NavigationContext navigationContext)
        {
        }

        protected override void OnNavigatedFromEvent(NavigationContext navigationContext)
        {
        }

        protected override void OnDestroy()
        {
        }

        protected override void OnLoadedEvent()
        {
        }

        protected override void OnUnloadedEvent()
        {
        }
    }
}