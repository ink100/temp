using HRB.Payment.Core.Models;
using HRB.Payment.Message.Core.BusEvents;
using HRB.Platform.Client.Core.Interfaces;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Abstractions;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Services;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Plugins.Alipay
{
    /// <summary>
    /// 支付宝渠道插件。
    /// 封装所有支付宝插件的业务命令和事件处理，主程序通过 IPaymentChannelPlugin 接口与之交互。
    ///
    /// 命令（主程序 -> 插件进程，通过 UIToAPModuleEvent）：
    ///   "AppStarted"        — 握手：通知插件主程序已就绪
    ///   "StartAlipayPolling" — 开始收款轮询
    ///   "StopAlipayPolling"  — 停止收款轮询
    ///   "Enable"             — 启用（触发插件端配置/授权流程）
    ///   "Disable"            — 禁用
    ///
    /// 事件（插件进程 -> 主程序，通过 APModuleToUIEvent）：
    ///   "AppStarted"      — 握手响应：插件已就绪
    ///   "PollingStarted"  — 轮询已启动
    ///   "PollingStoped"   — 轮询已停止（原始拼写）
    ///   "Success"         — 启用/配置成功
    ///   "GetInfoSuccess"  — 获取配置成功
    ///   "GetInfo"         — 正在获取配置
    ///   "GetInfoFail"     — 获取配置失败
    ///   "Disable"         — 已禁用
    ///   "CanceledLogin"   — 用户取消登录
    ///   "NeedConfig"      — 需要先配置支付宝
    /// </summary>
    public sealed class AlipayChannelPlugin : IPaymentChannelPlugin
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IPluginRuntimeStatusService _statusService;
        private readonly PaymentAppContext _appContext;
     //   private readonly IHrbLogger _log;
        private readonly INotificationService _log;

        private volatile bool _isEnabled;
        private volatile bool _isPolling;
        private volatile bool _appStarted;
        private ChannelStatus _status = ChannelStatus.Unavailable;

        public string ChannelId => "Alipay";
        public string DisplayName => "支付宝";
        public PaymentChannel Channel => PaymentChannel.Alipay;

        /// <summary>
        /// 插件进程是否就绪（来自 IPluginRuntimeStatusService，只读）
        /// </summary>
        public bool IsAvailable => _statusService.IsAlipayPluginRunning;

        /// <summary>
        /// 业务是否已启用（由插件事件驱动）
        /// </summary>
        public bool IsEnabled => _isEnabled;

        /// <summary>
        /// 轮询是否正在运行
        /// </summary>
        public bool IsPolling => _isPolling;

        public event EventHandler<ChannelStatusChangedEventArgs>? StatusChanged;

        public AlipayChannelPlugin(
            IEventAggregator eventAggregator,
            IPluginRuntimeStatusService statusService,
            PaymentAppContext appContext,
            INotificationService notificationService
            )
        {
            _eventAggregator = eventAggregator;
            _statusService = statusService;
            _appContext = appContext;
            _log = notificationService;
            //  _log = GlobalSettings.CurrentAppContext.CurrentLogger;
        }

        /// <summary>
        /// 初始化插件：订阅事件总线，监听插件进程状态变化。
        /// 如果插件进程已就绪，立即发送握手命令。
        /// </summary>
        public Task InitializeAsync(CancellationToken ct = default)
        {
            _eventAggregator.GetEvent<APModuleToUIEvent>().Subscribe(OnPluginEvent);
            _statusService.StatusChanged += OnRuntimeStatusChanged;

            if (_statusService.IsAlipayPluginRunning)
            {
                SendAppStartWithRetry();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 启用支付宝（发送 Enable 命令，触发插件端配置/授权流程）
        /// </summary>
        public Task EnableAsync()
        {
            if (!IsAvailable)
            {
                _log.ShowInfo("[AlipayPlugin] 插件进程未就绪，无法启用");
                return Task.CompletedTask;
            }

            _eventAggregator.GetEvent<UIToAPModuleEvent>().Publish("Enable");
            UpdateStatus(ChannelStatus.Enabling, "正在配置支付宝插件");
            return Task.CompletedTask;
        }

        /// <summary>
        /// 禁用支付宝（发送停止轮询 + 禁用命令）
        /// </summary>
        public Task DisableAsync()
        {
            _eventAggregator.GetEvent<UIToAPModuleEvent>().Publish("StopAlipayPolling");
            _eventAggregator.GetEvent<UIToAPModuleEvent>().Publish("Disable");
            return Task.CompletedTask;
        }

        /// <summary>
        /// 主程序退出时调用，停止轮询。不杀进程。
        /// </summary>
        public Task ShutdownAsync()
        {
            if (_isPolling)
            {
                _eventAggregator.GetEvent<UIToAPModuleEvent>().Publish("StopAlipayPolling");
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _statusService.StatusChanged -= OnRuntimeStatusChanged;
        }

        #region 事件处理

        /// <summary>
        /// 处理支付宝插件发来的事件
        /// </summary>
        private void OnPluginEvent(string eventName)
        {
            switch (eventName)
            {
                case "AppStarted":
                    _appStarted = true;
                    _log.ShowSuccess("[AlipayPlugin] 插件已就绪");
                    // 如果上次是启用状态，自动恢复轮询
                    if (_appContext.CurrentSettings.IsAlipayEnabled)
                    {
                        _eventAggregator.GetEvent<UIToAPModuleEvent>().Publish("StartAlipayPolling");
                    }
                    UpdateStatus(ChannelStatus.Available);
                    break;

                case "Success":
                case "GetInfoSuccess":
                    _isEnabled = true;
                    _log.ShowSuccess("[AlipayPlugin] 启用成功，开始轮询");
                    _eventAggregator.GetEvent<UIToAPModuleEvent>().Publish("StartAlipayPolling");
                    UpdateStatus(ChannelStatus.Running);
                    break;

                case "GetInfo":
                    UpdateStatus(ChannelStatus.Enabling, "正在配置支付宝插件");
                    break;

                case "PollingStarted":
                    _isEnabled = true;
                    _isPolling = true;
                    UpdateStatus(ChannelStatus.Running);
                    break;

                case "PollingStoped":
                case "Disable":
                    _isEnabled = false;
                    _isPolling = false;
                    UpdateStatus(ChannelStatus.Disabled);
                    break;

                case "GetInfoFail":
                    _isEnabled = false;
                    UpdateStatus(ChannelStatus.Faulted, "支付宝配置失败");
                    break;

                case "CanceledLogin":
                    _isEnabled = false;
                    UpdateStatus(ChannelStatus.Faulted, "取消登录");
                    break;

                case "NeedConfig":
                    _isEnabled = false;
                    _isPolling = false;
                    UpdateStatus(ChannelStatus.Faulted, "需要先配置支付宝");
                    break;
            }
        }

        /// <summary>
        /// 插件进程状态变化回调（由 IPluginRuntimeStatusService 触发）
        /// </summary>
        private void OnRuntimeStatusChanged(object? sender, EventArgs e)
        {
            if (_statusService.IsAlipayPluginRunning)
            {
                // 进程出现，如果尚未握手则发送握手
                if (!_appStarted)
                {
                    SendAppStartWithRetry();
                }
            }
            else
            {
                // 进程消失，重置所有状态
                _appStarted = false;
                _isEnabled = false;
                _isPolling = false;
                UpdateStatus(ChannelStatus.Unavailable);
            }
        }

        #endregion

        #region 内部方法

        /// <summary>
        /// 发送握手命令（AppStarted），每 3 秒重试直到插件响应或进程消失
        /// </summary>
        private async void SendAppStartWithRetry()
        {
            _log.ShowInfo("[AlipayPlugin] 开始握手...");

            while (!_appStarted && _statusService.IsAlipayPluginRunning)
            {
                _eventAggregator.GetEvent<UIToAPModuleEvent>().Publish("AppStarted");
                await Task.Delay(3000);
            }
        }

        private void UpdateStatus(ChannelStatus status, string? message = null)
        {
            _status = status;
            StatusChanged?.Invoke(this, new ChannelStatusChangedEventArgs
            {
                ChannelId = ChannelId,
                Status = status,
                Message = message
            });
        }

        #endregion
    }
}
