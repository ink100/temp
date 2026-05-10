//using DM.Platform.Client.WPF.Platform.Core;
//using System.Windows;
//using System.Windows.Controls;

//namespace HRB.Platform.Client.WPF.Platform.Core.ViewModels
//{
//    public class DialogRegionViewModel : BasePlatformDialog
//    {
//        private readonly IContainerProvider _containerProvider;

//        private Visibility _dialogRegionVisibility = Visibility.Collapsed;
//        private Control _dialogContent;


//        //public Action<CloseDialogEto> RequestClose
//        //{
//        //    get;
//        //    set;
//        //}


//        public DialogRegionViewModel(PlatformWpfAppContext appContext, IEventAggregator eventAggregator,
//            IRegionManager regionManager, IWpfDeviceRequestService deviceRequestService) : base(appContext,
//            eventAggregator, regionManager, deviceRequestService)
//        {
//            _containerProvider = GlobalSettings.CurrentAppContext.CurrentContainerProvider;
//        }


//        protected override void CloseDialog()
//        {
//            DialogRegionVisibility = Visibility.Collapsed;
//        }

//        protected override void ShowDialog(ShowDialogEto eto)
//        {
//            var content = _containerProvider.Resolve(eto.DialogContentType, eto.DialogContentName);

//            if (content is Control { DataContext: IDialogContentAware dc } c)
//            {
//                DialogContent = c;
//                DialogRegionVisibility = Visibility.Visible;
//                dc.OnDialogOpened(eto);
//            }
//        }


//        public Control DialogContent
//        {
//            get => _dialogContent;
//            set => SetProperty(ref _dialogContent, value);
//        }


//        public Visibility DialogRegionVisibility
//        {
//            get => _dialogRegionVisibility;
//            set => SetProperty(ref _dialogRegionVisibility, value);
//        }
//    }
//}