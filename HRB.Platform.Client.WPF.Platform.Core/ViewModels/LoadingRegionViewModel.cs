//namespace HRB.Platform.Client.WPF.Platform.Core.ViewModels
//{
//    public class LoadingRegionViewModel : BaseLoading
//    {
//        public LoadingRegionViewModel(PlatformWpfAppContext appContext, IEventAggregator eventAggregator,
//            IRegionManager regionManager, IWpfDeviceRequestService deviceRequestService) : base(
//            appContext, eventAggregator, regionManager, deviceRequestService)
//        {
//            _eventAggregator = eventAggregator;
//        }

//        private readonly IEventAggregator _eventAggregator;

//        public object Content
//        {
//            get => _content;
//            set => SetProperty(ref _content, value);
//        }

//        private object _content = "Loading...";

//        private Action _timeOutCallback;


//        protected override void CloseLoading()
//        {
//            _eventAggregator.GetEvent<LoadingTimeoutEvent>().Publish();

//            IsShowLoading = false;

//            if (_timeOutTimer != null)
//            {
//                _timeOutTimer?.Change(Timeout.Infinite, Timeout.Infinite);
//                _timeOutTimer = null;
//                // _timeOutTimer.Dispose();
//            }
//        }

//        private Timer _timeOutTimer;

//        protected override void ShowLoading(IShowLoadingModel obj)
//        {
//            IsShowLoading = true;
//          //  Debug.Write($"Current loading status :{IsShowLoading}");
//            Content = obj.Content;
//            _timeOutCallback = obj.TimeOutCallback;
//            if (obj.TimeOutInterval != 0)
//            {
//                _timeOutTimer = new Timer(_ => LoadingTimeOutCallback(), null, obj.TimeOutInterval, Timeout.Infinite);
//            }
//        }

//        private void LoadingTimeOutCallback()
//        {
//            _timeOutCallback?.Invoke();
//            _timeOutCallback = null;
//            CloseLoading();
//        }


//        protected override void OnLoadedEvent()
//        {
//        }

//        protected override void OnUnloadedEvent()
//        {
//        }

//        protected override void OnNavigatedToEvent(NavigationContext navigationContext)
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