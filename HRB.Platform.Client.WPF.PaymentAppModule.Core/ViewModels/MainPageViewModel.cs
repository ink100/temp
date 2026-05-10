using HRB.Payment.Core.Events;
using HRB.Payment.Core.Helpers;
using HRB.Payment.Core.Models;
using HRB.Payment.Core.Services;
using HRB.Payment.Message.Client.BusEvents;
using HRB.Payment.Message.Core.BusEvents;
using HRB.Platform.Client.Core.ExtensionFunctions;
using HRB.Platform.Client.Core.Interfaces;
using HRB.Platform.Client.WPF.Core.Services.IServices;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Configuration;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.DboModels;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Extensions;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Repository;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.ViewModels
{
    /// <summary>
    /// 主页面视图模型
    /// 负责：UI状态管理、支付事件订阅、命令处理、数据加载
    /// </summary>
    public partial class MainPageViewModel : BasePaymentRegionViewModel
    {
        #region 字段

        // 服务依赖
        private readonly PaymentAppContext _appContext;
        private readonly IEventAggregator _eventAggregator;
        private readonly IWeChatConnectionService _weChatConnectionService;
        private readonly IWeChatService _weChatService;
        private readonly IPaymentNotificationHandler _paymentNotificationHandler;
        private readonly IPluginProcessService _pluginProcessService;
        private readonly IPaymentRepository _paymentRepository;
        private readonly INotificationService _notificationService;
        private readonly IDialogService _dialogService;
        private readonly IPaymentVoiceService _voiceService;
        private readonly IOrderStateManager _orderStateManager;
        private readonly ITimerCoordinator _timerCoordinator;
        private readonly IPaymentChannelCoordinator _channelCoordinator;
        private readonly IPluginRuntimeStatusService _pluginRuntimeStatusService;

        // 状态字段
        private bool _isPaymentInProgress = false;
        //   private PaymentEventArgs? _currentPaymentArgs;

        // UI绑定字段
        //  private readonly ObservableCollection<TransactionRecord> _transactions = new();
        private DispatcherTimer? _timer;

        // Avoid re-entrant initialization and repeated WeChat starts.
        private int _newModeInitStartedFlag = 0;
        private int _weChatStartRequestedFlag = 0;
        private int _weChatEnsureStartOnEnterFlag = 0;

        // Unified WeChat monitoring - 一个循环搞定所有事情
        private CancellationTokenSource? _weChatMonitorCts;
        private bool _isWeChatMonitoring = false;
        private bool _pluginIsWorking = false;


        #endregion

        #region 属性

        /// <summary>
        /// 视图模型保持活动状态
        /// </summary>
        public override bool KeepAlive { get; } = true;

        /// <summary>
        /// 交易记录集合（所有数据）
        /// </summary>
        public ObservableCollection<TransactionRecord> Transactions { get; } = new();


        /// <summary>
        /// 微信是否已连接
        /// </summary>
        public bool IsWeChatConnected
        {
            get;
            set => SetProperty(ref field, value);
        } = false;

        public bool IsMessageCenterRunning
        {
            get;
            set => SetProperty(ref field, value);
        } = false;

        public DateTime? MessageCenterLastSeenAt
        {
            get;
            set => SetProperty(ref field, value);
        }

        public bool IsAlipayPluginRunning
        {
            get;
            set => SetProperty(ref field, value);
        } = false;

        public DateTime? AlipayLastReplyAt
        {
            get;
            set => SetProperty(ref field, value);
        }

        public bool IsWeChatPluginRunning
        {
            get;
            set => SetProperty(ref field, value);
        } = false;

        public DateTime? WeChatLastReplyAt
        {
            get;
            set => SetProperty(ref field, value);
        }

        public bool IsConnectingService
        {
            get;
            set => SetProperty(ref field, value);
        } = false;

        /// <summary>
        /// 基础字体大小
        /// </summary>
        public double BaseFontSize
        {
            get;
            set
            {
                if (SetProperty(ref field, value))
                {
                    // 行高 = 字体大小 * 1.5 + 20（上下padding）
                    RowHeight = value * 1.5 + 20;

                    // 保存字体大小
                    var settings = _appContext.CurrentSettings;
                    settings.FontSize = value;
                    settings.LastUpdateDateTime = DateTime.Now;
                    _CurrentAppContext.SaveCurrentSettings(settings);

                    AmountFontSize = value * 2;

                    RaisePropertyChanged(nameof(AmountFontSize));
                }
            }
        } = 60;

        /// <summary>
        /// 金额字体大小（比基础字体大5号）
        /// </summary>
        public double AmountFontSize
        {
            get;
            set => SetProperty(ref field, value);
        } = 120;

        /// <summary>
        /// 行高
        /// </summary>
        public double RowHeight
        {
            get;
            set => SetProperty(ref field, value);
        } = 110;

        /// <summary>
        /// 当前时间（网络时间）
        /// </summary>
        public string CurrentTime
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;

        /// <summary>
        /// 店铺名称
        /// </summary>
        public string StoreName
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;

        /// <summary>
        /// 维护人员联系方式
        /// </summary>
        public string MaintenanceContact
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;


        /// <summary>
        /// 微信进程ID
        /// </summary>
        private int _weChatProcessId = -1;

        private volatile bool _alipayIsRunning = false;
        private volatile bool _appStarted = false;
        private IHttpNotificationService _httpNotificationService;

        // 命令属性
        public ICommand NavigateToHistoryCommand { get; }
        public ICommand NavigateToSettingsCommand { get; }
        public ICommand NavigateToLogCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand SetSmallFontCommand { get; }
        public ICommand SetMediumFontCommand { get; }
        public ICommand SetLargeFontCommand { get; }

        private readonly IHrbLogger _log;

        #endregion

        #region 构造函数

        private readonly IPaymentEventPublisher _eventPublisher;
        public bool IsNewPluginLifecycleModeEnabled =>
            PluginLifecycleModeHelper.IsNewModeEnabled(_appContext.CurrentSettings);


        public MainPageViewModel(
            PaymentAppContext appContext,
            IEventAggregator eventAggregator,
            IRegionManager regionManager,
            IWeChatConnectionService weChatConnectionService,
            IWeChatService weChatService,
            IPaymentNotificationHandler paymentNotificationHandler,
            IPluginProcessService pluginProcessService,

            IWpfDeviceRequestService wpfDeviceRequestService,
            IPaymentRepository paymentRepository,
            IPaymentEventPublisher eventPublisher,
            INotificationService notificationService,
            IDialogService dialogService,
            IPaymentVoiceService voiceService,
            IOrderStateManager orderStateManager,
            ITimerCoordinator timerCoordinator,
            IPaymentChannelCoordinator channelCoordinator,
            IHttpNotificationService httpNotificationService,
            IPluginRuntimeStatusService pluginRuntimeStatusService) : base(appContext, eventAggregator, regionManager, wpfDeviceRequestService)
        {
            _appContext = appContext;
            _eventAggregator = eventAggregator;
            _weChatConnectionService = weChatConnectionService;
            _weChatService = weChatService;
            _paymentNotificationHandler = paymentNotificationHandler;
            _pluginProcessService = pluginProcessService;
            _paymentRepository = paymentRepository;
            _eventPublisher = eventPublisher;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _voiceService = voiceService;
            _orderStateManager = orderStateManager;
            _timerCoordinator = timerCoordinator;
            _channelCoordinator = channelCoordinator;
            _httpNotificationService = httpNotificationService;
            _pluginRuntimeStatusService = pluginRuntimeStatusService;
            _pluginRuntimeStatusService.StatusChanged += (_, __) =>
            {
                Application.Current?.Dispatcher.BeginInvoke(RefreshPluginStatusFromService);
            };

            // 订阅订单状态管理事件
            _orderStateManager.OrderTimeout += OnOrderTimeout;
            _orderStateManager.OrderNotification += OnOrderNotification;


            //_eventAggregator.GetEvent<APModuleToUIEvent>().Subscribe(OnApToUi);
            // 加载保存的字体大小
            BaseFontSize = _appContext.CurrentSettings.FontSize;
            StoreName = _appContext.CurrentSettings.StoreName;
            MaintenanceContact = _appContext.CurrentSettings.MaintenanceContact;

            // 初始化命令
            NavigateToHistoryCommand = new DelegateCommand(NavigateToHistory);
            NavigateToSettingsCommand = new DelegateCommand(NavigateToSettings);
            NavigateToLogCommand = new DelegateCommand(NavigateToLog);
            ExitCommand = new DelegateCommand(Exit);
            SetSmallFontCommand = new DelegateCommand(() => BaseFontSize = 32);
            SetMediumFontCommand = new DelegateCommand(() => BaseFontSize = 48);
            SetLargeFontCommand = new DelegateCommand(() => BaseFontSize = 60);

            _log = appContext.CurrentLogger;

#if DEBUG
            // 初始化测试命令
            InitializeTestCommands();
#endif

            // 订阅事件
            SubscribeToEvents();

            // 加载数据
            _ = LoadDataAsync();

            // 启动定时器
            StartTimers();

            // Initialize plugin/runtime status snapshot
            RefreshPluginStatusFromService();
            RaisePropertyChanged(nameof(IsNewPluginLifecycleModeEnabled));

            //_notificationService.ShowError("测试Error");
            //_notificationService.ShowWarning("测试Warning");
            //_notificationService.ShowInfo("测试Info");
            //_notificationService.ShowSuccess("测试Success");


        }

        private void OnApToUi(string obj)
        {
            if (obj == "NeedConfig")
            {
                GlobalSettings.IsAlipayPoolingStarted = false;
                GlobalSettings.IsAlipayShellStarted = true;

                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    _dialogService.ShowConfirm("需要先配置支付宝", callback: result =>
                    {
                        if (result.Result == ButtonResult.OK)
                        {
                            var settings = _appContext.CurrentSettings;
                            settings.IsAlipayEnabled = false;
                            settings.LastUpdateDateTime = DateTime.Now;
                            _appContext.SaveCurrentSettings(settings);
                            NavigateRegion<SettingsPageViewModel>(PaymentRegionNames.PaymentContentRegion);
                        }
                    });

                });
            }

            if (obj == "PollingStarted")
            {
                GlobalSettings.IsAlipayPoolingStarted = true;
                GlobalSettings.IsAlipayShellStarted = true;
                _notificationService.ShowSuccess("支付宝收款播报已启动");
            }

            if (obj == "PollingStoped")
            {
                GlobalSettings.IsAlipayPoolingStarted = false;
                _notificationService.ShowWarning("支付宝收款播报已停止");
            }

            if (obj == "AppStarted")
            {
                GlobalSettings.IsAlipayShellStarted = true;
                _notificationService.ShowInfo("支付宝插件已就绪");
            }
        }

        #endregion

        #region 事件订阅与处理

        /// <summary>
        /// 订阅所有事件
        /// </summary>
        private void SubscribeToEvents()
        {
            _eventAggregator.GetEvent<PaymentStartedEvent>().Subscribe(OnPaymentStarted, ThreadOption.UIThread);
            _eventAggregator.GetEvent<PaymentCancelledEvent>().Subscribe(OnPaymentCancelled, ThreadOption.UIThread);
            _eventAggregator.GetEvent<PaymentSuccessEvent>().Subscribe(OnPaymentSuccess, ThreadOption.UIThread);
            EventAggregator.Current.GetEvent<NotificationPayMessageEvent>().Subscribe(OnNotificationMessageEvent, ThreadOption.BackgroundThread);

            _eventAggregator.GetEvent<GetVXStatusAnswerEvent>().Subscribe(OnVXWork, ThreadOption.BackgroundThread);

            _weChatConnectionService.ConnectionStatusChanged += OnWeChatConnectionStatusChanged;
        }

        private void RefreshPluginStatusFromService()
        {
            IsMessageCenterRunning = _pluginRuntimeStatusService.IsMessageCenterRunning;
            MessageCenterLastSeenAt = _pluginRuntimeStatusService.MessageCenterLastSeenAt;

            IsAlipayPluginRunning = _pluginRuntimeStatusService.IsAlipayPluginRunning;
            AlipayLastReplyAt = _pluginRuntimeStatusService.AlipayLastReplyAt;

            IsWeChatPluginRunning = _pluginRuntimeStatusService.IsWeChatPluginRunning;
            WeChatLastReplyAt = _pluginRuntimeStatusService.WeChatLastReplyAt;

            IsConnectingService = IsNewPluginLifecycleModeEnabled && !IsMessageCenterRunning;
        }

        private void OnVXWork(VXStatusEto obj)
        {
            _pluginIsWorking = obj.IsWork;

            if (obj.IsWork)
            {
                _CurrentDeviceRequestService.CurrentApplication.Dispatcher.Invoke(() => _notificationService.ShowSuccess("微信收款已启动"));
            }
            else
            {
                // 插件未工作，发送启动命令，但继续监控等待启动完成
                Task.Run(async () =>
                {
                    var isWeChatRunning = await _weChatService.IsWeChatProcessRunningAsync();
                    if (isWeChatRunning && _weChatProcessId != -1)
                    {
                        _eventAggregator.GetEvent<StartVXModuleEvent>().Publish(_weChatProcessId);
                    }
                });
            }
        }

        /// <summary>
        /// 处理支付通知消息
        /// </summary>
        private void OnNotificationMessageEvent(NotificationPayMessageEto eto)
        {
            _ = _paymentNotificationHandler.HandleNotificationAsync(eto);
        }

        /// <summary>
        /// 处理微信连接状态变化
        /// </summary>
        private void OnWeChatConnectionStatusChanged(object? sender, ConnectionStatusChangedEventArgs e)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                IsWeChatConnected = e.IsConnected;
            });
        }

        #endregion

        #region 支付事件处理

        /// <summary>
        /// 处理支付开始事件
        /// </summary>
        private void OnPaymentStarted(PaymentEventArgs args)
        {
            _isPaymentInProgress = true;
            //_currentPaymentArgs = args;

            // 先检查当前订单是否已存在（防止重复推送导致错误取消）
            var transaction = FindTransaction(t => t.OrderNumber == args.OrderNumber);
            if (transaction != null)
            {
                // 如果订单已经是终态（成功或取消），不再处理
                if (transaction.Status == PaymentStatus.Success || transaction.Status == PaymentStatus.Cancel)
                {
                    return;
                }

                // 重复推送：只更新金额，不取消订单
                transaction.Amount = args.Amount;

                return;
            }

            // 当前订单不存在，检查该用户是否有其他未支付订单
            bool shouldPlayBeforeNotPaySound = false;
            var lastOrder = FindTransaction(t => t.UserId == args.UserId && t.OrderNumber != args.OrderNumber && t.Status != PaymentStatus.Success);


            if (lastOrder != null && lastOrder.Status != PaymentStatus.Success)
            {
                var currentTime = PcHelper.GetNetNowTime();
                var elapsedTime = currentTime - lastOrder.CreatedAt;

                if (elapsedTime.TotalSeconds >= 120)
                {
                    shouldPlayBeforeNotPaySound = true;
                }

                // 标记为静默取消（重复扫码导致的取消）
                _orderStateManager.MarkSilentCancel(lastOrder.OrderNumber);

            }

            // 创建新订单
            var processingTransaction = new TransactionRecord
            {
                TransactionTime = args.PayTime,
                OrderNumber = args.OrderNumber,
                UserId = args.UserId,
                DisplayName = args.DisplayName,
                PaymentChannel = args.PaymentChannel,
                Amount = args.Amount,
                Remarks = args.Remarks,
                CreatedAt = DateTime.Now,
                Status = PaymentStatus.Scan
            };
            AddTransaction(processingTransaction);

            RaisePropertyChanged(nameof(Transactions));


            _ = _httpNotificationService.SendPaymentNotificationAsync(args);

            // 开始追踪订单
            _orderStateManager.TrackOrder(args.OrderNumber);

            // 确定是否播放昵称
            bool shouldPlayNickname = false;
            if (args.PaymentChannel == PaymentChannel.Alipay && _appContext.CurrentSettings.IsAlipayNicknameReminderEnabled)
            {
                shouldPlayNickname = true;
            }
            else if (args.PaymentChannel == PaymentChannel.WeChat && _appContext.CurrentSettings.IsWeChatNicknameReminderEnabled)
            {
                shouldPlayNickname = true;
            }

            var nickname = shouldPlayNickname ? args.DisplayName : null;

            // 播放语音提示
            if (shouldPlayBeforeNotPaySound)
            {
                _ = _voiceService.PlayPaymentStartedWithBeforeNotPayAsync(args.PaymentChannel, nickname);
            }
            else
            {
                _ = _voiceService.PlayPaymentStartedAsync(args.PaymentChannel, nickname);
            }
        }

        /// <summary>
        /// 处理支付取消事件
        /// </summary>
        private async void OnPaymentCancelled(PaymentEventArgs args)
        {
            _isPaymentInProgress = false;
            //_currentPaymentArgs = null;

            // 停止追踪订单
            _orderStateManager.UntrackOrder(args.OrderNumber);

            // 判断是否为静默取消（超时或重复扫码）
            bool isSilentCancel = _orderStateManager.IsSilentCancel(args.OrderNumber);
            if (isSilentCancel)
            {
                _orderStateManager.ClearSilentCancel(args.OrderNumber);
            }

            // 查找并更新内存中的记录
            var transaction = FindTransaction(t => t.OrderNumber == args.OrderNumber);
            if (transaction != null)
            {
                if (transaction.Status == PaymentStatus.Cancel)
                {
                    return;
                }
                transaction.Status = PaymentStatus.Cancel;
                transaction.TransactionTime = args.PayTime;
                transaction.DisplayName = args.DisplayName;
                transaction.Amount = args.Amount;
            }
            else
            {
                // 如果内存中没有（可能已刷新），创建新记录
                var cancelledTransaction = new TransactionRecord
                {
                    TransactionTime = args.PayTime,
                    OrderNumber = args.OrderNumber,
                    UserId = args.UserId,
                    DisplayName = args.DisplayName,
                    PaymentChannel = args.PaymentChannel,
                    Amount = args.Amount,
                    Remarks = args.Remarks,
                    CreatedAt = DateTime.Now,
                    Status = PaymentStatus.Cancel
                };
                AddTransaction(cancelledTransaction);
            }


            RaisePropertyChanged(nameof(Transactions));

            _ = _httpNotificationService.SendPaymentNotificationAsync(args);

            // 保存到数据库
            await SaveOrUpdateTransactionAsync(args, PaymentStatus.Cancel);

            // 只有用户主动取消才播放提示音
            if (!isSilentCancel)
            {
                // 确定是否播放昵称
                bool shouldPlayNickname = false;
                if (args.PaymentChannel == PaymentChannel.Alipay && _appContext.CurrentSettings.IsAlipayNicknameReminderEnabled)
                {
                    shouldPlayNickname = true;
                }
                else if (args.PaymentChannel == PaymentChannel.WeChat && _appContext.CurrentSettings.IsWeChatNicknameReminderEnabled)
                {
                    shouldPlayNickname = true;
                }

                var nickname = shouldPlayNickname ? args.DisplayName : null;
                _ = _voiceService.PlayPaymentCancelledAsync(nickname);
            }





            //// 显示警告通知
            //_notificationService.ShowWarning("支付已取消");
        }

        /// <summary>
        /// 处理支付成功事件
        /// </summary>
        private async void OnPaymentSuccess(PaymentEventArgs args)
        {
            _isPaymentInProgress = false;

            // 停止追踪订单
            _orderStateManager.UntrackOrder(args.OrderNumber);

            // 从内存数据中检查订单是否已存在
            var transaction = FindTransaction(t => t.OrderNumber == args.OrderNumber);
            if (transaction != null)
            {
                // 更新内存中的记录状态
                if (transaction.Status != PaymentStatus.Success)
                {
                    transaction.Status = PaymentStatus.Success;
                    transaction.TransactionTime = args.PayTime;
                    transaction.DisplayName = args.DisplayName;
                    transaction.Amount = args.Amount;

                    // 保存到数据库
                    await SaveOrUpdateTransactionAsync(args, PaymentStatus.Success);
                }
                else
                {
                    return;
                }
            }
            else
            {
                // 订单不存在，创建新记录
                var newTransaction = new TransactionRecord
                {
                    TransactionTime = args.PayTime,
                    OrderNumber = args.OrderNumber,
                    UserId = args.UserId,
                    DisplayName = args.DisplayName,
                    PaymentChannel = args.PaymentChannel,
                    Amount = args.Amount,
                    Remarks = args.Remarks,
                    CreatedAt = DateTime.Now,
                    Status = PaymentStatus.Success
                };
                AddTransaction(newTransaction);


                // 保存到数据库
                await SaveOrUpdateTransactionAsync(args, PaymentStatus.Success);
            }

            RaisePropertyChanged(nameof(Transactions));

            _ = _httpNotificationService.SendPaymentNotificationAsync(args);

            //// 显示成功通知
            //_notificationService.ShowSuccess($"收款成功 ¥{args.Amount:F2}");

            // 播放成功语音
            _ = _voiceService.PlayPaymentSuccessAsync(args.Amount, args.PaymentChannel);

            //_currentPaymentArgs = null;
        }

        /// <summary>
        /// 保存或更新交易记录（包含状态）
        /// </summary>
        private async Task SaveOrUpdateTransactionAsync(PaymentEventArgs args, PaymentStatus status)
        {
            try
            {
                // 先检查数据库中是否已存在
                var existingOrder = await _paymentRepository.GetTransactionByOrderAsync(args.OrderNumber);

                if (existingOrder != null)
                {
                    // 更新现有记录状态
                    existingOrder.Status = status;
                    existingOrder.TransactionTime = args.PayTime;
                    existingOrder.UserId = args.UserId;
                    existingOrder.DisplayName = args.DisplayName;
                    existingOrder.PaymentChannel = args.PaymentChannel;
                    existingOrder.Amount = args.Amount;
                    existingOrder.Remarks = args.Remarks;
                    // 注意：这里需要Update方法，假设ITransactionService有Update方法
                    // 如果没有，可能需要调用AddTransactionAsync（如果支持覆盖）
                    await _paymentRepository.UpdateTransactionAsync(existingOrder); // 暂时用Add，实际应为Update
                }
                else
                {
                    // 创建新记录
                    var transaction = new TransactionRecord
                    {
                        TransactionTime = args.PayTime,
                        OrderNumber = args.OrderNumber,
                        UserId = args.UserId,
                        DisplayName = args.DisplayName,
                        PaymentChannel = args.PaymentChannel,
                        Amount = args.Amount,
                        Remarks = args.Remarks,
                        CreatedAt = DateTime.Now,
                        Status = status
                    };
                    await _paymentRepository.AddTransactionAsync(TransactionRecordDbo.FromModel(transaction));
                }
                GlobalSettings.CurrentAppContext.CurrentLogger.Info($"交易记录已保存/更新: {args.OrderNumber}, 状态: {status}");
            }
            catch (Exception ex)
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Error($"保存交易记录失败: {ex.Message}");
            }
        }

        #endregion

        #region 交易记录管理

        private const int MaxTransactionCount = 50;

        /// <summary>
        /// 查找符合条件的交易记录
        /// </summary>
        private TransactionRecord? FindTransaction(Func<TransactionRecord, bool> predicate)
        {
            return Transactions.FirstOrDefault(predicate);
        }

        /// <summary>
        /// 插入交易记录到集合头部，超过50条时移除最旧的记录
        /// </summary>
        private void AddTransaction(TransactionRecord transaction)
        {
            Transactions.Insert(0, transaction);
            while (Transactions.Count > MaxTransactionCount)
            {
                Transactions.RemoveAt(Transactions.Count - 1);
            }
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 异步加载交易数据
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                var todayTransactions = await _paymentRepository.GetTransactionsByDateRangeAsync(today, tomorrow);

                Transactions.Clear();

                var latestTransactions = todayTransactions
                    .OrderByDescending(t => t.TransactionTime)
                    .Take(MaxTransactionCount)
                    .ToList();

                foreach (var transaction in latestTransactions)
                {
                    Transactions.Add(transaction.ToModel());
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"加载数据失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                _dialogService.ShowError(ex.Message);
            }
        }

        #endregion

        #region 导航命令

        /// <summary>
        /// 导航到历史记录页面
        /// </summary>
        private void NavigateToHistory()
        {
            NavigateRegion<HistoryPageModel>(PaymentRegionNames.PaymentContentRegion);
        }

        /// <summary>
        /// 导航到设置页面
        /// </summary>
        private void NavigateToSettings()
        {
            NavigateRegion<SettingsPageViewModel>(PaymentRegionNames.PaymentContentRegion);
        }

        /// <summary>
        /// 导航到日志页面
        /// </summary>
        private void NavigateToLog()
        {
            NavigateRegion<PluginLogPageViewModel>(PaymentRegionNames.PaymentContentRegion);
        }

        /// <summary>
        /// 退出应用程序
        /// </summary>
        private async void Exit()
        {
            var result = MessageBox.Show(
                "确定要退出收银台系统吗？",
                "确认退出",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // 停止微信监控
                StopWeChatMonitoring();
                // 旧模式：主程序负责插件进程清理
                await _pluginProcessService.CleanupExistingProcessesAsync();
                Application.Current.Shutdown();
            }
        }

        #endregion

        #region 页面导航生命周期

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            // base.OnNavigatedTo(navigationContext);

            // 刷新配置信息
            StoreName = _appContext.CurrentSettings.StoreName;
            MaintenanceContact = _appContext.CurrentSettings.MaintenanceContact;

            if (IsNewPluginLifecycleModeEnabled)
            {
                if (Interlocked.Exchange(ref _newModeInitStartedFlag, 1) == 0)
                {
                    _ = InitializePaymentChannelsInNewModeAsync();
                }


                if (!_appContext.CurrentSettings.IsAlipayEnabled)
                {
                    await _channelCoordinator.StopAlipayAsync();
                }
                //else
                //{
                //    await _channelCoordinator.StartAlipayPollingAsync();
                //}

            }
            else
            {
                await InitializePaymentChannelsInLegacyModeAsync();
            }
        }

        private async Task InitializePaymentChannelsInLegacyModeAsync()
        {
            // 旧模式：主程序管理插件进程 + 业务就绪
            if (_appContext.CurrentSettings.IsWeChatEnabled && Interlocked.Exchange(ref _weChatStartRequestedFlag, 1) == 0)
            {
                await _channelCoordinator.StartWeChatAsync(StartWeChatMonitoring);
            }

            if (_appContext.CurrentSettings.IsAlipayEnabled)
            {
                await _channelCoordinator.StartAlipayShellAsync();
                await _channelCoordinator.StartAlipayPollingAsync();
            }
            else
            {
                await _channelCoordinator.StopAlipayAsync();
            }
        }


        /// <summary>
        /// 初始化支付渠道（新模式）
        /// </summary>
        /// <returns></returns>
        private async Task InitializePaymentChannelsInNewModeAsync()
        {

            #region 等待消息中心起来并连接

            IsConnectingService = true;

            await _pluginRuntimeStatusService.WaitForMessageCenterRunningAsync();

            IsConnectingService = false;

            await _weChatConnectionService.StartAsync();

            RefreshPluginStatusFromService();

            #endregion



            // 启用微信插件
            if (_appContext.CurrentSettings.IsWeChatEnabled && Interlocked.Exchange(ref _weChatStartRequestedFlag, 1) == 0)
            {
                await _channelCoordinator.StartWeChatAsync();

                await Task.Delay(1000);

                StartWeChatMonitoring();

            }


            //// 启用支付宝插件
            //if (_appContext.CurrentSettings.IsAlipayEnabled)
            //{
            //    if (_pluginRuntimeStatusService.IsAlipayPluginRunning)
            //    {
            //        if (!GlobalSettings.IsAlipayShellStarted)
            //        {
            //            await _channelCoordinator.SendAlipayAppStart();
            //        }

            //        if (!GlobalSettings.IsAlipayPoolingStarted)
            //        {
            //            await _channelCoordinator.StartAlipayPollingAsync();
            //        }
            //    }
            //    else
            //    {
            //        _notificationService.ShowInfo("支付宝插件未就绪");
            //    }
            //}
            //else
            //{
            //    await _channelCoordinator.StopAlipayAsync();
            //}


        }

        #endregion

        #region 微信统一监控

        /// <summary>
        /// 启动微信统一监控 - 一个循环搞定所有事情
        /// </summary>
        private void StartWeChatMonitoring()
        {
            if (_isWeChatMonitoring)
                return;

            _isWeChatMonitoring = true;
            _weChatMonitorCts?.Cancel();
            _weChatMonitorCts = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                var token = _weChatMonitorCts.Token;

                while (!token.IsCancellationRequested && _isWeChatMonitoring)
                {
                    try
                    {
                        await UnifiedWeChatMonitoring(token);
                        await Task.Delay(2000, token); // 统一2秒间隔
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        // 出错继续，不要停止监控
                        await Task.Delay(5000, token);
                    }
                }
            });
        }

        /// <summary>
        /// 统一的微信监控逻辑 - 替代原来的3个独立任务
        /// </summary>
        private async Task UnifiedWeChatMonitoring(CancellationToken token)
        {
            if (!_appContext.CurrentSettings.IsWeChatEnabled)
                return;

            // 1. 检查微信进程状态
            var processInfo = await _weChatService.GetWeChatProcessInfoAsync();

            if (processInfo == null)
            {
                // 微信不存在 - 清理一切
                HandleWeChatNotRunning();
                await _channelCoordinator.StartWeChatAsync();
                return;
            }

            // 2. 检查是否是新的微信进程
            if (_weChatProcessId != processInfo.ProcessId)
            {
                HandleNewWeChatProcess(processInfo.ProcessId);
            }

            // 3. 检查微信是否登录
            var isLoggedIn = await _weChatService.CheckWeChatLoginStatusAsync();
            if (!isLoggedIn)
            {
                // 未登录，显示登录轮询状态但不启动插件
                return;
            }

            // 4. 微信已登录，确保插件正常工作
            await EnsurePluginWorking(processInfo.ProcessId, token);
        }

        /// <summary>
        /// 处理微信进程不存在的情况，避免插件状态混乱
        /// </summary>
        private void HandleWeChatNotRunning()
        {
            _weChatProcessId = -1;
            _pluginIsWorking = false;

            if (!IsNewPluginLifecycleModeEnabled)
            {
                _pluginProcessService.StopProcessAsync(PluginSettings.WeChatShellExe, "旧模式微信掉线强制杀插件进程");
            }

        }

        /// <summary>
        /// 处理新的微信进程
        /// </summary>
        private void HandleNewWeChatProcess(int newProcessId)
        {
            _weChatProcessId = newProcessId;
            _pluginIsWorking = false; // 新进程，插件状态重置
        }

        /// <summary>
        /// 确保插件正常工作
        /// </summary>
        private async Task EnsurePluginWorking(int processId, CancellationToken token)
        {
            if (_pluginIsWorking)
                return; // 已经工作了，不用管

            // 检查插件进程是否运行
            if (!_pluginProcessService.IsPluginRunning(PluginSettings.WeChatShellExe))
                return;

            // 查询插件状态
            _eventAggregator.GetEvent<GetVXStatusRequestEvent>().Publish();

            // 给插件1秒响应时间
            await Task.Delay(1000, token);

            // 如果插件没响应，发送启动命令
            if (!_pluginIsWorking)
            {
                _eventAggregator.GetEvent<StartVXModuleEvent>().Publish(processId);
            }
        }

        /// <summary>
        /// 停止微信监控
        /// </summary>
        private void StopWeChatMonitoring()
        {
            _isWeChatMonitoring = false;
            _weChatMonitorCts?.Cancel();
        }


        #endregion

        #region 定时器管理

        /// <summary>
        /// 启动所有定时器
        /// </summary>
        private void StartTimers()
        {
            // 启动时间更新定时器
            _timerCoordinator.StartTimeUpdate(UpdateCurrentTime);

            // 启动订单监控定时器
            _timerCoordinator.StartOrderMonitoring(
                getOrders: () =>
                    Application.Current?.Dispatcher.Invoke(() =>
                        Transactions
                            .Where(t => t.Status == PaymentStatus.Scan)
                            .ToList())
                    ?? new List<TransactionRecord>(),
                getCurrentTime: PcHelper.GetNetNowTime,
                onCheck: (orders, currentTime) => _orderStateManager.CheckOrderStates(orders, currentTime)
            );
        }

        /// <summary>
        /// 更新当前时间
        /// </summary>
        private void UpdateCurrentTime(DateTime time)
        {
            try
            {
                var networkTime = PcHelper.GetNetNowTime();
                CurrentTime = networkTime.ToString("HH:mm:ss");
            }
            catch
            {
                // 如果获取网络时间失败，使用本地时间
                CurrentTime = time.ToString("HH:mm:ss");
            }
        }

        /// <summary>
        /// 处理订单通知事件（20秒、40秒、60秒播报）
        /// </summary>
        private void OnOrderNotification(object? sender, OrderNotificationEventArgs e)
        {
            // 播放扫码未支付提示音
            _ = _voiceService.PlayScanNotPayAsync();
        }

        /// <summary>
        /// 处理订单超时事件（超过2分钟自动取消）
        /// </summary>
        private void OnOrderTimeout(object? sender, OrderTimeoutEventArgs e)
        {
            var cancelArgs = new PaymentEventArgs
            {
                OrderNumber = e.OrderNumber,
                UserId = e.UserId,
                DisplayName = e.DisplayName,
                Amount = e.Amount,
                PaymentChannel = e.PaymentChannel,
                Remarks = e.Remarks,
                PayTime = PcHelper.GetNetNowTime(),
                Status = PaymentStatus.Cancel
            };

            // 发布取消支付事件
            _eventPublisher.PublishPaymentCancelled(cancelArgs);
        }

        #endregion

#if DEBUG
        // Partial method for test command initialization
        partial void InitializeTestCommands();
#endif
    }
}
