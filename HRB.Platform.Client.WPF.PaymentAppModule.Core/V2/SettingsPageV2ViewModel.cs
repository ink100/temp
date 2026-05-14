using HRB.Payment.Core.Services;
using HRB.Platform.Client.Core.ExtensionFunctions;
using HRB.Platform.Client.WPF.Core.Services.IServices;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Abstractions;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.DboModels;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Extensions;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Plugins.Alipay;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Plugins.WeChat;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Repository;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Services;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.ViewModels;
using Edge_tts_sharp;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MediaBrush = System.Windows.Media.Brush;
using WinForms = System.Windows.Forms;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Brushes = System.Windows.Media.Brushes;
using Application = System.Windows.Application;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Clipboard = System.Windows.Clipboard;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.V2
{
    /// <summary>
    /// 2.0 设置页 ViewModel — 纯设置壳。
    ///
    /// 固定区域：激活/店铺/自启动/通知/版本
    /// 插件区域：通过 IChannelSettingsContributor 动态聚合各插件设置项
    ///
    /// 砍掉：APModuleToUIEvent 事件处理、插件进程管理、保活、旧模式分支
    /// </summary>
    public class SettingsPageV2ViewModel : BasePaymentRegionViewModel
    {
        #region Win32

        [DllImport("user32.dll")]
        private static extern IntPtr GetOpenClipboardWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        #endregion

        private readonly PaymentAppContext _appContext;
        private readonly ILicenseService _licenseService;
        private readonly INotificationService _notificationService;
        private readonly IDialogService _dialogService;
        private readonly IPaymentRepository _paymentRepository;
        private readonly AlipaySettingsContributor _alipayContributor;
        private readonly WeChatSettingsContributor _weChatContributor;
        private readonly AlipayChannelPlugin _alipayPlugin;
        private readonly WeChatChannelPlugin _weChatPlugin;
        private readonly IPluginRuntimeStatusService _pluginRuntimeStatusService;
        private readonly IPaymentVoiceService _paymentVoiceService;
        private bool _isNotificationEnabled;
        private bool _isLoadingGeneralSettings;

        private IEnumerable<IChannelSettingsContributor> AllContributors =>
            [_alipayContributor, _weChatContributor];

        #region 属性

        public override bool KeepAlive { get; } = false;

        // 激活相关
        public string MachineSN { get; set => SetProperty(ref field, value); } = string.Empty;
        public string ActivationStatus { get; set => SetProperty(ref field, value); } = "未激活";
        public string ExpiresAt { get; set => SetProperty(ref field, value); } = "未激活";
        public int RemainingDays { get; set => SetProperty(ref field, value); }
        public bool IsActivated { get; set => SetProperty(ref field, value); }
        public bool IsActivating { get; set => SetProperty(ref field, value); }
        public string LicenseFilePath { get; set => SetProperty(ref field, value); } = string.Empty;

        // 店铺信息
        public string StoreName
        {
            get;
            set
            {
                if (SetProperty(ref field, value) && !_isLoadingGeneralSettings)
                    SaveGeneralSettings();
            }
        } = string.Empty;

        public string MaintenanceContact
        {
            get;
            set
            {
                if (SetProperty(ref field, value) && !_isLoadingGeneralSettings)
                    SaveGeneralSettings();
            }
        } = string.Empty;

        // 自启动
        public bool IsAutoStartEnabled
        {
            get;
            set
            {
                if (SetProperty(ref field, value))
                {
                    if (_isLoadingGeneralSettings)
                        return;

                    var success = AutoStartupService.SetEnabled(value);
                    if (!success)
                    {
                        _dialogService.ShowError("设置开机自启动失败，请以管理员权限运行程序");
                        field = !value;
                        RaisePropertyChanged(nameof(IsAutoStartEnabled));
                        return;
                    }
                    SaveGeneralSettings();
                }
            }
        }

        // 新交易自动滚动到顶部
        public bool IsAutoScrollToLatestEnabled
        {
            get;
            set
            {
                if (SetProperty(ref field, value) && !_isLoadingGeneralSettings)
                    SaveGeneralSettings();
            }
        }

        // 每天凌晨 1 点自动同步 Windows 系统时间
        public bool IsAutoSyncSystemTimeEnabled
        {
            get;
            set
            {
                if (SetProperty(ref field, value) && !_isLoadingGeneralSettings)
                {
                    SaveGeneralSettings();
                    SystemTimeSyncService.RefreshSchedule();
                }
            }
        }

        public bool IsSyncingSystemTime
        {
            get;
            set => SetProperty(ref field, value);
        }

        public string LastSystemTimeSyncText
        {
            get;
            set => SetProperty(ref field, value);
        } = "尚未同步";


        // 语音与显示设置
        public bool IsScanNotPayVoiceEnabled
        {
            get;
            set
            {
                if (SetProperty(ref field, value) && !_isLoadingGeneralSettings)
                    SaveGeneralSettings();
            }
        } = true;

        public int ScanTimeoutSeconds
        {
            get;
            set
            {
                var safeValue = Math.Clamp(value, 1, 3600);
                if (SetProperty(ref field, safeValue) && !_isLoadingGeneralSettings)
                    SaveGeneralSettings();
            }
        } = 10;

        public int ScanNotPayNotifyIntervalSeconds
        {
            get;
            set
            {
                var safeValue = Math.Clamp(value, 1, 3600);
                if (SetProperty(ref field, safeValue) && !_isLoadingGeneralSettings)
                    SaveGeneralSettings();
            }
        } = 10;

        public int ScanNotPayVoiceRepeatCount
        {
            get;
            set
            {
                var safeValue = Math.Clamp(value, 1, 20);
                if (SetProperty(ref field, safeValue) && !_isLoadingGeneralSettings)
                    SaveGeneralSettings();
            }
        } = 1;

        public int VoiceRepeatIntervalSeconds
        {
            get;
            set
            {
                var safeValue = Math.Clamp(value, 1, 60);
                if (SetProperty(ref field, safeValue) && !_isLoadingGeneralSettings)
                    SaveGeneralSettings();
            }
        } = 1;

        public bool IsPriorUnpaidVoiceEnabled
        {
            get;
            set
            {
                if (SetProperty(ref field, value) && !_isLoadingGeneralSettings)
                    SaveGeneralSettings();
            }
        } = true;

        public int PriorUnpaidVoiceRepeatCount
        {
            get;
            set
            {
                var safeValue = Math.Clamp(value, 1, 10);
                if (SetProperty(ref field, safeValue) && !_isLoadingGeneralSettings)
                    SaveGeneralSettings();
            }
        } = 2;

        public bool IsPaymentCancelledVoiceEnabled
        {
            get;
            set
            {
                if (SetProperty(ref field, value) && !_isLoadingGeneralSettings)
                    SaveGeneralSettings();
            }
        } = true;

        public int PaymentCancelledVoiceRepeatCount
        {
            get;
            set
            {
                var safeValue = Math.Clamp(value, 1, 10);
                if (SetProperty(ref field, safeValue) && !_isLoadingGeneralSettings)
                    SaveGeneralSettings();
            }
        } = 2;

        public bool IsPaymentSuccessVoiceEnabled
        {
            get;
            set
            {
                if (SetProperty(ref field, value) && !_isLoadingGeneralSettings)
                    SaveGeneralSettings();
            }
        } = true;

        public ObservableCollection<string> TtsVoiceOptions { get; } = new();

        public string SelectedTtsVoiceName
        {
            get;
            set
            {
                if (SetProperty(ref field, value) && !_isLoadingGeneralSettings)
                    SaveGeneralSettings();
            }
        } = string.Empty;

        public int TtsRate
        {
            get;
            set
            {
                var safeValue = Math.Clamp(value, -50, 100);
                if (SetProperty(ref field, safeValue) && !_isLoadingGeneralSettings)
                    SaveGeneralSettings();
            }
        }

        public int TtsVolume
        {
            get;
            set
            {
                var safeValue = Math.Clamp(value, 0, 100);
                if (SetProperty(ref field, safeValue) && !_isLoadingGeneralSettings)
                    SaveGeneralSettings();
            }
        } = 100;

        public double DisplayFontSize
        {
            get;
            set
            {
                var safeValue = Math.Clamp(value, 24, 96);
                if (SetProperty(ref field, safeValue) && !_isLoadingGeneralSettings)
                    SaveGeneralSettings();
            }
        } = 60;

        public string AmountColorHex
        {
            get;
            set
            {
                if (SetProperty(ref field, NormalizeColorHex(value)) && !_isLoadingGeneralSettings)
                {
                    AmountColorPreviewBrush = BuildBrush(field);
                    SaveGeneralSettings();
                }
            }
        } = "#F01F1B";

        public MediaBrush AmountColorPreviewBrush
        {
            get;
            set => SetProperty(ref field, value);
        } = Brushes.Red;

        // 通知
        public string NotificationUrl { get; set => SetProperty(ref field, value); } = string.Empty;
        public bool IsNotificationEnabled
        {
            get => _isNotificationEnabled;
            set
            {
                if (!IsActivated && value)
                {
                    _dialogService.ShowWarning("请先激活软件后再启用消息通知功能");
                    RaisePropertyChanged();
                    return;
                }
                if (value && string.IsNullOrWhiteSpace(NotificationUrl))
                {
                    _dialogService.ShowWarning("请先配置通知接口URL");
                    RaisePropertyChanged();
                    return;
                }
                if (value && !_isNotificationEnabled)
                    _ = CheckConsentAndEnableNotificationAsync();
                else if (!value && _isNotificationEnabled)
                {
                    if (SetProperty(ref _isNotificationEnabled, value))
                        _ = SaveNotificationConfigAsync();
                }
            }
        }

        // 版本
        public string AppVersion { get; set => SetProperty(ref field, value); } = string.Empty;

        /// <summary>
        /// 插件渠道设置项（动态聚合）
        /// </summary>
        public ObservableCollection<ChannelSettingItem> ChannelSettings { get; } = new();

        #endregion

        #region 命令

        public ICommand SelectKeyFileCommand { get; }
        public ICommand ActivateCommand { get; }
        public ICommand CopySNCommand { get; }
        public ICommand GoBackCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand ShowDisclaimerCommand { get; }
        public ICommand ShowNotificationConsentCommand { get; }
        public ICommand SaveNotificationCommand { get; }
        public ICommand ToggleChannelSettingCommand { get; }
        public ICommand SyncSystemTimeCommand { get; }
        public ICommand TestVoiceCommand { get; }
        public ICommand ChooseAmountColorCommand { get; }

        public ICommand ConnectMessageCenterCommand { get; }

        #endregion

        public SettingsPageV2ViewModel(
            PaymentAppContext appContext,
            IEventAggregator eventAggregator,
            IRegionManager regionManager,
            IWpfDeviceRequestService deviceRequestService,
            ILicenseService licenseService,
            INotificationService notificationService,
            IDialogService dialogService,
            IPaymentRepository paymentRepository,
            AlipaySettingsContributor alipayContributor,
            WeChatSettingsContributor weChatContributor,
            AlipayChannelPlugin alipayPlugin,
            WeChatChannelPlugin weChatPlugin,
            IPluginRuntimeStatusService pluginRuntimeStatusService,
            IPaymentVoiceService paymentVoiceService,
            IWeChatConnectionService weChatConnectionService
        ) : base(appContext, eventAggregator, regionManager, deviceRequestService)
        {
            _appContext = appContext;
            _licenseService = licenseService;
            _notificationService = notificationService;
            _dialogService = dialogService;
            _paymentRepository = paymentRepository;
            _alipayContributor = alipayContributor;
            _weChatContributor = weChatContributor;
            _alipayPlugin = alipayPlugin;
            _weChatPlugin = weChatPlugin;
            _pluginRuntimeStatusService = pluginRuntimeStatusService;
            _paymentVoiceService = paymentVoiceService;


            // 命令
            SelectKeyFileCommand = new DelegateCommand(SelectKeyFile);
            ActivateCommand = new DelegateCommand(async () => await ActivateAsync());
            CopySNCommand = new DelegateCommand(CopySN);
            GoBackCommand = new DelegateCommand(OnGoBack);
            ExitCommand = new DelegateCommand(OnExit);
            ShowDisclaimerCommand = new DelegateCommand(ShowDisclaimer);
            ShowNotificationConsentCommand = new DelegateCommand(ShowNotificationConsent);
            SaveNotificationCommand = new DelegateCommand(async () => await SaveNotificationConfigAsync());
            ToggleChannelSettingCommand = new DelegateCommand<ChannelSettingItem>(OnToggleChannelSetting);
            SyncSystemTimeCommand = new DelegateCommand(async () => await SyncSystemTimeNowAsync());
            TestVoiceCommand = new DelegateCommand(async () => await TestVoiceAsync());
            ChooseAmountColorCommand = new DelegateCommand(ChooseAmountColor);

            ConnectMessageCenterCommand = new DelegateCommand(() =>
            {

                weChatConnectionService.StartAsync();

            });



            _pluginRuntimeStatusService.StatusChanged += _pluginRuntimeStatusService_StatusChanged;

            _alipayPlugin.StatusChanged += OnChannelStatusChanged;
            _weChatPlugin.StatusChanged += OnChannelStatusChanged;

            // 初始化
            AppVersion = GetAppVersion();
            LoadTtsVoiceOptions();
            _ = LoadLicenseInfoAsync();
            LoadGeneralSettings();
            SystemTimeSyncService.Start(_appContext);
            _ = LoadNotificationConfigAsync();
            RefreshChannelSettings();
        }

        private void _pluginRuntimeStatusService_StatusChanged(object? sender, EventArgs e)
        {
            Application.Current?.Dispatcher.BeginInvoke(RefreshChannelSettings);
        }

        private void OnChannelStatusChanged(object? sender, ChannelStatusChangedEventArgs e)
        {
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                if (!string.IsNullOrEmpty(e.Message))
                {
                    _notificationService.ShowInfo(e.Message);
                }
                RefreshChannelSettings();
            });
        }

        #region 插件渠道设置

        private void RefreshChannelSettings()
        {
            ChannelSettings.Clear();
            foreach (var contributor in AllContributors)
                foreach (var item in contributor.GetSettings().OrderBy(s => s.Order))
                    ChannelSettings.Add(item);
        }

        private async void OnToggleChannelSetting(ChannelSettingItem? item)
        {
            if (item is not { Type: SettingType.Toggle })
                return;
            if (!item.IsEnabled)
                return;

            if (!IsActivated)
            {
                _dialogService.ShowWarning("请先激活软件后再配置支付方式");
                return;
            }

            var newValue = !(bool)(item.CurrentValue ?? false);

            foreach (var contributor in AllContributors)
            {
                if (contributor.GetSettings().Any(s => s.Key == item.Key))
                {
                    await contributor.OnSettingChangedAsync(item.Key, newValue);
                    break;
                }
            }

            RefreshChannelSettings();
        }

        #endregion

        #region 通用设置

        private void LoadGeneralSettings()
        {
            var settings = _appContext.CurrentSettings;

            _isLoadingGeneralSettings = true;
            try
            {
                // 加载时不要走属性 setter 自动保存。
                // 否则先给 StoreName 赋值时，会把尚未加载的 MaintenanceContact 空值写回配置，导致维护联系方式丢失。
                StoreName = settings.StoreName;
                MaintenanceContact = settings.MaintenanceContact;

                var actualAutoStartEnabled = AutoStartupService.IsEnabled();
                IsAutoStartEnabled = actualAutoStartEnabled;
                IsAutoScrollToLatestEnabled = settings.IsAutoScrollToLatestEnabled;
                IsAutoSyncSystemTimeEnabled = settings.IsAutoSyncSystemTimeEnabled;
                LastSystemTimeSyncText = BuildLastSystemTimeSyncText(settings.LastSystemTimeSyncTime, settings.LastSystemTimeSyncResult);

                IsScanNotPayVoiceEnabled = settings.IsScanNotPayVoiceEnabled;
                ScanTimeoutSeconds = Math.Clamp(settings.ScanTimeoutSeconds <= 0 ? 10 : settings.ScanTimeoutSeconds, 1, 3600);
                ScanNotPayNotifyIntervalSeconds = Math.Clamp(settings.ScanNotPayNotifyIntervalSeconds <= 0 ? 10 : settings.ScanNotPayNotifyIntervalSeconds, 1, 3600);
                ScanNotPayVoiceRepeatCount = Math.Clamp(settings.ScanNotPayVoiceRepeatCount <= 0 ? 1 : settings.ScanNotPayVoiceRepeatCount, 1, 20);
                VoiceRepeatIntervalSeconds = Math.Clamp(settings.VoiceRepeatIntervalSeconds <= 0 ? 1 : settings.VoiceRepeatIntervalSeconds, 1, 60);
                IsPriorUnpaidVoiceEnabled = settings.IsPriorUnpaidVoiceEnabled;
                PriorUnpaidVoiceRepeatCount = Math.Clamp(settings.PriorUnpaidVoiceRepeatCount, 1, 10);
                IsPaymentCancelledVoiceEnabled = settings.IsPaymentCancelledVoiceEnabled;
                PaymentCancelledVoiceRepeatCount = Math.Clamp(settings.PaymentCancelledVoiceRepeatCount, 1, 10);
                IsPaymentSuccessVoiceEnabled = settings.IsPaymentSuccessVoiceEnabled;
                SelectedTtsVoiceName = settings.TtsVoiceName;
                TtsRate = Math.Clamp(settings.TtsRate, -50, 100);
                TtsVolume = Math.Clamp(settings.TtsVolume, 0, 100);
                DisplayFontSize = Math.Clamp(settings.FontSize, 24, 96);
                AmountColorHex = NormalizeColorHex(settings.AmountColorHex);
                AmountColorPreviewBrush = BuildBrush(AmountColorHex);

                // 仅在任务计划程序实际状态和配置不一致时，同步一次配置。
                if (settings.IsAutoStartEnabled != actualAutoStartEnabled)
                {
                    settings.IsAutoStartEnabled = actualAutoStartEnabled;
                    settings.LastUpdateDateTime = DateTime.Now;
                    _appContext.SaveCurrentSettings(settings);
                }
            }
            finally
            {
                _isLoadingGeneralSettings = false;
            }
        }

        private void SaveGeneralSettings()
        {
            var settings = _appContext.CurrentSettings;
            settings.StoreName = StoreName;
            settings.MaintenanceContact = MaintenanceContact;
            settings.IsAutoStartEnabled = IsAutoStartEnabled;
            settings.IsAutoScrollToLatestEnabled = IsAutoScrollToLatestEnabled;
            settings.IsAutoSyncSystemTimeEnabled = IsAutoSyncSystemTimeEnabled;
            settings.IsScanNotPayVoiceEnabled = IsScanNotPayVoiceEnabled;
            settings.ScanTimeoutSeconds = Math.Clamp(ScanTimeoutSeconds, 1, 3600);
            settings.ScanNotPayNotifyIntervalSeconds = Math.Clamp(ScanNotPayNotifyIntervalSeconds, 1, settings.ScanTimeoutSeconds);
            settings.ScanNotPayVoiceRepeatCount = Math.Clamp(ScanNotPayVoiceRepeatCount, 1, 20);
            settings.VoiceRepeatIntervalSeconds = Math.Clamp(VoiceRepeatIntervalSeconds, 1, 60);
            settings.IsPriorUnpaidVoiceEnabled = IsPriorUnpaidVoiceEnabled;
            settings.PriorUnpaidVoiceRepeatCount = Math.Clamp(PriorUnpaidVoiceRepeatCount, 1, 10);
            settings.IsPaymentCancelledVoiceEnabled = IsPaymentCancelledVoiceEnabled;
            settings.PaymentCancelledVoiceRepeatCount = Math.Clamp(PaymentCancelledVoiceRepeatCount, 1, 10);
            settings.IsPaymentSuccessVoiceEnabled = IsPaymentSuccessVoiceEnabled;
            settings.TtsVoiceName = SelectedTtsVoiceName ?? string.Empty;
            settings.TtsRate = Math.Clamp(TtsRate, -50, 100);
            settings.TtsVolume = Math.Clamp(TtsVolume, 0, 100);
            settings.FontSize = Math.Clamp(DisplayFontSize, 24, 96);
            settings.AmountColorHex = NormalizeColorHex(AmountColorHex);
            settings.LastUpdateDateTime = DateTime.Now;
            _appContext.SaveCurrentSettings(settings);
        }


        private static string GetVoiceDisplayName(Edge_tts_sharp.Model.eVoice voice)
        {
            var type = voice.GetType();
            return type.GetProperty("Name")?.GetValue(voice)?.ToString()
                ?? type.GetProperty("ShortName")?.GetValue(voice)?.ToString()
                ?? type.GetProperty("FriendlyName")?.GetValue(voice)?.ToString()
                ?? voice.ToString()
                ?? string.Empty;
        }

        private void LoadTtsVoiceOptions()
        {
            try
            {
                TtsVoiceOptions.Clear();
                foreach (var voice in Edge_tts.GetVoice()
                             .Where(v => string.Equals(v.Locale, "zh-CN", StringComparison.OrdinalIgnoreCase))
                             .Select(GetVoiceDisplayName)
                             .Where(v => !string.IsNullOrWhiteSpace(v))
                             .Distinct()
                             .OrderBy(v => v))
                {
                    TtsVoiceOptions.Add(voice);
                }
            }
            catch (Exception ex)
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Info($"加载 TTS 音色列表失败: {ex.Message}");
            }
        }

        private async Task TestVoiceAsync()
        {
            SaveGeneralSettings();
            await _paymentVoiceService.PlayPaymentStartedWithBeforeNotPayAsync(HRB.Payment.Core.Models.PaymentChannel.Alipay, "测试用户");
            await _paymentVoiceService.PlayPaymentSuccessAsync(12.34m, HRB.Payment.Core.Models.PaymentChannel.Alipay);
        }

        private void ChooseAmountColor()
        {
            try
            {
                using var dialog = new WinForms.ColorDialog
                {
                    FullOpen = true,
                    AnyColor = true
                };

                var currentColor = TryParseDrawingColor(AmountColorHex);
                if (currentColor != null)
                    dialog.Color = currentColor.Value;

                if (dialog.ShowDialog() == WinForms.DialogResult.OK)
                {
                    AmountColorHex = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowWarning($"打开颜色选择器失败：{ex.Message}");
            }
        }

        private static System.Drawing.Color? TryParseDrawingColor(string? colorHex)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(NormalizeColorHex(colorHex));
                return System.Drawing.Color.FromArgb(color.R, color.G, color.B);
            }
            catch
            {
                return null;
            }
        }

        private static string NormalizeColorHex(string? colorHex)
        {
            if (string.IsNullOrWhiteSpace(colorHex))
                return "#F01F1B";

            var value = colorHex.Trim();
            if (!value.StartsWith("#"))
                value = "#" + value;

            try
            {
                _ = (Color)ColorConverter.ConvertFromString(value);
                return value.ToUpperInvariant();
            }
            catch
            {
                return "#F01F1B";
            }
        }

        private static MediaBrush BuildBrush(string? colorHex)
        {
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(NormalizeColorHex(colorHex)));
            }
            catch
            {
                return Brushes.Red;
            }
        }

        private static string BuildLastSystemTimeSyncText(DateTime? syncTime, string? result)
        {
            if (syncTime == null && string.IsNullOrWhiteSpace(result))
                return "尚未同步";

            var timeText = syncTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "未知时间";
            return string.IsNullOrWhiteSpace(result) ? $"上次同步：{timeText}" : $"上次同步：{timeText}，{result}";
        }

        private async Task SyncSystemTimeNowAsync()
        {
            if (IsSyncingSystemTime)
                return;

            IsSyncingSystemTime = true;
            LastSystemTimeSyncText = "正在同步 Windows 系统时间...";

            try
            {
                var result = await SystemTimeSyncService.SyncNowAsync();

                var settings = _appContext.CurrentSettings;
                settings.LastSystemTimeSyncTime = DateTime.Now;
                settings.LastSystemTimeSyncResult = result.Message;
                settings.LastUpdateDateTime = DateTime.Now;
                _appContext.SaveCurrentSettings(settings);

                LastSystemTimeSyncText = BuildLastSystemTimeSyncText(settings.LastSystemTimeSyncTime, result.Message);

                if (result.Success)
                    _dialogService.ShowSuccess(result.Message);
                else
                    _dialogService.ShowWarning(result.Message + "。如果失败原因是权限不足，请以管理员权限运行程序后重试。");
            }
            finally
            {
                IsSyncingSystemTime = false;
            }
        }

        #endregion

        #region 激活管理

        private async Task LoadLicenseInfoAsync()
        {
            try
            {
                MachineSN = _licenseService.GetLocalKey();

                var license = await _licenseService.GetLicenseInfoAsync();
                if (license != null)
                {
                    IsActivated = true;
                    ExpiresAt = license.MaxDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                    RemainingDays = (license.MaxDateTime - DateTime.Now).Days;

                    if (RemainingDays > 0)
                        ActivationStatus = $"已激活（剩余{RemainingDays}天）";
                    else
                    {
                        ActivationStatus = "已过期";
                        IsActivated = false;
                    }

                    LicenseFilePath = _licenseService.GetLicenseFilePath();
                }
                else
                {
                    IsActivated = false;
                    ActivationStatus = "未激活";
                    ExpiresAt = "未激活";
                    RemainingDays = 0;
                    LicenseFilePath = string.Empty;
                }

                RefreshChannelSettings();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"加载激活信息失败: {ex.Message}");
            }
        }

        private void SelectKeyFile()
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择激活文件",
                Filter = "激活文件 (*.key)|*.key|所有文件 (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (dialog.ShowDialog() == true)
            {
                LicenseFilePath = dialog.FileName;
                _ = ActivateAsync();
            }
        }

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

        private void CopySN()
        {
            try
            {
                SetClipboardText(MachineSN);
                _dialogService.ShowInfo("机器序列号已复制到剪贴板");
            }
            catch
            {
                _dialogService.ShowError("复制失败，请手动复制机器序列号");
            }
        }

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
                catch (COMException ex) when (ex.HResult == unchecked((int)0x800401D0))
                {
                    if (i == maxRetries - 1)
                    {
                        var ownerInfo = GetClipboardOwnerInfo();
                        throw new InvalidOperationException($"剪切板被占用: {ownerInfo}", ex);
                    }
                    Thread.Sleep(baseDelayMs * (i + 1));
                }
            }
        }

        private string GetClipboardOwnerInfo()
        {
            try
            {
                IntPtr hwnd = GetOpenClipboardWindow();
                if (hwnd == IntPtr.Zero)
                    return "未知进程";

                GetWindowThreadProcessId(hwnd, out uint processId);
                if (processId == 0)
                    return $"窗口句柄: {hwnd}";

                try
                {
                    var process = Process.GetProcessById((int)processId);
                    return $"{process.ProcessName} (PID: {processId})";
                }
                catch
                {
                    return $"进程ID: {processId}";
                }
            }
            catch
            {
                return "未知";
            }
        }

        #endregion

        #region 通知配置

        private async Task CheckConsentAndEnableNotificationAsync()
        {
            try
            {
                var tcs = new TaskCompletionSource<bool>();

                _dialogService.ShowDialog("NotificationConsentDialog", new DialogParameters(), async result =>
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        var consent = new NotificationConsentDbo
                        {
                            ConsentTime = DateTime.Now,
                            IsAgreed = true,
                            MachineId = Environment.MachineName,
                            Remarks = "用户同意启用消息通知服务"
                        };

                        if (await _paymentRepository.SaveNotificationConsentAsync(consent))
                        {
                            SetProperty(ref _isNotificationEnabled, true, nameof(IsNotificationEnabled));
                            await SaveNotificationConfigAsync();
                            _dialogService.ShowSuccess("消息通知已启用");
                            tcs.TrySetResult(true);
                        }
                        else
                        {
                            _dialogService.ShowError("保存知情同意记录失败");
                            RaisePropertyChanged(nameof(IsNotificationEnabled));
                            tcs.TrySetResult(false);
                        }
                    }
                    else
                    {
                        _dialogService.ShowInfo("您已拒绝知情同意书，无法启用消息通知");
                        RaisePropertyChanged(nameof(IsNotificationEnabled));
                        tcs.TrySetResult(false);
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

        private async Task SaveNotificationConfigAsync()
        {
            try
            {
                var config = await _paymentRepository.GetNotificationConfigAsync() ?? new NotificationConfigDbo();
                config.IsEnabled = IsNotificationEnabled;
                config.NotificationUrl = NotificationUrl;
                await _paymentRepository.SaveNotificationConfigAsync(config);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"保存通知配置失败: {ex.Message}");
            }
        }

        private async Task LoadNotificationConfigAsync()
        {
            try
            {
                var config = await _paymentRepository.GetNotificationConfigAsync();
                if (config != null)
                {
                    NotificationUrl = config.NotificationUrl;
                    SetProperty(ref _isNotificationEnabled, config.IsEnabled && IsActivated, nameof(IsNotificationEnabled));
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"加载通知配置失败: {ex.Message}");
            }
        }

        #endregion

        #region 导航/退出

        private void OnGoBack()
        {
            if (!IsActivated)
                return;
            NavigationToRoot<MainPageV2ViewModel>(PaymentRegionNames.PaymentContentRegion);
        }

        private void OnExit()
        {
            _dialogService.ShowConfirm("确定要退出收银台系统吗？", "确认退出", result =>
            {
                if (result.Result == ButtonResult.OK)
                    Application.Current.Shutdown();
            });
        }

        #endregion

        protected override void OnDestroy()
        {
            _alipayPlugin.StatusChanged -= OnChannelStatusChanged;
            _weChatPlugin.StatusChanged -= OnChannelStatusChanged;
            _pluginRuntimeStatusService.StatusChanged -= _pluginRuntimeStatusService_StatusChanged;
        }

        #region 版本/文档

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
            catch { }
            return "V2.0.0";
        }

        private void ShowDisclaimer()
        {
            var parameters = new DialogParameters
            {
                { "Title", "免责条款与知情同意书" },
                { "FilePath", "免责条款与知情同意书.md" },
                { "IsReadOnlyMode", true }
            };
            _dialogService.ShowDialog("NotificationConsentDialog", parameters, _ => { });
        }

        private void ShowNotificationConsent()
        {
            var parameters = new DialogParameters
            {
                { "Title", "支付消息通知服务知情同意书" },
                { "FilePath", "支付消息通知服务知情同意书.md" },
                { "IsReadOnlyMode", true }
            };
            _dialogService.ShowDialog("NotificationConsentDialog", parameters, _ => { });
        }

        #endregion
    }
}
