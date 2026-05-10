using HRB.Platform.Client.WPF.Core.Models;
using HRB.Platform.Client.WPF.Core.Services.IServices;
using Prism.Navigation.Regions;
using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.ViewModels
{


    public abstract class BasePaymentRegionViewModel : BaseRegionViewModel
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IRegionManager _regionManager;

        //protected virtual void EcgNavigateRegion<TViewModel>(Action<NavigationResult> navigationCallback = null, NavigationParameters navigationParameters = null)
        //    where TViewModel : BaseRegionViewModel
        //{
        //    //NavigateRegion<TViewModel>(RegionNames.ContentRegion, GlobalSettings.APP_MODULE_NAME, navigationCallback, navigationParameters);

        //    _navigationJournal = regionManager.Regions[PaymentRegionNames.PaymentContentRegion].NavigationService.Journal;


        //}




        protected BasePaymentRegionViewModel(PaymentAppContext appContext,
            IEventAggregator eventAggregator,
            IRegionManager regionManager,
            IWpfDeviceRequestService deviceRequestService) : base(appContext, eventAggregator, regionManager, deviceRequestService)
        {
            _eventAggregator = eventAggregator;
            _regionManager = regionManager;
        }


        protected virtual void PaymentGoBack()
        {
            var navigationJournal = _regionManager.Regions[PaymentRegionNames.PaymentContentRegion].NavigationService.Journal;
            navigationJournal.GoBack();
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