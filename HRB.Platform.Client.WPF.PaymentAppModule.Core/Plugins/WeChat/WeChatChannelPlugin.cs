using HRB.Payment.Core.Models;
using HRB.Payment.Message.Core.BusEvents;
using HRB.Platform.Client.Core.Interfaces;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Abstractions;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Services;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Plugins.WeChat
{
    /// <summary>
    /// 微信渠道插件。
    /// 封装所有微信插件的业务状态和事件处理，主程序通过 IPaymentChannelPlugin 接口与之交互。
    ///
    /// 事件交互（通过消息中心）：
    ///   发送 GetVXStatusRequestEvent     — 查询 VXModule 插件工作状态
    ///   发送 StartVXModuleEvent(pid)     — 命令 VXModule 注入指定微信进程
    ///   接收 GetVXStatusAnswerEvent      — VXModule 回报工作状态 (VXStatusEto.IsWork)
    ///
    /// 内部委托 WeChatMonitor 执行 2 秒轮询循环（进程检测/登录检测/注入）。
    /// 不启动/杀任何进程 — 进程管理完全由看门狗负责。
    /// </summary>
    public sealed class WeChatChannelPlugin : IPaymentChannelPlugin
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IPluginRuntimeStatusService _statusService;
        private readonly IWeChatService _weChatService;
        private readonly PaymentAppContext _appContext;
        //  private readonly IHrbLogger _log;

        private readonly INotificationService _log;
        private readonly IDialogService _dialogService;
        private readonly IPluginProcessService _pluginProcessService;


        private WeChatMonitor? _monitor;
        private ChannelStatus _status = ChannelStatus.Unavailable;

        public string ChannelId => "WeChat";
        public string DisplayName => "微信";
        public PaymentChannel Channel => PaymentChannel.WeChat;

        /// <summary>
        /// VXModule.Shell 进程是否就绪（来自 IPluginRuntimeStatusService，只读）
        /// </summary>
        public bool IsAvailable => _statusService.IsWeChatPluginRunning;

        /// <summary>
        /// 业务是否已启用（监控循环运行中且插件已确认工作）
        /// </summary>
        public bool IsEnabled => _monitor?.PluginIsWorking ?? false;

        public event EventHandler<ChannelStatusChangedEventArgs>? StatusChanged;

        public WeChatChannelPlugin(
            IEventAggregator eventAggregator,
            IPluginRuntimeStatusService statusService,
            IWeChatService weChatService,
            PaymentAppContext appContext,
            INotificationService log,
            IDialogService dialogService, IPluginProcessService pluginProcessService)
        {
            _eventAggregator = eventAggregator;
            _statusService = statusService;
            _weChatService = weChatService;
            _appContext = appContext;
            _log = log;
            _dialogService = dialogService;
            _pluginProcessService = pluginProcessService;

            // _log = GlobalSettings.CurrentAppContext.CurrentLogger;
        }

        /// <summary>
        /// 初始化插件：订阅事件，监听进程状态变化。
        /// 如果设置中微信已启用，自动启动监控循环。
        /// </summary>
        public Task InitializeAsync(CancellationToken ct = default)
        {
            _eventAggregator.GetEvent<GetVXStatusAnswerEvent>().Subscribe(OnVXStatusAnswer, ThreadOption.BackgroundThread);
            _statusService.StatusChanged += OnRuntimeStatusChanged;

            if (_appContext.CurrentSettings.IsWeChatEnabled)
            {
                EnsureMonitorRunning();
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 启用微信渠道：启动监控循环
        /// </summary>
        public Task EnableAsync()
        {
            _log.ShowInfo("[WeChatPlugin] 启用");
            EnsureMonitorRunning();
            UpdateStatus(ChannelStatus.Enabling, "正在检测微信状态");
            return Task.CompletedTask;
        }

        /// <summary>
        /// 禁用微信渠道：停止监控循环
        /// </summary>
        public Task DisableAsync()
        {
            _log.ShowInfo("[WeChatPlugin] 禁用");
            _monitor?.Stop();
            _monitor?.ResetPluginState();
            UpdateStatus(ChannelStatus.Disabled);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 主程序退出时调用，停止监控。不杀进程。
        /// </summary>
        public Task ShutdownAsync()
        {
            _monitor?.Stop();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _eventAggregator.GetEvent<GetVXStatusAnswerEvent>().Unsubscribe(OnVXStatusAnswer);
            _monitor?.Stop();
            _statusService.StatusChanged -= OnRuntimeStatusChanged;
        }

        #region 事件处理

        /// <summary>
        /// VXModule 插件回报工作状态
        /// </summary>
        private void OnVXStatusAnswer(VXStatusEto eto)
        {
            if (_monitor == null)
                return;

            if (eto.IsWork)
            {
                _monitor.PluginIsWorking = true;
                _log.ShowSuccess("[WeChatPlugin] 插件已确认工作");
                UpdateStatus(ChannelStatus.Running);
            }
            // IsWork=false 时不做特殊处理，WeChatMonitor 下一轮会重试注入
        }

        /// <summary>
        /// 插件进程状态变化回调（由 IPluginRuntimeStatusService 触发）
        /// </summary>
        private void OnRuntimeStatusChanged(object? sender, EventArgs e)
        {
            if (!_statusService.IsWeChatPluginRunning)
            {
                // VXModule.Shell 进程消失，重置状态
                _monitor?.ResetPluginState();

                if (_status == ChannelStatus.Running || _status == ChannelStatus.Enabling)
                {
                    UpdateStatus(ChannelStatus.Unavailable);
                }
            }
        }

        /// <summary>
        /// WeChatMonitor 状态回调
        /// </summary>
        private void OnMonitorStateChanged(WeChatMonitorState state)
        {
            switch (state)
            {
                case WeChatMonitorState.WeChatNotRunning:
                    if (_status != ChannelStatus.Disabled)
                        UpdateStatus(ChannelStatus.Unavailable, "微信未运行");
                    break;

                case WeChatMonitorState.WaitingForLogin:
                    if (_status != ChannelStatus.Disabled)
                        UpdateStatus(ChannelStatus.Enabling);
                    break;

                case WeChatMonitorState.WaitingForPlugin:
                    if (_status != ChannelStatus.Disabled)
                        UpdateStatus(ChannelStatus.Enabling, "等待插件进程启动");
                    break;

                case WeChatMonitorState.Injecting:
                    if (_status != ChannelStatus.Disabled)
                        UpdateStatus(ChannelStatus.Enabling, "正在注入微信进程");
                    break;
            }
        }

        #endregion

        #region 内部方法

        private void EnsureMonitorRunning()
        {
            if (_monitor == null)
            {
                _monitor = new WeChatMonitor(_eventAggregator, _statusService, _weChatService, _dialogService, _pluginProcessService);
                _monitor.StateChanged = OnMonitorStateChanged;
            }

            _monitor.Start();
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
