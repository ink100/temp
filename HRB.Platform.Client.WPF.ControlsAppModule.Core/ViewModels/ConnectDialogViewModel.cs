//using System.Windows.Input;

//namespace HRB.Platform.Client.WPF.ControlsAppModule.Core.ViewModels
//{


//    public class ConnectDialogViewModel(
//        ControlsAppContext appContext,
//        IEventAggregator eventAggregator,
//        IRegionManager regionManager,
//        IWpfDeviceRequestService deviceRequestService,
//        IPlatformDialogService dialogService)
//        : BaseDialogContentViewModel(appContext, eventAggregator, regionManager, deviceRequestService)
//    {

//        private ShowDialogEto _dialogArgs;


//        public ICommand CloseDialogCommand => BindDelegateCommand(dialogService.CloseDialog);


//        public ICommand WifiCommand => BindDelegateCommand(() => OnCommand(false));

//        public ICommand UsbCommand => BindDelegateCommand(() => OnCommand(true));



//        public override void OnDialogOpened(ShowDialogEto eto)
//        {
//            _dialogArgs = eto;
//        }

//        protected override void OnDialogClosed()
//        {
//            // _dialogArgs?.Callback?.Invoke(new CloseDialogEto());
//        }


//        private void OnCommand(bool isUSB)
//        {


//            _dialogArgs?.Callback?.Invoke
//            (
//                new CloseDialogEto(DialogButtonResultEnum.Ok, new DialogParameters
//                { {
//                    EquipmentConnectionModeKeys.EQUIPMENT_CONNECTION_PARAMETER_KEY_NAME,isUSB
//                },})
//            );

//            dialogService.CloseDialog();

//        }


//    }
//}
