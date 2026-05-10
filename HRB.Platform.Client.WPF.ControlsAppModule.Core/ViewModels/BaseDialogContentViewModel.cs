//using HRB.Platform.Client.WPF.Core.Services.IServices;

//namespace HRB.Platform.Client.WPF.ControlsAppModule.Core.ViewModels
//{
//    public abstract class BaseDialogContentViewModel : BaseControlsUserControlViewModel, IDialogContentAware
//    {
//        protected BaseDialogContentViewModel(ControlsAppContext appContext, IEventAggregator eventAggregator, IRegionManager regionManager, IWpfDeviceRequestService deviceRequestService) : base(appContext, eventAggregator, regionManager, deviceRequestService)
//        {
//            eventAggregator.GetEvent<CloseDialogEvent>().Subscribe(OnDialogClosed);
//        }



//        public abstract void OnDialogOpened(ShowDialogEto eto);

//        protected abstract void OnDialogClosed();

//        public DialogButtonEnum DialogButtonType { get; set; }
//    }
//}
