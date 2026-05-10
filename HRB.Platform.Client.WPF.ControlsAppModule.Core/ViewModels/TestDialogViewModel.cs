//using HRB.Platform.Client.WPF.Core.Services.IServices;

//namespace HRB.Platform.Client.WPF.ControlsAppModule.Core.ViewModels
//{
//    public class TestDialogViewModel : BaseControlsUserControlViewModel, IDialogContentAware
//    {
//        public TestDialogViewModel(ControlsAppContext appContext, IEventAggregator eventAggregator, IRegionManager regionManager, IWpfDeviceRequestService deviceRequestService) : base(appContext,
//            eventAggregator, regionManager, deviceRequestService)
//        {
//        }


//        public IDialogResult CloseCallback(ButtonResult buttonResult)
//        {
//            var result = new DialogResult(buttonResult);

//            result.Parameters.Add("Content", "被关闭了");

//            return result;
//        }

//        public void CloseCallback()
//        {
//            // throw new NotImplementedException();
//        }

//        public DialogButtonEnum DialogButtonType { get; set; }

//        public void OnDialogOpened(ShowDialogEto eto)
//        {
//        }
//    }
//}