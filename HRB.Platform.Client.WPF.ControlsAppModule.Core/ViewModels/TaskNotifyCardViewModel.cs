//using HRB.Platform.Client.WPF.Core.Services.IServices;
//using System.Windows;
//using System.Windows.Input;

//namespace HRB.Platform.Client.WPF.ControlsAppModule.Core.ViewModels
//{
//    public class TaskNotifyCardViewModel : BaseControlsUserControlViewModel, ITaskNotifyAware
//    {
//        private readonly ITaskNotifyServices _taskNotifyServices;


//        #region Command

//        public ICommand CloseNotifyCommand { get; }

//        private void OnCloseNotify()
//        {
//            _taskNotifyServices.CloseNotify(Token);
//            _autoCloseTimer?.Dispose();
//        }

//        #endregion

//        private Timer _autoCloseTimer;

//        public TaskNotifyCardViewModel(ControlsAppContext appContext, IEventAggregator eventAggregator,
//            IRegionManager regionManager, IWpfDeviceRequestService deviceRequestService,
//            ITaskNotifyServices taskNotifyServices) : base(appContext,
//            eventAggregator, regionManager, deviceRequestService)
//        {
//            _taskNotifyServices = taskNotifyServices;

//            CloseNotifyCommand = new DelegateCommand(OnCloseNotify);
//        }


//        #region fields

//        private string _message;

//        public string Message
//        {
//            get => _message;
//            set => SetProperty(ref _message, value);
//        }


//        public TaskNotifyToken Token { get; set; }
//        public bool CanAutoCloseTaskNotify { get; set; }

//        public TimeSpan AutoCloseTime { get; set; }

//        #endregion

//        #region methods

//        public void SetValues(ITaskNotifyAware model)
//        {
//            Token = model.Token;
//            Message = model.Message;
//            CanAutoCloseTaskNotify = model.CanAutoCloseTaskNotify;
//            AutoCloseTime = model.AutoCloseTime;

//            if (CanAutoCloseTaskNotify)
//            {
//                _autoCloseTimer = new Timer(_ =>
//                {
//                    Application.Current.Dispatcher.InvokeAsync(OnCloseNotify);
//                }, null, AutoCloseTime, TimeSpan.Zero);
//            }
//        }

//        #endregion

//        protected override void OnLoadedEvent()
//        {
//        }

//        protected override void OnUnloadedEvent()
//        {
//        }
//    }
//}
