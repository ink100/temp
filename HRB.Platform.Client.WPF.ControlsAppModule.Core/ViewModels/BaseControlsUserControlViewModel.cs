using HRB.Platform.Client.WPF.Core.Models;
using HRB.Platform.Client.WPF.Core.Services.IServices;

namespace HRB.Platform.Client.WPF.ControlsAppModule.Core.ViewModels
{
    public abstract class BaseControlsUserControlViewModel : BaseUserControlViewModel
    {
        protected BaseControlsUserControlViewModel(ControlsAppContext appContext, IEventAggregator eventAggregator, IRegionManager regionManager, IWpfDeviceRequestService deviceRequestService) : base(
            appContext, eventAggregator, regionManager, deviceRequestService)
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