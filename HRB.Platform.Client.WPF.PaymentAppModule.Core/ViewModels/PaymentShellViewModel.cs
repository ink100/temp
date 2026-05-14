using HRB.Payment.Core.Helpers;
using HRB.Platform.Client.WPF.Core.Services.IServices;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Configuration;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.DboModels;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Extensions;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Plugins.Alipay;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Plugins.WeChat;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Repository;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Services;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.V2;
using Lanymy.Common.Helpers;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Reflection;
using System.Windows;
using Application = System.Windows.Application;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.ViewModels
{
    /// <summary>
    /// 支付应用Shell视图模型
    /// 负责应用启动时的激活验证和页面导航
    /// </summary>
    public class PaymentShellViewModel : BasePaymentRegionViewModel
    {
        private readonly ILicenseService _licenseService;
        private readonly IPluginRuntimeStatusService _pluginRuntimeStatusService;
        private readonly IWeChatConnectionService _weChatConnectionService;
        private readonly IDialogService _dialogService;
        private readonly IPaymentRepository _paymentRepository;
        private readonly AlipayChannelPlugin _alipayPlugin;
        private readonly WeChatChannelPlugin _weChatPlugin;
        private readonly INotificationService _log;

        private volatile bool _isMessageCenterConnected;

        public PaymentShellViewModel(
            PaymentAppContext appContext,
            IEventAggregator eventAggregator,
            IRegionManager regionManager,
            IWpfDeviceRequestService deviceRequestService,
            ILicenseService licenseService,
            IPluginRuntimeStatusService pluginRuntimeStatusService,
            IWeChatConnectionService weChatConnectionService,
            IDialogService dialogService,
            IPaymentRepository paymentRepository,
            AlipayChannelPlugin alipayPlugin,
            WeChatChannelPlugin weChatPlugin, INotificationService log) : base(appContext, eventAggregator, regionManager, deviceRequestService)
        {
            _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
            _pluginRuntimeStatusService = pluginRuntimeStatusService;
            _weChatConnectionService = weChatConnectionService;
            _dialogService = dialogService;
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
            _alipayPlugin = alipayPlugin;
            _weChatPlugin = weChatPlugin;
            _log = log;
        }

        protected override void OnLoadedEvent()
        {
            base.OnLoadedEvent();
            _ = CheckUserAgreementAndLicenseAsync();
        }

        /// <summary>
        /// 检查用户协议和激活状态
        /// </summary>
        private async Task CheckUserAgreementAndLicenseAsync()
        {
            try
            {
                // 获取当前协议版本号
                string currentVersion = EnvironmentSettings.USER_AGREEMENT_VERSION;

                // 检查用户是否已同意当前版本的条款
                var hasAgreedCurrentVersion = await _paymentRepository.HasUserAgreedVersionAsync(currentVersion);

                if (!hasAgreedCurrentVersion)
                {
                    // 获取用户之前同意的版本号（用于提示）
                    var previousVersion = await _paymentRepository.GetLatestAgreedVersionAsync();

                    // 显示用户协议对话框
                    await ShowUserAgreementDialogAsync(currentVersion, previousVersion);
                }
                else
                {
                    // 已同意当前版本条款,继续检查激活状态
                    await CheckLicenseAndNavigateAsync();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(
                    $"初始化失败: {ex.Message}",
                    "错误",
                    _ => Application.Current.Shutdown());
            }
        }

        /// <summary>
        /// 显示用户协议对话框
        /// </summary>
        /// <param name="currentVersion">当前版本号</param>
        /// <param name="previousVersion">之前同意的版本号</param>
        private async Task ShowUserAgreementDialogAsync(string currentVersion, string? previousVersion)
        {
            var tcs = new TaskCompletionSource<bool>();

            // 如果有之前的版本，说明是更新，需要特殊提示
            var dialogParams = new DialogParameters();
            if (!string.IsNullOrEmpty(previousVersion))
            {
                dialogParams.Add("Title", $"用户协议已更新 ({previousVersion} → {currentVersion})");
            }

            _dialogService.ShowDialog(
                "UserAgreementDialog",
                dialogParams,
                async result =>
                {
                    if (result.Result == ButtonResult.OK)
                    {
                        // 用户同意,保存记录
                        var agreement = new UserAgreementDbo
                        {
                            FirstUseTime = DateTime.Now,
                            AgreementTime = DateTime.Now,
                            IsAgreed = true,
                            Version = currentVersion,
                            MachineId = GetMachineId(),
                            Remarks = string.IsNullOrEmpty(previousVersion)
                                ? "用户首次使用同意条款"
                                : $"用户同意条款更新 (从 {previousVersion} 更新到 {currentVersion})"
                        };

                        var saved = await _paymentRepository.SaveUserAgreementAsync(agreement);

                        if (saved)
                        {
                            // 保存成功,继续检查激活状态
                            await CheckLicenseAndNavigateAsync();
                            tcs.SetResult(true);
                        }
                        else
                        {
                            _dialogService.ShowError(
                                "保存用户协议记录失败,请重试。",
                                "错误",
                                _ => Application.Current.Shutdown());
                            tcs.SetResult(false);
                        }
                    }
                    else
                    {
                        // 用户拒绝,退出应用
                        _dialogService.ShowInfo(
                            "您已拒绝用户协议,应用将退出。",
                            "提示",
                            _ => Application.Current.Shutdown());
                        tcs.SetResult(false);
                    }
                });

            await tcs.Task;
        }

        /// <summary>
        /// 获取机器唯一标识
        /// </summary>
        private string GetMachineId()
        {
            try
            {
                // 使用CPU序列号和主板序列号组合作为机器ID
                string cpuId = string.Empty;
                string boardId = string.Empty;

                try
                {
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        cpuId = obj["ProcessorId"]?.ToString() ?? string.Empty;
                        break;
                    }
                }
                catch { }

                try
                {
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        boardId = obj["SerialNumber"]?.ToString() ?? string.Empty;
                        break;
                    }
                }
                catch { }

                // 如果都获取失败,使用机器名称
                if (string.IsNullOrEmpty(cpuId) && string.IsNullOrEmpty(boardId))
                {
                    return Environment.MachineName;
                }

                return $"{cpuId}_{boardId}";
            }
            catch
            {
                return Environment.MachineName;
            }
        }

        /// <summary>
        /// 检查激活状态并导航 — 2.0 统一路径，无新旧模式分支
        /// </summary>
        private async Task CheckLicenseAndNavigateAsync()
        {
            try
            {
                var isValid = await _licenseService.ValidateLicenseAsync();

                if (isValid)
                {

                    var watchProcess = Process.GetProcessesByName("HBR.Payment.WatchDog");

                    if (!watchProcess.Any())
                    {
                        // 获取应用程序根目录
                        var rootPath = PathHelper.GetCallDomainPath();

                        // 插件根目录在应用根目录的上一层
                        var pluginRootPath = PathHelper.CombineRelativePath(rootPath, "../");

                        // 总服务（与应用根目录同级）
                        var watchDogPath = Path.Combine(pluginRootPath, "HBR.Payment.WatchDog", "HBR.Payment.WatchDog.exe");

                        var currentExe = Path.Combine(AppContext.BaseDirectory, "HRB.Payment.Client.App.exe");

                        if (Path.Exists(watchDogPath))
                        {
                            var processStartInfo = new ProcessStartInfo
                            {
                                FileName = watchDogPath,
                                UseShellExecute = false,
                                WorkingDirectory = Path.GetDirectoryName(watchDogPath),
                                ArgumentList =
                                {
                                    PluginSettings.MessageServicePath,
                                    PluginSettings.WeChatShellPath,
                                    PluginSettings.AlipayShellPath,
                                    currentExe

                                }
                            };
                            Process.Start(processStartInfo);
                        }

                    }


                    // 导航到 V2 主页（立即显示 UI）
                    NavigateRegion<MainPageV2ViewModel>(PaymentRegionNames.PaymentContentRegion);

                    // 后台：等待消息中心就绪 → 连接（不阻塞 UI）
                    _ = ConnectMessageCenterAsync();




                }
                else
                {
                    // 未激活，显示激活提示并导航到设置页
                    _dialogService.ShowConfirm(
                        "软件尚未激活，请输入注册码进行激活。\n\n点击\"确定\"前往设置页面激活。",
                        "软件激活提示",
                        result =>
                        {
                            if (result.Result == ButtonResult.OK)
                            {
                                NavigateRegion<SettingsPageV2ViewModel>(PaymentRegionNames.PaymentContentRegion);
                            }
                            else
                            {
                                Application.Current.Shutdown();
                            }
                        });
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(
                    $"激活验证失败: {ex.Message}\n\n将导航到设置页面。",
                    "错误",
                    _ =>
                    {
                        NavigateRegion<SettingsPageV2ViewModel>(PaymentRegionNames.PaymentContentRegion);
                    });
            }
        }

        /// <summary>
        /// 后台连接消息中心并初始化插件
        /// 顺序：等待消息中心就绪 → 建立连接 → 初始化插件（订阅事件 + 启动监控/握手）
        /// 即使连接失败，插件仍需初始化（本地进程检测仍可工作）
        /// </summary>
        private async Task ConnectMessageCenterAsync()
        {
            try
            {
                var licenseInfo = await _licenseService.GetLicenseInfoAsync();
                if (licenseInfo != null && licenseInfo.MaxDateTime > PcHelper.GetNetNowTime())
                {
                    await _pluginRuntimeStatusService.WaitForMessageCenterRunningAsync();
                    await _weChatConnectionService.StartAsync();
                    _isMessageCenterConnected = true;
                }
            }
            catch (Exception ex)
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Error(
                    $"[PaymentShellViewModel] 连接消息中心失败: {ex.Message}");
            }

            // 无论连接是否成功，都初始化插件
            try
            {
                await _alipayPlugin.InitializeAsync();
                await _weChatPlugin.InitializeAsync();
            }
            catch (Exception ex)
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Error(
                    $"[PaymentShellViewModel] 初始化插件失败: {ex.Message}");
            }

            // 监听消息中心状态变化，掉线后自动重连
            _pluginRuntimeStatusService.StatusChanged += OnRuntimeStatusChanged;
        }

        /// <summary>
        /// 消息中心状态变化：掉线 → 标记断开；重新上线 → 自动重连
        /// </summary>
        private async void OnRuntimeStatusChanged(object? sender, EventArgs e)
        {
            var isRunning = _pluginRuntimeStatusService.IsMessageCenterRunning;

            if (!isRunning)
            {
                _isMessageCenterConnected = false;
                return;
            }

            // 消息中心上线且当前未连接 → 重连
            if (isRunning && !_isMessageCenterConnected)
            {
                _isMessageCenterConnected = true;
                try
                {
                    await _weChatConnectionService.StartAsync();
                    _log.ShowInfo("[PaymentShellViewModel] 消息中心重新上线，已重新连接");
                    GlobalSettings.CurrentAppContext.CurrentLogger.Info(
                        "[PaymentShellViewModel] 消息中心重新上线，已重新连接");
                }
                catch (Exception ex)
                {
                    _isMessageCenterConnected = false;
                    _log.ShowError($"[PaymentShellViewModel] 消息中心重连失败: {ex.Message}");
                    GlobalSettings.CurrentAppContext.CurrentLogger.Error(
                        $"[PaymentShellViewModel] 消息中心重连失败: {ex.Message}");
                }
            }
        }
    }
}
