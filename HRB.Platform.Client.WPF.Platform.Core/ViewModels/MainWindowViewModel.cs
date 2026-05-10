//using DM.Platform.Client.WPF.Core.Models;
//using DM.Platform.Client.WPF.Core.Services.IServices;
//using Microsoft.OData;
//using Prism.Commands;
//using Prism.Ioc;
//using Prism.Regions;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Input;

//namespace DM.Platform.Client.WPF.Platform.Core.ViewModels
//{



//    public class MainWindowViewModel : BaseWpfViewModel
//    {

//        //private int _CurrentHashID;//= Guid.NewGuid().GetHashCode();

//        private string _ContentLeftRegionName;

//        public string ContentLeftRegionName
//        {
//            get { return _ContentLeftRegionName; }
//            set
//            {
//                _ContentLeftRegionName = value;
//                RaisePropertyChanged();
//            }
//        }

//        private string _ContentRightRegionName;

//        public string ContentRightRegionName
//        {
//            get { return _ContentRightRegionName; }
//            set
//            {
//                _ContentRightRegionName = value;
//                RaisePropertyChanged();
//            }
//        }


//        private string _TitleMessage;

//        public string TitleMessage
//        {
//            get { return _TitleMessage; }
//            set
//            {
//                SetProperty(ref _TitleMessage, value);
//            }
//        }

//        public ICommand NavigationLeftCommand { get; }
//        public ICommand NavigationRightCommand { get; }
//        public ICommand NewWindowCommand { get; }


//        private readonly IWpfCommonRequestService _CurrentCommonRequestService;
//        private readonly IRegionManager _regionManager;
//        private readonly Prism.Ioc.IContainerProvider _containerProvider;
//        //private readonly RegionManager _regionFullManager;

//        public MainWindowViewModel
//        (
//            Prism.Ioc.IContainerProvider containerProvider,
//            IRegionManager regionManager,
//            IWpfCommonRequestService commonRequestService
//        )
//        {

//            //regionManage.RegisterViewlithRegion

//            _CurrentCommonRequestService = commonRequestService;
//            _regionManager = regionManager;
//            _containerProvider = containerProvider;
//            //_regionFullManager = regionManager as RegionManager;

//            NavigationLeftCommand = new DelegateCommand(OnNavigationLeftCommand);
//            NavigationRightCommand = new DelegateCommand(OnNavigationRightCommand);
//            NewWindowCommand = new DelegateCommand(OnNewWindowCommand);

//            Task.Run(() =>
//            {

//                while (true)
//                {
//                    TitleMessage = string.Format("{0} - [ {1} ]", nameof(TitleMessage), DateTime.Now);
//                    Task.Delay(1 * 1000).Wait();
//                }

//            });


//            var hashID = Guid.NewGuid().GetHashCode();//.ToString();

//            ContentLeftRegionName = nameof(ContentLeftRegionName) + hashID;
//            ContentRightRegionName = nameof(ContentRightRegionName) + hashID;

//            //RegionManager.SetRegionName();

//        }


//        private void OnNewWindowCommand()
//        {

//        }


//        private void OnNavigationLeftCommand()
//        {

//            //_regionManager.RequestNavigate("ContentLeftRegion", "ViewA");
//            _regionManager.RequestNavigate(_ContentLeftRegionName, "ViewA");

//        }

//        private void OnNavigationRightCommand()
//        {

//            //_regionManager.RequestNavigate("ContentRightRegion", "ViewB");
//            _regionManager.RequestNavigate(_ContentRightRegionName, "ViewB");

//        }


//    }


//}
