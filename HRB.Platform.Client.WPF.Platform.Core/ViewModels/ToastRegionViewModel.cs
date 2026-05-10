//using System.Collections.ObjectModel;
//using System.Windows;

//namespace HRB.Platform.Client.WPF.Platform.Core.ViewModels
//{
//    public class ToastRegionViewModel : BaseToast
//    {

//        public ToastRegionViewModel(PlatformWpfAppContext appContext, IEventAggregator eventAggregator, IRegionManager regionManager, IWpfDeviceRequestService deviceRequestService) : base(appContext, eventAggregator, regionManager, deviceRequestService)
//        {
//        }
//        public ObservableCollection<ToastView> Toasts { get; } = new();


//        protected override void OnCloseToast(ToastToken obj)
//        {
//            var notifyCard = Toasts.FirstOrDefault(c =>
//                (c.DataContext is ToastViewViewModel dc) && dc.Token.Equals(obj));
//            Toasts.Remove(notifyCard);
//        }

//        protected override void OnShowToast(IToastAware obj)
//        {
//            var toast = new ToastView();

//            (toast.DataContext as ToastViewViewModel)?.SetValues(obj);

//            Toasts.Add(toast);

//            if (Toasts.Count > 1)
//            {
//                var controlHeight = Toasts[0].ActualHeight;
//                var totalHeight = controlHeight * Toasts.Count;
//                var actualHeight = Application.Current.MainWindow?.ActualHeight;
//                if (totalHeight > actualHeight)
//                {
//                    Toasts.RemoveAt(0);
//                }
//            }
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
