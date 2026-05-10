using HRB.Platform.Client.WPF.Core.Instruments;
using HRB.Platform.Client.WPF.Core.Services.IServices;
using System.Windows.Input;
using DelegateCommand = Prism.Commands.DelegateCommand;

namespace HRB.Platform.Client.WPF.Platform.Core.ViewModels
{
    public class ContentRegionViewModel : BasePlatformRegionViewModel
    {
        //private readonly ILoadingService _loadingService;

        //private readonly ITaskNotifyServices _taskNotifyServices;

        //private readonly IPlatformDialogService _platformDialogService;

        //  private readonly IDialogService _dialogService;

        public ICommand ShowNotifyCommand { get; }

        public ICommand ShowLoadingCommand { get; }

        public ICommand ShowDialogCommand { get; }

        public ICommand NextPageCommand { get; }
        public ICommand ToRootCommand { get; }


        public ContentRegionViewModel(PlatformWpfAppContext appContext, IEventAggregator eventAggregator, IRegionManager regionManager,
            IWpfDeviceRequestService deviceRequestService
           /* ILoadingService loadingService, ITaskNotifyServices taskNotifyServices, IPlatformDialogService platformDialogService, IDialogService dialogService*/) : base(
            appContext, eventAggregator, regionManager, deviceRequestService)
        {

            //_loadingService = loadingService;
            //_taskNotifyServices = taskNotifyServices;
            //_platformDialogService = platformDialogService;
            //// _dialogService = dialogService;
            //ShowNotifyCommand = new DelegateCommand(ShowNotify);
            //ShowLoadingCommand = new DelegateCommand(ShowLoading);
            //ShowDialogCommand = new DelegateCommand(ShowDialog);
            //NextPageCommand = BindDelegateCommand(() =>
            //{
            //    NavigateRegion<TestContentPageViewModel>(RegionNames.ContentRegion, ControlsAppModule.Core.GlobalSettings.APP_MODULE_NAME);
            //});
            //ToRootCommand = BindDelegateCommand(() =>
            //{
            //    NavigationToRoot<TestContentPageViewModel>(RegionNames.ContentRegion, ControlsAppModule.Core.GlobalSettings.APP_MODULE_NAME, (result) => { }, new NavigationParameters());
            //});

        }

        private void ShowDialog()
        {
            //显示控件
            //var model = new ShowDialogEto("这是title", nameof(TestDialog), typeof(TestDialog), null, (value) =>
            //{
            //    Debug.Write(JsonConvert.SerializeObject(value));
            //});

            //显示文字
            //var model = new ShowDialogEto("这是title", "Dialog内容", null, DialogButtonEnum.Ok, (value) =>
            //{
            //    Debug.Write(JsonConvert.SerializeObject(value));
            //});


            //  _platformDialogService.ShowDialog(model);
        }


        //private void ShowLoading()
        //{
        //    var btnShowLoading = GetControlByNameFromView<RadButton>("btnShowLoading");
        //    var btnShowLoading1 = GetControlByNameFromView<RadButton>("btnShowLoading1");
        //    _loadingService.ShowLoading(new ShowLoadingEto("Loading的内容"));
        //}

        //private int _index = 0;

        //private void ShowNotify()
        //{
        //    _taskNotifyServices.Notify(new TaskNotifyEto($"Show{_index}"));
        //    _index++;
        //}


    }
}