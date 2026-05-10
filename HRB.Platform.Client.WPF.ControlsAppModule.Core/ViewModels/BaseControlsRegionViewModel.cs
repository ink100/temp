using HRB.Platform.Client.WPF.Core.Models;
using HRB.Platform.Client.WPF.Core.Services.IServices;

namespace HRB.Platform.Client.WPF.ControlsAppModule.Core.ViewModels
{
    public abstract class BaseControlsRegionViewModel : BaseRegionViewModel
    {
        protected BaseControlsRegionViewModel(ControlsAppContext appContext, IEventAggregator eventAggregator, IRegionManager regionManager, IWpfDeviceRequestService deviceRequestService) : base(appContext, eventAggregator, regionManager, deviceRequestService)
        {
        }
    }

}
