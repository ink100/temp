using HRB.Payment.Message.Core.BusEvents;
using HRB.Platform.Client.Core.Interfaces;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Configuration;
using System.Diagnostics;
using System.Windows;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 支付渠道协调器实现
    /// 负责微信和支付宝的启动、停止和状态管理
    /// </summary>
    public class PaymentChannelCoordinator : IPaymentChannelCoordinator
    {
        private readonly PaymentAppContext _appContext;
        private readonly IPluginProcessService _pluginProcessService;
        private readonly IWeChatService _weChatService;
        private readonly IEventAggregator _eventAggregator;

        private volatile bool _alipayIsRunning = false;
        private volatile bool _appStarted = false;

        private readonly IHrbLogger _log;

        public PaymentChannelCoordinator(
            PaymentAppContext appContext,
            IPluginProcessService pluginProcessService,
            IWeChatService weChatService,
            IEventAggregator eventAggregator)
        {
            _appContext = appContext ?? throw new ArgumentNullException(nameof(appContext));
            _pluginProcessService = pluginProcessService;
            _weChatService = weChatService;
            _eventAggregator = eventAggregator;

            _log = GlobalSettings.CurrentAppContext.CurrentLogger;

            // 订阅支付宝模块事件
          //  _eventAggregator.GetEvent<APModuleToUIEvent>().Subscribe(OnAlipayModuleEvent);
        }

        private bool IsNewPluginLifecycleModeEnabled =>
            PluginLifecycleModeHelper.IsNewModeEnabled(_appContext.CurrentSettings);

        /// <summary>
        /// 启动微信客户端/插件（根据新旧模式不同处理）
        /// - 旧模式：主程序负责启动微信插件壳 + WeChat.exe
        /// - 新模式：插件壳进程由看门狗管理；主程序仅负责 WeChat.exe，并等待插件壳存在后再进入业务链路
        /// </summary>
        public async Task StartWeChatAsync(Action? onLoginPollingStart = null)
        {
            try
            {
                #region 新模式

                if (IsNewPluginLifecycleModeEnabled)
                {
                    // 判断微信是否运行，未运行则启动微信（新模式下插件壳由看门狗管理，主程序不直接启动插件壳）
                    var weChatRunning = await _weChatService.IsWeChatProcessRunningAsync();
                    if (!weChatRunning)
                    {
                        // CheckWeChatVersionAsync 此方法会自动处理版本不匹配的情况
                        await _weChatService.CheckWeChatVersionAsync();

                        await _pluginProcessService.StartProcessAsync("WeChat.exe", EnvironmentSettings.VX_START_FILE_FULL_PATH, "启动微信客户端", EnvironmentSettings.VX_PROCESS_NAME);
                    }

                    return;
                }


                #endregion



                #region 旧模式


                // Legacy mode
                // 检查微信插件是否已运行
                var isRunning = _pluginProcessService.IsPluginRunning(PluginSettings.WeChatShellExe);
                if (isRunning)
                {
                    // 已运行，直接启动登录轮询
                    onLoginPollingStart?.Invoke();
                    return;
                }

                // 启动微信 Shell
                await _pluginProcessService.StartWeChatShellAsync();

                // 杀掉旧的微信进程
                var vxProcess = Process.GetProcessesByName(EnvironmentSettings.VX_PROCESS_NAME).FirstOrDefault();
                vxProcess?.Kill();

                // 检查微信版本
                var isCorrect = await _weChatService.CheckWeChatVersionAsync();
                if (!isCorrect)
                {
                    MessageBox.Show($"微信版本号不匹配，请下载 {EnvironmentSettings.VX_VERSION} 版本", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 启动微信进程
                await _pluginProcessService.StartProcessAsync("微信", EnvironmentSettings.VX_START_FILE_FULL_PATH, string.Empty, EnvironmentSettings.VX_PROCESS_NAME);

                // 启动登录轮询
                onLoginPollingStart?.Invoke();

                GlobalSettings.CurrentAppContext.CurrentLogger.Info("微信支付渠道已启动");
                #endregion
            }
            catch (Exception ex)
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Error($"启动微信支付渠道失败: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// 启动支付宝插件（仅启动插件进程，等待插件就绪）
        /// </summary>
        public async Task StartAlipayShellAsync()
        {
            try
            {
                if (IsNewPluginLifecycleModeEnabled)
                {
                    // New mode: Alipay shell process is managed by watchdog.
                    _log.Info("New plugin lifecycle mode enabled, skip starting Alipay shell process from app.");
                    return;
                }

                if (_appStarted)
                    return;

                if (!_pluginProcessService.IsPluginRunning(PluginSettings.AlipayShellExe))
                {
                    if (await _pluginProcessService.StartAlipayShellAsync())
                    {

                        await SendAlipayAppStart();
                        _log.Info("支付宝插件已就绪");
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error($"启动支付宝插件失败: {ex.Message}");
                throw;
            }
        }

        //private volatile bool _startingAlipay = false;



        /// <summary>
        /// 给支付宝插件发送开始事件，直到插件响应已启动（AppStarted）为止
        /// </summary>
        /// <returns></returns>
        public async Task SendAlipayAppStart()
        {
            while (!GlobalSettings.IsAlipayShellStarted)
            {
               
                if (_pluginProcessService.IsPluginRunning(PluginSettings.AlipayShellExe))
                {
                    _eventAggregator.GetEvent<UIToAPModuleEvent>().Publish("AppStarted");
                }
                else
                {
                    break;
                }
                await Task.Delay(3000);
            }
        }


        /// <summary>
        /// 启动支付宝业务轮询（发送 StartAlipayPolling 命令）
        /// </summary>
        public async Task StartAlipayPollingAsync()
        {
            try
            {
                while (!GlobalSettings.IsAlipayPoolingStarted)
                {
                    if (_pluginProcessService.IsPluginRunning(PluginSettings.AlipayShellExe))
                    {
                        _eventAggregator.GetEvent<UIToAPModuleEvent>().Publish("StartAlipayPolling");
                    }
                    else
                    {
                        break;
                    }
                    await Task.Delay(3000);
                }

                _log.Info("支付宝业务轮询已启动");
            }
            catch (Exception ex)
            {
                _log.Error($"启动支付宝业务轮询失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 停止支付宝支付渠道
        /// </summary>
        public async Task StopAlipayAsync()
        {
            try
            {
                if (IsNewPluginLifecycleModeEnabled)
                {
                    _alipayIsRunning = false;
                    _appStarted = false;

                    //停止轮询
                    _eventAggregator.GetEvent<UIToAPModuleEvent>().Publish("StopAlipayPolling");


                    _log.Info("New plugin lifecycle mode enabled, skip stopping Alipay shell process from app.");
                    return;
                }

                if (_pluginProcessService.IsPluginRunning(PluginSettings.AlipayShellExe))
                {
                    await _pluginProcessService.StopProcessAsync(PluginSettings.AlipayShellExe);
                    _alipayIsRunning = false;
                    _appStarted = false;
                    _log.Info("支付宝支付渠道已停止");
                }
            }
            catch (Exception ex)
            {
                _log.Info($"停止支付宝支付渠道失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 处理支付宝模块事件
        /// </summary>
        private void OnAlipayModuleEvent(string eventName)
        {
            if (eventName == "AppStarted")
            {
                _appStarted = true;
                _log.Info("支付宝插件已启动");
            }
            else if (eventName == "PollingStarted")
            {
                _alipayIsRunning = true;
                _log.Info("支付宝轮询已启动");
            }
            else if (eventName == "PollingStoped")
            {
                _alipayIsRunning = false;
                _log.Info("支付宝轮询已停止");
            }
        }
    }
}
