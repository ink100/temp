//using HRB.Platform.Client.WPF.Core.Services.IServices;
//using System.Windows;
//using System.Windows.Input;

//namespace HRB.Platform.Client.WPF.ControlsAppModule.Core.ViewModels
//{
//    public class BaseDialogViewModel(
//        ControlsAppContext appContext,
//        IEventAggregator eventAggregator,
//        IRegionManager regionManager,
//        IWpfDeviceRequestService deviceRequestService,
//        IPlatformDialogService dialogService)
//        : BaseDialogContentViewModel(appContext,
//            eventAggregator, regionManager, deviceRequestService)
//    {


//        private string _title =
//                GlobalSettings.CurrentAppContext.CurrentTranslator.GetLanguageValue(Languages.LanguageEnums.Hint);

//        private Visibility _okButtonVisibility = Visibility.Visible;
//        private Visibility _cancelButtonVisibility = Visibility.Visible;
//        private string _message;
//        private string _okText;
//        private string _cancelText;
//        private bool _swapPlaces;
//        private int _okButtonIndex = 0;
//        private int _cancelButtonIndex = 1;

//        public Visibility OkButtonVisibility
//        {
//            get => _okButtonVisibility;
//            set => SetProperty(ref _okButtonVisibility, value);
//        }

//        public Visibility CancelButtonVisibility
//        {
//            get => _cancelButtonVisibility;
//            set => SetProperty(ref _cancelButtonVisibility, value);
//        }

//        /// <summary>
//        /// 交换按钮位置
//        /// </summary>
//        public bool SwapPlaces
//        {
//            get => _swapPlaces;
//            set => SetProperty(ref _swapPlaces, value);
//        }


//        public ICommand CloseCommand => BindDelegateCommand<object>(OnDialogClosed);


//        public string Message
//        {
//            get => _message;
//            set => SetProperty(ref _message, value);
//        }




//        public virtual bool CanCloseDialog() => true;

//        private DialogButtonResultEnum _closeResult;

//        public void OnDialogClosed(object obj)
//        {
//            if (CanCloseDialog() && obj is DialogButtonResultEnum buttonResult)
//            {
//                _closeResult = buttonResult;
//                dialogService.CloseDialog();
//                RequestClose?.Invoke(new CloseDialogEto(_closeResult, new DialogParameters()));
//            }
//        }


//        public string Title
//        {
//            get => _title;
//            set => SetProperty(ref _title, value);
//        }

//        public event Action<IDialogCloseModel> RequestClose;


//        protected override void OnDialogClosed()
//        {

//        }



//        public string OkText
//        {
//            get => _okText;
//            set => SetProperty(ref _okText, value);
//        }

//        public string CancelText
//        {
//            get => _cancelText;
//            set => SetProperty(ref _cancelText, value);
//        }

//        public int OkButtonIndex
//        {
//            get => _okButtonIndex;
//            set => SetProperty(ref _okButtonIndex, value);
//        }


//        public int CancelButtonIndex
//        {
//            get => _cancelButtonIndex;
//            set => SetProperty(ref _cancelButtonIndex, value);
//        }

//        public override void OnDialogOpened(ShowDialogEto eto)
//        {


//            if (eto.SwapPlaces)
//            {
//                OkButtonIndex = 1;
//                CancelButtonIndex = 0;
//            }

//            switch (eto.DialogButton)
//            {
//                case DialogButtonEnum.Ok:
//                    CancelButtonVisibility = Visibility.Collapsed;
//                    OkButtonIndex = CancelButtonIndex = 0;
//                    break;
//                case DialogButtonEnum.OkCancel:
//                    break;
//                case DialogButtonEnum.YesNoCancel:
//                    break;
//                case DialogButtonEnum.YesNo:
//                    break;
//            }

//            if (string.IsNullOrEmpty(eto.CancelText))
//            {
//                CancelText = GlobalSettings.CurrentAppContext.CurrentTranslator.GetLanguageValue(Languages.LanguageEnums
//                    .Button_Cancel);
//            }
//            else
//            {
//                CancelText = eto.CancelText;
//            }

//            if (string.IsNullOrEmpty(eto.OkText))
//            {
//                OkText = GlobalSettings.CurrentAppContext.CurrentTranslator.GetLanguageValue(Languages.LanguageEnums
//                    .Button_OK);
//            }
//            else
//            {
//                OkText = eto.OkText;
//            }




//            SwapPlaces = eto.SwapPlaces;

//            Title = eto.Title;
//            Message = eto.ContentString;
//            RequestClose = eto.Callback;
//        }
//    }
//}