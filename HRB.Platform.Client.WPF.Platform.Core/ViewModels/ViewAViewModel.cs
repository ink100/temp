//namespace HRB.Platform.Client.WPF.Platform.Core.ViewModels
//{
//    public class ViewAViewModel : BasePlatformRegionViewModel
//    {
//        private string _TitleMessage;

//        public string TitleMessage
//        {
//            get { return _TitleMessage; }
//            set
//            {
//                SetProperty(ref _TitleMessage, value);
//            }
//        }

//        public ViewAViewModel(PlatformWpfAppContext appContext, IEventAggregator eventAggregator, IRegionManager regionManager, IWpfDeviceRequestService deviceRequestService) : base(appContext,
//            eventAggregator, regionManager, deviceRequestService)
//        {
//            Task.Run(() =>
//            {
//                while (true)
//                {
//                    TitleMessage = string.Format("{0} - [ {1} ]", nameof(ViewAViewModel), DateTime.Now);
//                    Task.Delay(1 * 1000).Wait();
//                }
//            });
//        }


//        /// <summary>Called when the implementer has been navigated to.</summary>
//        /// <param name="navigationContext">The navigation context.</param>
//        public override void OnNavigatedTo(NavigationContext navigationContext)
//        {
//        }

//        protected override void OnNavigatedToEvent(NavigationContext navigationContext)
//        {
//            throw new NotImplementedException();
//        }

//        /// <summary>
//        /// Called when the implementer is being navigated away from.
//        /// </summary>
//        /// <param name="navigationContext">The navigation context.</param>
//        public override void OnNavigatedFrom(NavigationContext navigationContext)
//        {
//        }

//        protected override void OnNavigatedFromEvent(NavigationContext navigationContext)
//        {
//        }

//        protected override void OnDestroy()
//        {
//        }
//    }
//}