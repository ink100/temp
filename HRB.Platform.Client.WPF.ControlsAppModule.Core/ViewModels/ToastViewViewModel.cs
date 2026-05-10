//using System.Windows;

//namespace HRB.Platform.Client.WPF.ControlsAppModule.Core.ViewModels
//{
//    public class ToastViewViewModel : BaseControlsUserControlViewModel, IToastAware
//    {
//        private readonly IToastService _toastService;

//        public ToastViewViewModel(ControlsAppContext appContext, IEventAggregator eventAggregator,
//            IRegionManager regionManager, IWpfDeviceRequestService deviceRequestService,
//            IToastService toastService) : base(appContext,
//            eventAggregator, regionManager, deviceRequestService)
//        {
//            _toastService = toastService;
//        }

//        public string Message { get; set; }

//        public ToastTypeEnum ToastType
//        {
//            get => _toastType;
//            set
//            {
//                ToastIcon = value switch
//                {
//                    ToastTypeEnum.None => "",
//                    ToastTypeEnum.Success => "\ue648",
//                    ToastTypeEnum.Fail => "\ue63d",
//                    ToastTypeEnum.Warn => "\ue60a",
//                    _ => ToastIcon
//                };

//                SetProperty(ref _toastType, value);
//            }
//        }

//        public TimeSpan ShowTimeInterval { get; set; }
//        public ToastToken Token { get; set; }

//        private Timer _timer;
//        private string _toastIcon = "";
//        private ToastTypeEnum _toastType;

//        public string ToastIcon
//        {
//            get => _toastIcon;
//            set => SetProperty(ref _toastIcon, value);
//        }


//        public void SetValues(IToastAware aware)
//        {
//            this.Message = aware.Message;
//            this.ToastType = aware.ToastType;
//            this.Token = aware.Token;
//            this.ShowTimeInterval = aware.ShowTimeInterval;


//            _timer = new Timer(_ =>
//            {
//                Application.Current.Dispatcher.InvokeAsync(OnCloseToast);
//            }, null, ShowTimeInterval, TimeSpan.Zero);
//        }

//        private void OnCloseToast()
//        {
//            _toastService.CloseToast(Token);
//            _timer?.Dispose();
//        }
//    }
//}
