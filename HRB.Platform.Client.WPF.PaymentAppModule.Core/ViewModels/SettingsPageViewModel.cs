using HRB.Payment.Core.Services;
using HRB.Payment.Message.Core.BusEvents;
using HRB.Platform.Client.Core.ExtensionFunctions;
using HRB.Platform.Client.WPF.Core.Services.IServices;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Configuration;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Extensions;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Repository;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Services;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.ViewModels
{
    /// <summary>
    /// 设置页面视图模型
    /// 负责：软件激活、机器序列号显示、到期时间显示
    /// </summary>
    public class SettingsPageViewModel : BasePaymentRegionViewModel
    {
        #region Windows API 声明

        [DllImport("user32.dll")]
        private static extern IntPtr GetOpenClipboardWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        #endregion

        #region 字段

        private readonly PaymentAppContext _appContext;
        private readonly ILicenseService _licenseService;
        private readonly IRegionManager _regionManager;
        // private readonly IAlipayConfigService _alipayConfigService;
        private readonly IPluginProcessService _pluginProcessService;
        private readonly INotificationService _notificationService;
        private readonly ILoadingOverlayService _loadingOverlayService;
        private readonly IDialogService _dialogService;
        private readonly IPluginKeepAliveService _pluginKeepAliveService;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPaymentChannelCoordinator _channelCoordinator;
        private readonly IPluginRuntimeStatusService _pluginRuntimeStatusService;

        // UI绑定字段
        private string _machineSN = string.Empty;
        private string _activationStatus = "未激活";
        private string _expiresAt = "未激活";
        private int _remainingDays = 0;
        private bool _isActivated = false;
        private bool _isActivating = false;
        private string _licenseFilePath = string.Empty;
        private bool _isAlipayEnabled = false;
        private bool _isWeChatEnabled = false;
        private bool _isConfiguring = false;
        private bool _appStarted = false;
        private bool _isAlipayNicknameReminderEnabled = false;
        private bool _isWeChatNicknameReminderEnabled = false;
        private bool _isAutoStartEnabled = false;
        private bool _useNewPluginLifecycleMode = false;
        private string _notificationUrl = string.Empty;
        private bool _isNotificationEnabled = false;
        private string _appVersion = string.Empty;
        private string _storeName = string.Empty;
        private string _maintenanceContact = string.Empty;

        #endregion

        #region 属性

        /// <summary>
        /// 视图模型保持活动状态
        /// </summary>
        public override bool KeepAlive { get; } = false;

        private bool IsNewPluginLifecycleModeEnabled =>
            PluginLifecycleModeHelper.IsNewModeEnabled(_appContext.CurrentSettings);

        /// <summary>
        /// 机器序列号（SN）
        /// </summary>
        public string MachineSN
        {
            get => _machineSN;
            set => SetProperty(ref _machineSN, value);
        }

        /// <summary>
        /// 激活状态显示
        /// </summary>
        public string ActivationStatus
        {
            get => _activationStatus;
            set => SetProperty(ref _activationStatus, value);
        }

        /// <summary>
        /// 到期时间显示
        /// </summary>
        public string ExpiresAt
        {
            get => _expiresAt;
            set => SetProperty(ref _expiresAt, value);
        }

        /// <summary>
        /// 剩余天数
        /// </summary>
        public int RemainingDays
        {
            get => _remainingDays;
            set => SetProperty(ref _remainingDays, value);
        }

        /// <summary>
        /// 是否已激活
        /// </summary>
        public bool IsActivated
        {
            get => _isActivated;
            set => SetProperty(ref _isActivated, value);
        }

        /// <summary>
        /// 是否正在激活
        /// </summary>
        public bool IsActivating
        {
            get => _isActivating;
            set => SetProperty(ref _isActivating, value);
        }

        /// <summary>
        /// 激活文件路径
        /// </summary>
        public string LicenseFilePath
        {
            get => _licenseFilePath;
            set => SetProperty(ref _licenseFilePath, value);
        }

        /// <summary>
        /// 是否启用支付宝支付
        /// </summary>
        public bool IsAlipayEnabled
        {
            get => _isAlipayEnabled;
            set
            {
                // 检查激活状态
                if (!_isActivated && value)
                {
                    _dialogService.ShowWarning("请先激活软件后再启用支付宝支付功能");
                    RaisePropertyChanged();
                    return;
                }

                // 检查 AppStarted 事件是否已接收
                if (!GlobalSettings.IsAlipayShellStarted && value)
                {
                    _dialogService.ShowWarning("支付宝插件尚未就绪，请稍后再试");
                    RaisePropertyChanged();
                    return;
                }

                // 防止重复点击
                if (_isConfiguring)
                {
                    // 如果正在配置中，恢复原状态
                    RaisePropertyChanged();
                    return;
                }

                // 如果尝试启用支付宝，需要验证是否有配置
                if (value && !_isAlipayEnabled)
                {
                    IsConfiguring = true;
                    _CurrentEventAggregator.GetEvent<UIToAPModuleEvent>().Publish("Enable");
                }
                else if (!value && _isAlipayEnabled)
                {
                    IsConfiguring = true;
                    _CurrentEventAggregator.GetEvent<UIToAPModuleEvent>().Publish("Disable");
                }
            }
        }

        /// <summary>
        /// 是否启用微信支付
        /// </summary>
        public bool IsWeChatEnabled
        {
            get => _isWeChatEnabled;
            set
            {
                // 检查激活状态
                if (!_isActivated && value)
                {
                    _dialogService.ShowWarning("请先激活软件后再启用微信支付功能");
                    RaisePropertyChanged();
                    return;
                }

                if (SetProperty(ref _isWeChatEnabled, value))
                {
                    SavePaymentSettings();
                }
            }
        }

        public bool IsPluginControlsEnabled => IsActivated && _pluginRuntimeStatusService.IsMessageCenterRunning;

        public string PluginControlsDisabledHint =>
            !IsActivated
                ? "请先激活软件后再配置"
                : (_pluginRuntimeStatusService.IsMessageCenterRunning
                    ? string.Empty
                    : "服务未就绪：等待消息中心启动后可配置");

        /// <summary>
        /// 是否正在配置支付宝
        /// </summary>
        public bool UseNewPluginLifecycleMode
        {
            get => _useNewPluginLifecycleMode;
            set
            {
                if (SetProperty(ref _useNewPluginLifecycleMode, value))
                {
                    SavePaymentSettings();
                    _notificationService.ShowInfo("插件生命周期模式已更新，建议重启收银台后生效。");
                }
            }
        }

        public bool IsConfiguring
        {
            get => _isConfiguring;
            set
            {
                if (SetProperty(ref _isConfiguring, value))
                {
                    // 使用服务控制遮罩层显示/隐藏
                    if (value)
                    {
                        // _loadingOverlayService.Show("正在配置支付宝插件,请稍候...");
                        _notificationService.ShowWarning("正在配置支付宝插件,请稍候...");
                    }
                    else
                    {
                        _loadingOverlayService.Hide();
                    }
                }
            }
        }

        /// <summary>
        /// 支付宝未支付昵称提醒播报
        /// </summary>
        public bool IsAlipayNicknameReminderEnabled
        {
            get => _isAlipayNicknameReminderEnabled;
            set
            {
                if (SetProperty(ref _isAlipayNicknameReminderEnabled, value))
                {
                    SavePaymentSettings();
                }
            }
        }

        /// <summary>
        /// 微信未支付昵称提醒播报
        /// </summary>
        public bool IsWeChatNicknameReminderEnabled
        {
            get => _isWeChatNicknameReminderEnabled;
            set
            {
                if (SetProperty(ref _isWeChatNicknameReminderEnabled, value))
                {
                    SavePaymentSettings();
                }
            }
        }

        /// <summary>
        /// 是否启用开机自启动
        /// </summary>
        public bool IsAutoStartEnabled
        {
            get => _isAutoStartEnabled;
            set
            {
                if (SetProperty(ref _isAutoStartEnabled, value))
                {
                    // 使用任务计划程序方式更新开机自启
                    var success = AutoStartupService.SetEnabled(value);
                    if (!success)
                    {
                        _dialogService.ShowError("设置开机自启动失败，请以管理员权限运行程序");
                        // 恢复原状态
                        _isAutoStartEnabled = !value;
                        RaisePropertyChanged(nameof(IsAutoStartEnabled));
                        return;
                    }

                    SavePaymentSettings();
                }
            }
        }

        /// <summary>
        /// 通知接口URL
        /// </summary>
        public string NotificationUrl
        {
            get => _notificationUrl;
            set => SetProperty(ref _notificationUrl, value);
        }

        /// <summary>
        /// 是否启用消息通知
        /// </summary>
        public bool IsNotificationEnabled
        {
            get => _isNotificationEnabled;
            set
            {
                // 检查激活状态
                if (!_isActivated && value)
                {
                    _dialogService.ShowWarning("请先激活软件后再启用消息通知功能");
                    RaisePropertyChanged(nameof(IsNotificationEnabled));
                    return;
                }

                // 检查URL是否配置
                if (value && string.IsNullOrWhiteSpace(_notificationUrl))
                {
                    _dialogService.ShowWarning("请先配置通知接口URL");
                    RaisePropertyChanged(nameof(IsNotificationEnabled));
                    return;
                }

                // 如果尝试启用，检查是否已同意知情同意书
                if (value && !_isNotificationEnabled)
                {
                    _ = CheckConsentAndEnableNotificationAsync();
                }
                else if (!value && _isNotificationEnabled)
                {
                    // 禁用通知
                    if (SetProperty(ref _isNotificationEnabled, value))
                    {
                        _ = SaveNotificationConfigAsync();
                    }
                }
            }
        }

        /// <summary>
        /// 选择激活文件命令
        /// </summary>
        public ICommand SelectKeyFileCommand { get; }

        /// <summary>
        /// 激活命令
        /// </summary>
        public ICommand ActivateCommand { get; }

        /// <summary>
        /// 复制机器序列号命令
        /// </summary>
        public ICommand CopySNCommand { get; }

        /// <summary>
        /// 返回主页命令
        /// </summary>
        public ICommand GoBackCommand { get; }

        /// <summary>
        /// 退出应用命令
        /// </summary>
        public ICommand ExitCommand { get; }

        /// <summary>
        /// 软件版本号
        /// </summary>
        public string AppVersion
        {
            get => _appVersion;
            set => SetProperty(ref _appVersion, value);
        }

        /// <summary>
        /// 店铺名称
        /// </summary>
        public string StoreName
        {
            get => _storeName;
            set
            {
                if (SetProperty(ref _storeName, value))
                {
                    SavePaymentSettings();
                }
            }
        }

        /// <summary>
        /// 维护人员联系方式
        /// </summary>
        public string MaintenanceContact
        {
            get => _maintenanceContact;
            set
            {
                if (SetProperty(ref _maintenanceContact, value))
                {
                    SavePaymentSettings();
                }
            }
        }

        /// <summary>
        /// 显示免责条款与知情同意书命令
        /// </summary>
        public ICommand ShowDisclaimerCommand { get; }

        /// <summary>
        /// 显示支付消息通知服务知情同意书命令
        /// </summary>
        public ICommand ShowNotificationConsentCommand { get; }


        private readonly IPaymentChannelCoordinator _paymentChannelCoordinator;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public SettingsPageViewModel(
            PaymentAppContext appContext,
            IEventAggregator eventAggregator,
            IRegionManager regionManager,
            IWpfDeviceRequestService deviceRequestService,
            ILicenseService licenseService,
            IPluginProcessService pluginProcessService,
            INotificationService notificationService,
            ILoadingOverlayService loadingOverlayService,
            IDialogService dialogService,
            IPluginKeepAliveService pluginKeepAliveService,
            IPaymentRepository paymentRepository,
            IPaymentChannelCoordinator channelCoordinator,
            IPluginRuntimeStatusService pluginRuntimeStatusService, IPaymentChannelCoordinator paymentChannelCoordinator) : base(appContext, eventAggregator, regionManager, deviceRequestService)
        {
            _pluginRuntimeStatusService = pluginRuntimeStatusService;
            _paymentChannelCoordinator = paymentChannelCoordinator;
            _pluginRuntimeStatusService.StatusChanged += (_, __) =>
            {
                _CurrentDeviceRequestService.CurrentApplication.Dispatcher.BeginInvoke(() =>
                {
                    RaisePropertyChanged(nameof(IsPluginControlsEnabled));
                    RaisePropertyChanged(nameof(PluginControlsDisabledHint));
                });
            };
            _appContext = appContext;
            _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _pluginProcessService = pluginProcessService;
            _notificationService = notificationService;
            _loadingOverlayService = loadingOverlayService;
            _dialogService = dialogService;
            _pluginKeepAliveService = pluginKeepAliveService;
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
            _channelCoordinator = channelCoordinator;

            // AP 模块消息
            eventAggregator.GetEvent<APModuleToUIEvent>().Subscribe(OnAPModuleToUi);



            // 初始化命令
            SelectKeyFileCommand = new DelegateCommand(SelectKeyFile);
            ActivateCommand = new DelegateCommand(async () => await ActivateAsync());
            CopySNCommand = new DelegateCommand(CopySN);
            GoBackCommand = new DelegateCommand(OnGoBack);
            ExitCommand = new DelegateCommand(OnExit);
            ShowDisclaimerCommand = new DelegateCommand(ShowDisclaimer);
            ShowNotificationConsentCommand = new DelegateCommand(ShowNotificationConsent);

            // 获取软件版本号
            AppVersion = GetAppVersion();

            // 加载激活信息
            _ = LoadLicenseInfoAsync();

            // 加载支付设置
            LoadPaymentSettings();

            // 加载通知配置
            _ = LoadNotificationConfigAsync();

            if (!IsNewPluginLifecycleModeEnabled)
            {
                _ = StartAlipayShellAsync();
            }

        }

        /// <summary>
        /// 启动支付宝插件
        /// </summary>
        /// <returns></returns>
        private async Task StartAlipayShellAsync()
        {
            if (IsNewPluginLifecycleModeEnabled)
            {
                return;
            }

            var isValid = await _licenseService.ValidateLicenseAsync();

            if (isValid)
            {
                //if (!_pluginProcessService.IsPluginRunning(PluginSettings.ALIPAY_SHELL))
                //{
                //    await _pluginProcessService.StartAlipayShellAsync();
                //}

                await _channelCoordinator.StartAlipayShellAsync();
            }


        }

        private void OnAPModuleToUi(string obj)
        {

            GlobalSettings.IsAlipayShellStarted = true;


            switch (obj)
            {
                // 开启成功
                case "Success":
                case "GetInfoSuccess":
                    _isAlipayEnabled = true;
                    IsConfiguring = false;
                    _notificationService.ShowSuccess("启用支付宝成功");

                    // 启动保活（包含心跳检测和进程检测）
                    // 低配置设备占用资源较大，停用心跳检测
                    // StartPluginKeepAlive();
                    break;

                //正在获取APPInfo
                case "GetInfo":
                    _isAlipayEnabled = false;
                    // IsConfiguring 保持为 true，因为还在配置中
                    _notificationService.ShowInfo("正在配置支付宝插件，请稍后...");
                    break;

                case "GetInfoFail":
                    _isAlipayEnabled = false;
                    IsConfiguring = false;
                    _notificationService.ShowInfo("支付宝配置失败，请联系管理员");
                    break;

                case "Disable":
                case "PollingStoped":
                    _isAlipayEnabled = false;
                    IsConfiguring = false;
                    _notificationService.ShowInfo("支付宝收款播报已关闭");

                    GlobalSettings.IsAlipayPoolingStarted = false;

                    // 停止保活
                    //低配置设备占用资源较大，停用心跳检测
                    // StopPluginKeepAlive();
                    break;

                // 心跳响应
                case "HeartbeatResponse":
                    if (!IsNewPluginLifecycleModeEnabled)
                    {
                        _pluginKeepAliveService.NotifyHeartbeatResponse(PluginSettings.AlipayShellExe);
                    }
                    break;

                // 支付宝插件已启动
                case "AppStarted":
                    _appStarted = true;
                    break;

                // 取消登录
                case "CanceledLogin":
                    _notificationService.ShowInfo("取消登录");
                    _isAlipayEnabled = false;
                    IsConfiguring = false;
                    break;

            }
            RaisePropertyChanged(nameof(IsAlipayEnabled));

            SavePaymentSettings();
        }

        /// <summary>
        /// 启动插件保活（包含进程检测和心跳检测）
        /// </summary>
        private void StartPluginKeepAlive()
        {
            if (IsNewPluginLifecycleModeEnabled)
            {
                return;
            }

            _pluginKeepAliveService.StartMonitoring(
                PluginSettings.AlipayShellExe,
                async () => await RestartAlipayShellAsync(),
                _CurrentEventAggregator
            );
        }

        /// <summary>
        /// 停止插件保活
        /// </summary>
        private void StopPluginKeepAlive()
        {
            if (IsNewPluginLifecycleModeEnabled)
            {
                return;
            }

            _pluginKeepAliveService.StopMonitoring(PluginSettings.AlipayShellExe);
        }

        /// <summary>
        /// 重启 AlipayShell 插件
        /// </summary>
        private async Task RestartAlipayShellAsync()
        {
            try
            {
                if (IsNewPluginLifecycleModeEnabled)
                {
                    GlobalSettings.CurrentAppContext.CurrentLogger.Info(
                        "[SettingsPageViewModel] New plugin lifecycle mode enabled, skip restarting Alipay shell from app.");
                    return;
                }

                if (_pluginProcessService.IsPluginRunning(PluginSettings.AlipayShellExe))
                {
                    return; // 已经在运行，不需要重启
                }

                // 重置 AppStarted 标志，等待插件重新发送 AppStarted 事件
                _appStarted = false;
                _notificationService.ShowInfo("正在重启支付宝插件...");

                await _pluginProcessService.StartAlipayShellAsync();
            }
            catch (Exception ex)
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Error($"重启 AlipayShell 插件失败: {ex.Message}");
            }
        }


        #endregion

        #region 支付设置管理

        /// <summary>
        /// 加载支付设置
        /// </summary>
        private void LoadPaymentSettings()
        {
            try
            {
                var settings = _appContext.CurrentSettings;

                // 如果未激活，强制禁用支付方式
                if (!_isActivated)
                {
                    _isAlipayEnabled = false;
                    _isWeChatEnabled = false;

                    // 如果配置中是启用状态，更新为禁用
                    if (settings.IsAlipayEnabled || settings.IsWeChatEnabled)
                    {
                        settings.IsAlipayEnabled = false;
                        settings.IsWeChatEnabled = false;
                        settings.LastUpdateDateTime = DateTime.Now;
                        _appContext.SaveCurrentSettings(settings);
                    }
                }
                else
                {
                    // 已激活，加载配置
                    _isAlipayEnabled = settings.IsAlipayEnabled;
                    _isWeChatEnabled = settings.IsWeChatEnabled;
                }

                // 加载昵称提醒开关
                _useNewPluginLifecycleMode = settings.UseNewPluginLifecycleMode;
                _isAlipayNicknameReminderEnabled = settings.IsAlipayNicknameReminderEnabled;
                _isWeChatNicknameReminderEnabled = settings.IsWeChatNicknameReminderEnabled;

                // 加载店铺名称和维护联系方式
                _storeName = settings.StoreName;
                _maintenanceContact = settings.MaintenanceContact;

                // 加载开机自启设置 - 从任务计划程序读取实际状态
                _isAutoStartEnabled = AutoStartupService.IsEnabled();
                // 如果实际状态与配置不一致，更新配置
                if (_isAutoStartEnabled != settings.IsAutoStartEnabled)
                {
                    settings.IsAutoStartEnabled = _isAutoStartEnabled;
                    _appContext.SaveCurrentSettings(settings);
                }

                // 通知UI更新（不触发保存）
                RaisePropertyChanged(nameof(IsAlipayEnabled));
                RaisePropertyChanged(nameof(IsWeChatEnabled));
                RaisePropertyChanged(nameof(IsAlipayNicknameReminderEnabled));
                RaisePropertyChanged(nameof(IsWeChatNicknameReminderEnabled));
                RaisePropertyChanged(nameof(IsAutoStartEnabled));
                RaisePropertyChanged(nameof(UseNewPluginLifecycleMode));
                RaisePropertyChanged(nameof(StoreName));
                RaisePropertyChanged(nameof(MaintenanceContact));
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"加载支付设置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 启用支付宝
        /// </summary>
        private async Task ValidateAndEnableAlipay()
        {

            //#if DEBUG
            //            if (SetProperty(ref _isAlipayEnabled, true))
            //            {
            //                SavePaymentSettings();
            //            }

            //            return;
            //#endif


            #region 正常方案




            //if (!_pluginProcessService.IsPluginRunning(PluginSettings.ALIPAY_SHELL))
            //{
            //    await _pluginProcessService.StartAlipayShellAsync();
            //    await Task.Delay(2500);

            //}

            _CurrentEventAggregator.GetEvent<UIToAPModuleEvent>().Publish("Enable");
            #endregion
            #region 备用方案


            //try
            //{
            //    // 检查数据库中是否有支付宝配置
            //    var configs = await _alipayConfigService.GetAllConfigsAsync();

            //    if (configs == null || configs.Count == 0)
            //    {
            //        // 没有配置，提示用户
            //        MessageBox.Show(
            //            "请先配置支付宝授权信息后再启用支付宝支付。",
            //            "提示",
            //            MessageBoxButton.OK,
            //            MessageBoxImage.Warning
            //        );


            //        await _pluginProcessService.StartAlipayConfigClientAsync();


            //        // 强制刷新UI，保持为false
            //        RaisePropertyChanged(nameof(IsAlipayEnabled));
            //        return;
            //    }

            //    // 有配置，允许启用
            //    if (SetProperty(ref _isAlipayEnabled, true))
            //    {
            //        SavePaymentSettings();
            //    }
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show($"验证支付宝配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);

            //    // 出错时保持为false
            //    RaisePropertyChanged(nameof(IsAlipayEnabled));
            //}
            #endregion 备用方案
        }

        /// <summary>
        /// 保存支付设置
        /// </summary>
        private void SavePaymentSettings()
        {
            try
            {
                var settings = _appContext.CurrentSettings;
                settings.IsAlipayEnabled = _isAlipayEnabled;
                settings.IsWeChatEnabled = _isWeChatEnabled;
                settings.IsAlipayNicknameReminderEnabled = _isAlipayNicknameReminderEnabled;
                settings.IsWeChatNicknameReminderEnabled = _isWeChatNicknameReminderEnabled;
                settings.IsAutoStartEnabled = _isAutoStartEnabled;
                settings.UseNewPluginLifecycleMode = _useNewPluginLifecycleMode;
                settings.StoreName = _storeName;
                settings.MaintenanceContact = _maintenanceContact;
                settings.LastUpdateDateTime = DateTime.Now;

                _appContext.SaveCurrentSettings(settings);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"保存支付设置失败: {ex.Message}");
            }
        }

        #endregion

        #region 激活管理

        /// <summary>
        /// 加载激活信息
        /// </summary>
        private async Task LoadLicenseInfoAsync()
        {
            try
            {
                // 获取机器序列号
                MachineSN = _licenseService.GetLocalKey();

                // 获取激活信息
                var license = await _licenseService.GetLicenseInfoAsync();
                if (license != null)
                {
                    IsActivated = true;
                    RaisePropertyChanged(nameof(IsPluginControlsEnabled));
                    RaisePropertyChanged(nameof(PluginControlsDisabledHint));
                    ExpiresAt = license.MaxDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                    RemainingDays = (license.MaxDateTime - DateTime.Now).Days;

                    if (RemainingDays > 0)
                    {
                        ActivationStatus = $"已激活（剩余{RemainingDays}天）";
                    }
                    else
                    {
                        ActivationStatus = "已过期";
                        IsActivated = false;
                    }

                    // 显示激活文件路径
                    LicenseFilePath = _licenseService.GetLicenseFilePath();
                }
                else
                {
                    IsActivated = false;
                    RaisePropertyChanged(nameof(IsPluginControlsEnabled));
                    RaisePropertyChanged(nameof(PluginControlsDisabledHint));
                    ActivationStatus = "未激活";
                    ExpiresAt = "未激活";
                    RemainingDays = 0;
                    LicenseFilePath = string.Empty;
                }

                // 激活状态变化后，重新加载支付设置（会强制禁用未激活时的支付方式）
                LoadPaymentSettings();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"加载激活信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 选择激活文件
        /// </summary>
        private void SelectKeyFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "选择激活文件",
                Filter = "激活文件 (*.key)|*.key|所有文件 (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LicenseFilePath = openFileDialog.FileName;

                _ = ActivateAsync();

            }
        }

        /// <summary>
        /// 执行激活
        /// </summary>
        private async Task ActivateAsync()
        {
            if (string.IsNullOrWhiteSpace(LicenseFilePath))
            {
                _dialogService.ShowWarning("请先选择激活文件");
                return;
            }

            IsActivating = true;

            try
            {
                var result = await _licenseService.ActivateFromKeyFileAsync(LicenseFilePath);
                if (result.Success)
                {
                    _dialogService.ShowSuccess(result.Message, "激活成功，请重启软件");
                    await LoadLicenseInfoAsync();
                }
                else
                {
                    _dialogService.ShowError(result.Message, "激活失败，请联系管理员");
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"激活过程出错: {ex.Message}");
            }
            finally
            {
                IsActivating = false;
            }
        }

        /// <summary>
        /// 判断是否可以激活
        /// </summary>
        /// <returns>是否可以激活</returns>
        private bool CanActivate()
        {
            return !IsActivating && !string.IsNullOrWhiteSpace(LicenseFilePath);
        }

        /// <summary>
        /// 复制机器序列号到剪贴板
        /// </summary>
        private void CopySN()
        {
            try
            {
                SetClipboardText(MachineSN);
                _dialogService.ShowInfo("机器序列号已复制到剪贴板");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("复制失败，请手动复制机器序列号");
            }
        }

        /// <summary>
        /// 设置剪贴板文本（带重试机制，兼容低版本系统）
        /// </summary>
        private void SetClipboardText(string text)
        {
            const int maxRetries = 10;
            const int baseDelayMs = 50;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    _CurrentDeviceRequestService.CurrentApplication.Dispatcher.Invoke(() =>
                    {
                        Clipboard.Clear();
                        Clipboard.SetDataObject(text, true);
                    });
                    return;
                }
                catch (System.Runtime.InteropServices.COMException ex) when (ex.HResult == unchecked((int)0x800401D0))
                {
                    // CLIPBRD_E_CANT_OPEN - 剪切板被占用
                    if (i == maxRetries - 1)
                    {
                        // 最后一次失败，记录占用进程信息
                        var ownerInfo = GetClipboardOwnerInfo();
                        GlobalSettings.CurrentAppContext.CurrentLogger.Error(
                            $"剪切板被占用，无法复制。占用进程: {ownerInfo}");
                        throw new InvalidOperationException($"剪切板被占用: {ownerInfo}", ex);
                    }
                    // 指数退避
                    System.Threading.Thread.Sleep(baseDelayMs * (i + 1));
                }
            }
        }

        /// <summary>
        /// 获取当前占用剪切板的进程信息
        /// </summary>
        /// <returns>进程信息字符串</returns>
        private string GetClipboardOwnerInfo()
        {
            try
            {
                IntPtr hwnd = GetOpenClipboardWindow();

                if (hwnd == IntPtr.Zero)
                {
                    return "未知进程（无法获取窗口句柄）";
                }

                uint processId;
                GetWindowThreadProcessId(hwnd, out processId);

                if (processId == 0)
                {
                    return $"窗口句柄: {hwnd}, 但无法获取进程ID";
                }

                try
                {
                    var process = Process.GetProcessById((int)processId);
                    return $"{process.ProcessName} (PID: {processId}, 路径: {process.MainModule?.FileName ?? "未知"})";
                }
                catch (Exception ex)
                {
                    return $"进程ID: {processId}, 但无法获取详细信息 ({ex.Message})";
                }
            }
            catch (Exception ex)
            {
                return $"获取剪切板占用信息失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 返回主页
        /// </summary>
        private void OnGoBack()
        {
            if (!_isActivated)
                return;
            NavigationToRoot<MainPageViewModel>(PaymentRegionNames.PaymentContentRegion);
        }

        /// <summary>
        /// 退出应用
        /// </summary>
        private void OnExit()
        {
            _dialogService.ShowConfirm(
                "确定要退出收银台系统吗？",
                "确认退出",
                async result =>
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        await _pluginProcessService.CleanupExistingProcessesAsync();
                        Application.Current.Shutdown();
                    }
                });
        }

        #endregion

        #region 消息通知配置管理

        /// <summary>
        /// 检查知情同意并启用通知
        /// </summary>
        private async Task CheckConsentAndEnableNotificationAsync()
        {
            try
            {
                // 每次启用都显示知情同意书对话框
                var tcs = new TaskCompletionSource<bool>();

                _dialogService.ShowDialog(
                    "NotificationConsentDialog",
                    new DialogParameters(),
                    async result =>
                    {
                        if (result.Result == ButtonResult.OK)
                        {
                            // 用户同意，保存记录
                            var consent = new DboModels.NotificationConsentDbo
                            {
                                ConsentTime = DateTime.Now,
                                IsAgreed = true,
                                MachineId = GetMachineId(),
                                Remarks = "用户同意启用消息通知服务"
                            };

                            var saved = await _paymentRepository.SaveNotificationConsentAsync(consent);

                            if (saved)
                            {
                                // 启用通知
                                _isNotificationEnabled = true;
                                RaisePropertyChanged(nameof(IsNotificationEnabled));
                                await SaveNotificationConfigAsync();
                                _dialogService.ShowSuccess("消息通知已启用");
                                tcs.SetResult(true);
                            }
                            else
                            {
                                _dialogService.ShowError("保存知情同意记录失败");
                                RaisePropertyChanged(nameof(IsNotificationEnabled));
                                tcs.SetResult(false);
                            }
                        }
                        else
                        {
                            // 用户拒绝
                            _dialogService.ShowInfo("您已拒绝知情同意书，无法启用消息通知");
                            RaisePropertyChanged(nameof(IsNotificationEnabled));
                            tcs.SetResult(false);
                        }
                    });

                await tcs.Task;
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"启用消息通知失败: {ex.Message}");
                RaisePropertyChanged(nameof(IsNotificationEnabled));
            }
        }

        /// <summary>
        /// 保存通知配置
        /// </summary>
        private async Task SaveNotificationConfigAsync()
        {
            try
            {
                var config = await _paymentRepository.GetNotificationConfigAsync();

                if (config == null)
                {
                    config = new DboModels.NotificationConfigDbo
                    {
                        IsEnabled = _isNotificationEnabled,
                        NotificationUrl = _notificationUrl
                    };
                }
                else
                {
                    config.IsEnabled = _isNotificationEnabled;
                    config.NotificationUrl = _notificationUrl;
                }

                await _paymentRepository.SaveNotificationConfigAsync(config);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"保存通知配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载通知配置
        /// </summary>
        private async Task LoadNotificationConfigAsync()
        {
            try
            {
                var config = await _paymentRepository.GetNotificationConfigAsync();

                if (config != null)
                {
                    _notificationUrl = config.NotificationUrl;
                    _isNotificationEnabled = config.IsEnabled && _isActivated; // 未激活时强制禁用

                    RaisePropertyChanged(nameof(NotificationUrl));
                    RaisePropertyChanged(nameof(IsNotificationEnabled));
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"加载通知配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取机器唯一标识
        /// </summary>
        private string GetMachineId()
        {
            try
            {
                return Environment.MachineName;
            }
            catch
            {
                return "Unknown";
            }
        }

        #endregion

        #region 知情同意书和版本信息

        /// <summary>
        /// 获取软件版本号
        /// </summary>
        private string GetAppVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetEntryAssembly();
                if (assembly != null)
                {
                    var version = assembly.GetName().Version;
                    return $"V{version?.Major}.{version?.Minor}.{version?.Build}";
                }
            }
            catch (Exception ex)
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Error($"获取版本号失败: {ex.Message}");
            }
            return "V1.0.0";
        }

        /// <summary>
        /// 显示免责条款与知情同意书
        /// </summary>
        private void ShowDisclaimer()
        {
            try
            {
                var parameters = new DialogParameters
                {
                    { "Title", "免责条款与知情同意书" },
                    { "FilePath", "免责条款与知情同意书.md" },
                    { "IsReadOnlyMode", true }
                };

                _dialogService.ShowDialog("NotificationConsentDialog", parameters, result =>
                {
                    // 只读模式下无需处理结果
                });
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"打开文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示支付消息通知服务知情同意书
        /// </summary>
        private void ShowNotificationConsent()
        {
            try
            {
                var parameters = new DialogParameters
                {
                    { "Title", "支付消息通知服务知情同意书" },
                    { "FilePath", "支付消息通知服务知情同意书.md" },
                    { "IsReadOnlyMode", true }
                };

                _dialogService.ShowDialog("NotificationConsentDialog", parameters, result =>
                {
                    // 只读模式下无需处理结果
                });
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"打开文件失败: {ex.Message}");
            }
        }

        #endregion
    }
}
