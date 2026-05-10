using HRB.Payment.Core;
using HRB.Platform.Client.WPF.Core.ExtensionFunctions;
using HRB.Platform.Client.WPF.Core.Helpers;
using HRB.Platform.Client.WPF.Core.Instruments;
using HRB.Platform.Client.WPF.Core.Instruments.Abstractions;
using HRB.Platform.Client.WPF.Core.Models;
using HRB.Platform.Client.WPF.Core.Services.IServices;

namespace HRB.Platform.Client.WPF.Platform.Core.ViewModels
{


    public abstract class BaseShellViewModel : BaseWpfViewModel, IUserControlLifecycleAware
    {



        protected BaseShellViewModel(PlatformWpfAppContext appContext, IEventAggregator eventAggregator, IRegionManager regionManager, IWpfDeviceRequestService deviceRequestService) : base(appContext, eventAggregator, regionManager, deviceRequestService)
        {
            OnInitialized();
        }

        internal void OnInitialized()
        {

            //_regionManager.RequestNavigate(RegionNames.ContentRegion, Pages.ContentRegion);
            //_regionManager.RequestNavigate(RegionNames.TaskNotifyRegion, Pages.TaskNotifyRegion);
            //_regionManager.RequestNavigate(RegionNames.LoadingRegion, Pages.LoadingRegion);

            //NavigateRegion<ContentRegionViewModel>(RegionNames.ContentRegion);

            // NavigateRegion<>();


            var platformWpfAppContext = _CurrentAppContext as PlatformWpfAppContext;



            _CurrentRegionManager.NavigateRegionToStartEntryAppModule(platformWpfAppContext, RegionNames.ContentRegion);

            //NavigateRegion<TaskNotifyRegionViewModel>(RegionNames.TaskNotifyRegion);
            //NavigateRegion<LoadingRegionViewModel>(RegionNames.LoadingRegion);
            //NavigateRegion<DialogRegionViewModel>(RegionNames.DialogRegion);
            //NavigateRegion<ToastRegionViewModel>(RegionNames.ToastRegion);


        }


        public void OnLoaded(object sender)
        {

            var platformWpfAppContext = _CurrentAppContext as PlatformWpfAppContext;
            var platformStartUpArgsDto = platformWpfAppContext.CurrentStartUpArgs;

            if (platformStartUpArgsDto.IsFullScreen)
            {
                OtherWpfHelper.SetFullScreen();
            }

        }

        public void OnUnloaded(object sender)
        {

        }


    }

}