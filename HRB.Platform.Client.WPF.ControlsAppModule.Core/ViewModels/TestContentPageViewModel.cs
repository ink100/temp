using HRB.Platform.Client.WPF.Core.Services.IServices;
using System.Windows.Input;

namespace HRB.Platform.Client.WPF.ControlsAppModule.Core.ViewModels
{
    public class TestContentPageViewModel : BaseControlsRegionViewModel
    {
        public TestContentPageViewModel(ControlsAppContext appContext, IEventAggregator eventAggregator, IRegionManager regionManager, IWpfDeviceRequestService deviceRequestService) : base(appContext,
            eventAggregator, regionManager, deviceRequestService)
        {
            PreviousPageCommand = BindDelegateCommand(GoBack);
        }

        public ICommand PreviousPageCommand { get; }


        protected override void OnLoadedEvent()
        {
        }

        protected override void OnUnloadedEvent()
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
    }
}
