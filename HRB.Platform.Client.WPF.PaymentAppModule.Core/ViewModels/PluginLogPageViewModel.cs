using HRB.Platform.Client.WPF.Core.Services.IServices;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.DboModels;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Repository;
using Prism.Commands;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.ViewModels
{
    /// <summary>
    /// 插件日志页面视图模型
    /// </summary>
    public class PluginLogPageViewModel : BasePaymentRegionViewModel, INavigationAware
    {
        private readonly IPaymentRepository _repository;
        private readonly IRegionManager _regionManager;

        // 分页参数
        private const int PAGE_SIZE = 20;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _totalCount = 0;

        // UI绑定字段
        private ObservableCollection<PluginLogModel> _logs = new();
        private string _selectedPluginFilter = "全部";
        private bool _isLoading;
        private string _pageInfo = "第 1 页，共 1 页";

        public PluginLogPageViewModel(
            PaymentAppContext appContext,
            IEventAggregator eventAggregator,
            IRegionManager regionManager,
            IWpfDeviceRequestService deviceRequestService,
            IPaymentRepository repository
        ) : base(appContext, eventAggregator, regionManager, deviceRequestService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));

            // 初始化命令
            LoadLogsCommand = new DelegateCommand(async () => await LoadLogsAsync(), () => !IsLoading)
                .ObservesProperty(() => IsLoading);
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());
            NavigateToMainCommand = new DelegateCommand(NavigateToMain);
            FirstPageCommand = new DelegateCommand(async () => await GoToFirstPageAsync(), () => CurrentPage > 1)
                .ObservesProperty(() => CurrentPage);
            PreviousPageCommand = new DelegateCommand(async () => await GoToPreviousPageAsync(), () => CurrentPage > 1)
                .ObservesProperty(() => CurrentPage);
            NextPageCommand = new DelegateCommand(async () => await GoToNextPageAsync(), () => CurrentPage < TotalPages)
                .ObservesProperty(() => CurrentPage)
                .ObservesProperty(() => TotalPages);
            LastPageCommand = new DelegateCommand(async () => await GoToLastPageAsync(), () => CurrentPage < TotalPages)
                .ObservesProperty(() => CurrentPage)
                .ObservesProperty(() => TotalPages);
        }

        #region 属性

        /// <summary>
        /// 日志列表
        /// </summary>
        public ObservableCollection<PluginLogModel> Logs
        {
            get => _logs;
            set => SetProperty(ref _logs, value);
        }

        /// <summary>
        /// 选中的插件筛选
        /// </summary>
        public string SelectedPluginFilter
        {
            get => _selectedPluginFilter;
            set
            {
                if (SetProperty(ref _selectedPluginFilter, value))
                {
                    _ = RefreshAsync();
                }
            }
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                SetProperty(ref _isLoading, value);
                LoadLogsCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    UpdatePageInfo();
                    FirstPageCommand.RaiseCanExecuteChanged();
                    PreviousPageCommand.RaiseCanExecuteChanged();
                    NextPageCommand.RaiseCanExecuteChanged();
                    LastPageCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages
        {
            get => _totalPages;
            set
            {
                if (SetProperty(ref _totalPages, value))
                {
                    UpdatePageInfo();
                    NextPageCommand.RaiseCanExecuteChanged();
                    LastPageCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount
        {
            get => _totalCount;
            set
            {
                if (SetProperty(ref _totalCount, value))
                {
                    UpdatePageInfo();
                }
            }
        }

        /// <summary>
        /// 页面信息
        /// </summary>
        public string PageInfo
        {
            get => _pageInfo;
            set => SetProperty(ref _pageInfo, value);
        }

        /// <summary>
        /// 插件筛选选项
        /// </summary>
        public ObservableCollection<string> PluginFilterOptions { get; } = new()
        {
            "全部",
            "MessageService",
            "AlipayShell",
            "AlipayConfigClient",
            "WeChatShell",
            "System"
        };

        #endregion

        #region 命令

        public DelegateCommand LoadLogsCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand NavigateToMainCommand { get; }
        public DelegateCommand FirstPageCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }
        public DelegateCommand NextPageCommand { get; }
        public DelegateCommand LastPageCommand { get; }

        #endregion

        #region 方法

        /// <summary>
        /// 加载日志
        /// </summary>
        private async Task LoadLogsAsync()
        {
            IsLoading = true;
            try
            {
                IEnumerable<PluginLogModel> allLogs;

                // 根据筛选条件获取日志
                if (SelectedPluginFilter == "全部")
                {
                    allLogs = await _repository.GetAllPluginLogs(10000); // 获取足够多的数据用于分页
                }
                else
                {
                    allLogs = await _repository.GetPluginLogs(SelectedPluginFilter, 10000);
                }

                // 计算分页
                var logsList = allLogs.ToList();
                TotalCount = logsList.Count;
                TotalPages = (int)Math.Ceiling((double)TotalCount / PAGE_SIZE);

                // 确保当前页在有效范围内
                if (CurrentPage > TotalPages && TotalPages > 0)
                {
                    CurrentPage = TotalPages;
                }
                else if (CurrentPage < 1)
                {
                    CurrentPage = 1;
                }

                // 获取当前页的数据
                var pagedLogs = logsList
                    .Skip((CurrentPage - 1) * PAGE_SIZE)
                    .Take(PAGE_SIZE)
                    .ToList();

                // 更新UI
                Logs.Clear();
                foreach (var log in pagedLogs)
                {
                    Logs.Add(log);
                }

                Debug.WriteLine($"[PluginLogPageViewModel] 加载日志成功: 总数={TotalCount}, 当前页={CurrentPage}/{TotalPages}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PluginLogPageViewModel] 加载日志失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 刷新数据
        /// </summary>
        private async Task RefreshAsync()
        {
            CurrentPage = 1;
            await LoadLogsAsync();
        }

        /// <summary>
        /// 跳转到第一页
        /// </summary>
        private async Task GoToFirstPageAsync()
        {
            CurrentPage = 1;
            await LoadLogsAsync();
        }

        /// <summary>
        /// 跳转到上一页
        /// </summary>
        private async Task GoToPreviousPageAsync()
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                await LoadLogsAsync();
            }
        }

        /// <summary>
        /// 跳转到下一页
        /// </summary>
        private async Task GoToNextPageAsync()
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                await LoadLogsAsync();
            }
        }

        /// <summary>
        /// 跳转到最后一页
        /// </summary>
        private async Task GoToLastPageAsync()
        {
            CurrentPage = TotalPages;
            await LoadLogsAsync();
        }

        /// <summary>
        /// 更新页面信息
        /// </summary>
        private void UpdatePageInfo()
        {
            if (TotalPages == 0)
            {
                PageInfo = "暂无数据";
            }
            else
            {
                PageInfo = $"第 {CurrentPage} 页，共 {TotalPages} 页（总计 {TotalCount} 条）";
            }
        }

        /// <summary>
        /// 返回主页
        /// </summary>
        private void NavigateToMain()
        {
            PaymentGoBack();
        }

        #endregion

        #region INavigationAware

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 导航到此视图时加载数据
            _ = LoadLogsAsync();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 导航离开时的清理工作（如果需要）
        }

        #endregion
    }
}
