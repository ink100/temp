using HRB.Payment.Core.Events;
using HRB.Payment.Core.Helpers;
using HRB.Payment.Core.Models;
using HRB.Payment.Core.Services;
using HRB.Payment.Message.Core.BusEvents;
using HRB.Platform.Client.Core.ExtensionFunctions;
using HRB.Platform.Client.WPF.Core.Services.IServices;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Abstractions;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Helpers;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Plugins.Alipay;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Plugins.WeChat;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Services;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.V2
{

    /// <summary>
    /// 2.0 主页 ViewModel — 纯 UI 壳 + 事件路由。
    ///
    /// 职责：
    ///   1. UI 状态（时间、字体、店铺名、导航、退出）
    ///   2. 订单展示（绑定 IPaymentTransactionService.Transactions）
    ///   3. 事件路由（支付事件 → TransactionService → 根据结果播声/推HTTP）
    ///
    /// 不直接依赖任何插件通信、不管进程、不管连接。
    /// </summary>
    public class MainPageV2ViewModel : BasePaymentRegionViewModel
    {
        private readonly PaymentAppContext _appContext;
        private readonly IPaymentTransactionService _transactionService;
        private readonly IPaymentVoiceService _voiceService;
        private readonly ITimerCoordinator _timerCoordinator;
        private readonly IHttpNotificationService _httpNotificationService;
        private readonly INotificationService _notificationService;
        private readonly IOrderStateManager _orderStateManager;
        private readonly IPaymentNotificationHandler _notificationHandler;
        private readonly IPaymentEventPublisher _eventPublisher;
        private readonly AlipayChannelPlugin _alipayPlugin;

        private readonly WeChatChannelPlugin _weChatPlugin;

        #region UI 绑定属性

        public override bool KeepAlive { get; } = true;

        /// <summary>
        /// 交易记录集合（来自 TransactionService）
        /// </summary>
        public ObservableCollection<TransactionRecord> Transactions => _transactionService.Transactions;

        public string CurrentTime
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;

        public string StoreName
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;

        public string MaintenanceContact
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;

        public bool IsAutoScrollToLatestEnabled
        {
            get;
            set => SetProperty(ref field, value);
        }

        public double BaseFontSize
        {
            get;
            set
            {
                if (SetProperty(ref field, value))
                {
                    RowHeight = value * 1.5 + 20;
                    AmountFontSize = value * 2;
                    RaisePropertyChanged(nameof(AmountFontSize));

                    var settings = _appContext.CurrentSettings;
                    settings.FontSize = value;
                    settings.LastUpdateDateTime = DateTime.Now;
                    _appContext.SaveCurrentSettings(settings);
                }
            }
        } = 60;
        private void OnPaymentDisplaySettingsChanged()
        {
            PaymentAmountColorHelper.ClearCache();

            App.Current.Dispatcher.Invoke(() =>
            {
                CollectionViewSource.GetDefaultView(TransactionRecords)?.Refresh();
            });
        }
        public double AmountFontSize
        {
            get;
            set => SetProperty(ref field, value);
        } = 120;

        public double RowHeight
        {
            get;
            set => SetProperty(ref field, value);
        } = 110;

        // 插件状态
        public bool IsAlipayAvailable { get; set => SetProperty(ref field, value); }
        public bool IsAlipayRunning { get; set => SetProperty(ref field, value); }
        public bool IsWeChatAvailable { get; set => SetProperty(ref field, value); }
        public bool IsWeChatRunning { get; set => SetProperty(ref field, value); }
        public string? AlipayStatusMessage { get; set => SetProperty(ref field, value); }
        public string? WeChatStatusMessage { get; set => SetProperty(ref field, value); }

        #endregion

        #region 命令

        public ICommand NavigateToHistoryCommand { get; }
        public ICommand NavigateToSettingsCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand SetSmallFontCommand { get; }
        public ICommand SetMediumFontCommand { get; }
        public ICommand SetLargeFontCommand { get; }

        #endregion

        public MainPageV2ViewModel(
            PaymentAppContext appContext,
            IEventAggregator eventAggregator,
            IRegionManager regionManager,
            IWpfDeviceRequestService deviceRequestService,
            IPaymentTransactionService transactionService,
            IPaymentVoiceService voiceService,
            ITimerCoordinator timerCoordinator,
            IHttpNotificationService httpNotificationService,
            INotificationService notificationService,
            IOrderStateManager orderStateManager,
            IPaymentNotificationHandler notificationHandler,
            IPaymentEventPublisher eventPublisher,
            AlipayChannelPlugin alipayPlugin,
            WeChatChannelPlugin weChatPlugin
        ) : base(appContext, eventAggregator, regionManager, deviceRequestService)
        {
            _appContext = appContext;
            _transactionService = transactionService;
            _voiceService = voiceService;
            _timerCoordinator = timerCoordinator;
            _httpNotificationService = httpNotificationService;
            _notificationService = notificationService;
            _orderStateManager = orderStateManager;
            _notificationHandler = notificationHandler;
            _eventPublisher = eventPublisher;
            _alipayPlugin = alipayPlugin;
            _weChatPlugin = weChatPlugin;

            // 启动系统时间自动同步调度器。
            SystemTimeSyncService.Start(_appContext);

            // UI 初始化
            BaseFontSize = appContext.CurrentSettings.FontSize;
            StoreName = appContext.CurrentSettings.StoreName;
            MaintenanceContact = appContext.CurrentSettings.MaintenanceContact;
            IsAutoScrollToLatestEnabled = appContext.CurrentSettings.IsAutoScrollToLatestEnabled;

            // 命令
            NavigateToHistoryCommand = new DelegateCommand(
                () => NavigateRegion<HistoryPageModel>(PaymentRegionNames.PaymentContentRegion));
            NavigateToSettingsCommand = new DelegateCommand(
                () => NavigateRegion<SettingsPageV2ViewModel>(PaymentRegionNames.PaymentContentRegion));
            ExitCommand = new DelegateCommand(Exit);
            SetSmallFontCommand = new DelegateCommand(() => BaseFontSize = 32);
            SetMediumFontCommand = new DelegateCommand(() => BaseFontSize = 48);
            SetLargeFontCommand = new DelegateCommand(() => BaseFontSize = 60);

            // 事件订阅
            SubscribeToEvents(eventAggregator);

            // 定时器
            StartTimers();

            // 加载今日数据
            _ = _transactionService.LoadTodayTransactionsAsync();
            
            // 插件状态
            _alipayPlugin.StatusChanged += OnPluginStatusChanged;
            _weChatPlugin.StatusChanged += OnPluginStatusChanged;
            RefreshPluginStatus();
        }
        public async Task<bool> LoadMoreTransactionsAsync()
        {
            try
            {
                return await _transactionService.LoadMoreTransactionsAsync();
            }
            catch (Exception ex)
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Error(
                    $"首页加载更多交易记录失败: {ex.Message}");
                return false;
            }
        }
        #region 生命周期

        protected override void OnNavigatedToEvent(NavigationContext navigationContext)
        {
            StoreName = _appContext.CurrentSettings.StoreName;
            MaintenanceContact = _appContext.CurrentSettings.MaintenanceContact;
            IsAutoScrollToLatestEnabled = _appContext.CurrentSettings.IsAutoScrollToLatestEnabled;
            SystemTimeSyncService.RefreshSchedule();
            RefreshPluginStatus();
        }

        #endregion

        #region 事件订阅

        private void SubscribeToEvents(IEventAggregator eventAggregator)
        {
            eventAggregator.GetEvent<PaymentStartedEvent>().Subscribe(OnPaymentStarted, ThreadOption.UIThread);
            eventAggregator.GetEvent<PaymentCancelledEvent>().Subscribe(OnPaymentCancelled, ThreadOption.UIThread);
            eventAggregator.GetEvent<PaymentSuccessEvent>().Subscribe(OnPaymentSuccess, ThreadOption.UIThread);
            EventAggregator.Current.GetEvent<NotificationPayMessageEvent>().Subscribe(OnNotificationMessage, ThreadOption.BackgroundThread);

            _orderStateManager.OrderTimeout += OnOrderTimeout;
            _orderStateManager.OrderNotification += OnOrderNotification;
        }

        private void OnNotificationMessage(NotificationPayMessageEto eto)
        {
            _ = _notificationHandler.HandleNotificationAsync(eto);
        }

        #endregion

        #region 支付事件处理

        private void OnPaymentStarted(PaymentEventArgs args)
        {
            _ = OnPaymentStartedAsync(args);
        }

        private async Task OnPaymentStartedAsync(PaymentEventArgs args)
        {
            try
            {
                var result = await _transactionService.HandlePaymentStartedAsync(args);
                if (result.AlreadyExists)
                    return;

                if (result.PriorCancelledPayment != null)
                    _ = _httpNotificationService.SendPaymentNotificationAsync(result.PriorCancelledPayment);

                _ = _httpNotificationService.SendPaymentNotificationAsync(args);

                var nickname = ShouldPlayNickname(args) ? args.DisplayName : null;
                var settings = _appContext.CurrentSettings;

                if (result.HasPriorUnpaid && settings.IsPriorUnpaidVoiceEnabled)
                    _ = _voiceService.PlayPaymentStartedWithBeforeNotPayAsync(args.PaymentChannel, nickname, args.OrderNumber);
                else
                    _ = _voiceService.PlayPaymentStartedAsync(args.PaymentChannel, nickname, args.OrderNumber);
            }
            catch (Exception ex)
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Error($"处理扫码开始事件失败: {ex.Message}");
            }
        }


        private void OnPaymentCancelled(PaymentEventArgs args)
        {
            _ = OnPaymentCancelledAsync(args);
        }

        private async Task OnPaymentCancelledAsync(PaymentEventArgs args)
        {
            try
            {
                var result = await _transactionService.HandlePaymentCancelledAsync(args);

                _voiceService.MarkOrderCompleted(args.OrderNumber);

                if (!result.StateChanged)
                    return;

                _ = _httpNotificationService.SendPaymentNotificationAsync(args);

                var settings = _appContext.CurrentSettings;
                if (!result.IsSilentCancel && settings.IsPaymentCancelledVoiceEnabled)
                {
                    var nickname = ShouldPlayNickname(args) ? args.DisplayName : null;
                    _ = _voiceService.PlayPaymentCancelledAsync(nickname, args.OrderNumber);
                }
            }
            catch (Exception ex)
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Error($"处理支付取消事件失败: {ex.Message}");
            }
        }

        private void OnPaymentSuccess(PaymentEventArgs args)
        {
            _ = OnPaymentSuccessAsync(args);
        }

        private async Task OnPaymentSuccessAsync(PaymentEventArgs args)
        {
            try
            {
                var result = await _transactionService.HandlePaymentSuccessAsync(args);

                // 先标记订单完成，防止旧的扫码未支付语音排队后误播。
                _voiceService.MarkOrderCompleted(args.OrderNumber);

                if (!result.StateChanged)
                    return;

                _ = _httpNotificationService.SendPaymentNotificationAsync(args);

                var settings = _appContext.CurrentSettings;
                if (settings.IsPaymentSuccessVoiceEnabled)
                    _ = _voiceService.PlayPaymentSuccessAsync(args.Amount, args.PaymentChannel, args.OrderNumber);
            }
            catch (Exception ex)
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Error(
                    $"处理支付成功事件失败，订单号:{args.OrderNumber}，渠道:{args.PaymentChannel}，异常:{ex.Message}");
            }
        }

        private bool ShouldPlayNickname(PaymentEventArgs args)
        {
            var settings = _appContext.CurrentSettings;
            return args.PaymentChannel switch
            {
                PaymentChannel.Alipay => settings.IsAlipayNicknameReminderEnabled,
                PaymentChannel.WeChat => settings.IsWeChatNicknameReminderEnabled,
                _ => false
            };
        }

        #endregion

        #region 订单超时

        private void OnOrderNotification(object? sender, OrderNotificationEventArgs e)
        {
            var settings = _appContext.CurrentSettings;
            if (!settings.IsScanNotPayVoiceEnabled)
                return;

            _ = _voiceService.PlayScanNotPayAsync(e.OrderNumber);
        }

        private void OnOrderTimeout(object? sender, OrderTimeoutEventArgs e)
        {
            // 超时未支付不等于取消支付。
            // 这里只做提醒，不发布 PaymentCancelled，
            // 因此界面状态仍保持“扫码中”。
            var settings = _appContext.CurrentSettings;

            if (settings.IsScanNotPayVoiceEnabled)
            {
                _ = _voiceService.PlayScanNotPayAsync(e.OrderNumber);
            }

            GlobalSettings.CurrentAppContext.CurrentLogger.Info(
                $"订单超时未支付提醒已触发，订单号:{e.OrderNumber}，渠道:{e.PaymentChannel}");
        }

        #endregion

        #region 定时器

        private void StartTimers()
        {
            _timerCoordinator.StartTimeUpdate(time =>
            {
                CurrentTime = time.ToString("HH:mm:ss");
            });

            _timerCoordinator.StartOrderMonitoring(
                getOrders: () =>
                    Application.Current?.Dispatcher.Invoke(() =>
                        _transactionService.Transactions
                            .Where(t => t.Status == PaymentStatus.Scan)
                            .ToList())
                    ?? new List<TransactionRecord>(),
                getCurrentTime: () => PcHelper.GetNetNowTime(),
                onCheck: (orders, time) => _orderStateManager.CheckOrderStates(orders, time));
        }

        #endregion

        #region 插件状态

        private void OnPluginStatusChanged(object? sender, ChannelStatusChangedEventArgs e)
        {
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                RefreshPluginStatus();
                if (!string.IsNullOrEmpty(e.Message))
                {
                    _notificationService.ShowInfo(e.Message);
                }
            });
        }

        private void RefreshPluginStatus()
        {
            IsAlipayAvailable = _alipayPlugin.IsAvailable;
            IsAlipayRunning = _alipayPlugin.IsEnabled;
            AlipayStatusMessage = _alipayPlugin.IsEnabled ? "运行中" : (_alipayPlugin.IsAvailable ? "就绪" : "未启动");

            IsWeChatAvailable = _weChatPlugin.IsAvailable;
            IsWeChatRunning = _weChatPlugin.IsEnabled;
            WeChatStatusMessage = _weChatPlugin.IsEnabled ? "运行中" : (_weChatPlugin.IsAvailable ? "就绪" : "未启动");
        }

        #endregion

        #region 退出

        private async void Exit()
        {
            var result = MessageBox.Show("确定要退出收银台系统吗？", "确认退出",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await _alipayPlugin.ShutdownAsync();
                await _weChatPlugin.ShutdownAsync();
                _timerCoordinator.StopAll();
                Application.Current.Shutdown();
            }
        }

        #endregion
    }
}
