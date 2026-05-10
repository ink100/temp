//using System.Collections.ObjectModel;
//using System.Windows;

//namespace HRB.Platform.Client.WPF.Platform.Core.ViewModels
//{
//    public class TaskNotifyRegionViewModel : BaseTaskNotify
//    {
//        public TaskNotifyRegionViewModel(PlatformWpfAppContext appContext, IEventAggregator eventAggregator,
//            IRegionManager regionManager, IWpfDeviceRequestService deviceRequestService) : base(
//            appContext, eventAggregator, regionManager, deviceRequestService)
//        {
//        }


//        public ObservableCollection<TaskNotifyCard> TaskNotifyCards { get; } = new();


//        protected override void OnCloseTaskNotify(TaskNotifyToken obj)
//        {
//            var notifyCard = TaskNotifyCards.FirstOrDefault(c => (c.DataContext is TaskNotifyCardViewModel dc) && dc.Token.Equals(obj));
//            TaskNotifyCards.Remove(notifyCard);
//        }

//        protected override void OnShowTaskNotify(ITaskNotifyAware obj)
//        {
//            var taskNotifyCard = new TaskNotifyCard();

//            (taskNotifyCard.DataContext as TaskNotifyCardViewModel)?.SetValues(obj);

//            TaskNotifyCards.Add(taskNotifyCard);

//            if (TaskNotifyCards.Count > 1)
//            {
//                var controlHeight = TaskNotifyCards[0].ActualHeight;
//                var totalHeight = controlHeight * TaskNotifyCards.Count;
//                var actualHeight = Application.Current.MainWindow?.ActualHeight;
//                if (totalHeight > actualHeight)
//                {
//                    TaskNotifyCards.RemoveAt(0);
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