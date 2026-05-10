using HRB.Payment.Message.Core.BusEvents;
using HRB.Platform.Client.Core.Interfaces;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Configuration;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// Runtime status aggregator for watchdog-managed plugin processes.
    /// - Polls process existence (Running)
    /// - Tracks last reply timestamps from bus events
    /// </summary>
    public sealed class PluginRuntimeStatusService : IPluginRuntimeStatusService, IDisposable
    {
        private readonly IPluginProcessService _pluginProcessService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IHrbLogger _log;

        private readonly CancellationTokenSource _cts = new();
        //private readonly Task _monitorTask;

        private readonly object _lock = new();

        private bool _isMessageCenterRunning;
        private DateTime? _messageCenterLastSeenAt;

        private bool _isAlipayPluginRunning;
        private bool _hasAlipayAppStarted;
        private DateTime? _alipayLastReplyAt;

        private bool _isWeChatPluginRunning;
        private DateTime? _weChatLastReplyAt;

        private TaskCompletionSource<bool> _messageCenterRunningTcs =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// 状态变更事件
        /// </summary>
        public event EventHandler? StatusChanged;

        /// <summary>
        /// 消息中心是否运行
        /// </summary>
        public bool IsMessageCenterRunning { get { lock (_lock) return _isMessageCenterRunning; } }


        /// <summary>
        /// 消息中心最后在线时间
        /// </summary>
        public DateTime? MessageCenterLastSeenAt { get { lock (_lock) return _messageCenterLastSeenAt; } }

        /// <summary>
        /// 支付宝插件是否运行
        /// </summary>
        public bool IsAlipayPluginRunning { get { lock (_lock) return _isAlipayPluginRunning; } }


        /// <summary>
        /// 是否已收到支付宝 App 启动事件（即支付宝 App 已启动并建立通讯）
        /// </summary>
        public bool HasAlipayAppStarted { get { lock (_lock) return _hasAlipayAppStarted; } }

        /// <summary>
        /// 支付宝插件最后回复时间
        /// </summary>
        public DateTime? AlipayLastReplyAt { get { lock (_lock) return _alipayLastReplyAt; } }


        /// <summary>
        /// 微信插件是否运行
        /// </summary>
        public bool IsWeChatPluginRunning { get { lock (_lock) return _isWeChatPluginRunning; } }

        /// <summary>
        /// 微信插件最后回复时间
        /// </summary>
        public DateTime? WeChatLastReplyAt { get { lock (_lock) return _weChatLastReplyAt; } }

        public PluginRuntimeStatusService(
            IPluginProcessService pluginProcessService,
            IEventAggregator eventAggregator
            )
        {
            _pluginProcessService = pluginProcessService;
            _eventAggregator = eventAggregator;
            _log = GlobalSettings.CurrentAppContext.CurrentLogger;

            // Track reply timestamps from plugin events
            _eventAggregator.GetEvent<APModuleToUIEvent>().Subscribe(OnAlipayEvent);
            _eventAggregator.GetEvent<GetVXStatusAnswerEvent>().Subscribe(OnWeChatStatusAnswer, ThreadOption.BackgroundThread);

            Task.Run(MonitorLoopAsync);
        }

        /// <summary>
        /// 等待消息中心启动
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task WaitForMessageCenterRunningAsync(CancellationToken ct = default)
        {
            Task waitTask;
            lock (_lock)
            {
                if (_isMessageCenterRunning)
                {
                    return;
                }

                waitTask = _messageCenterRunningTcs.Task;
            }

            if (ct == default)
            {
                await waitTask;
                return;
            }

            // Avoid relying on Task.WaitAsync (framework version differences)
            var cancelTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var reg = ct.Register(state => ((TaskCompletionSource<bool>)state!).TrySetResult(true), cancelTcs);

            await Task.WhenAny(waitTask, cancelTcs.Task);
            ct.ThrowIfCancellationRequested();
            await waitTask;
        }


        /// <summary>
        /// 循环检查进程状态，更新内部状态并触发事件
        /// </summary>
        /// <returns></returns>
        private async Task MonitorLoopAsync()
        {
            var token = _cts.Token;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    var messageCenterRunning = _pluginProcessService.IsPluginRunning(PluginSettings.MessageServiceExe);
                    var alipayRunning = _pluginProcessService.IsPluginRunning(PluginSettings.AlipayShellExe);
                    var weChatRunning = _pluginProcessService.IsPluginRunning(PluginSettings.WeChatShellExe);

                    bool changed = false;
                    DateTime now = DateTime.Now;

                    lock (_lock)
                    {
                        if (_isMessageCenterRunning != messageCenterRunning)
                        {
                            _isMessageCenterRunning = messageCenterRunning;
                            changed = true;

                            if (messageCenterRunning)
                            {
                                _messageCenterLastSeenAt = now;

                                _messageCenterRunningTcs.TrySetResult(true);
                            }
                            else
                            {

                                _messageCenterRunningTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
                            }
                        }
                        else if (messageCenterRunning)
                        {

                            _messageCenterLastSeenAt = now;
                        }

                        if (_isAlipayPluginRunning != alipayRunning)
                        {
                            _isAlipayPluginRunning = alipayRunning;
                            changed = true;

                            if (!alipayRunning)
                            {
                                _hasAlipayAppStarted = false;
                            }
                        }

                        if (_isWeChatPluginRunning != weChatRunning)
                        {
                            _isWeChatPluginRunning = weChatRunning;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        StatusChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
                catch (Exception ex)
                {
                    _log.Info($"[PluginRuntimeStatusService] MonitorLoop error: {ex.Message}");
                }

                try
                {
                    await Task.Delay(1000, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private void OnAlipayEvent(string eventName)
        {

            var now = DateTime.Now;
            bool changed = false;

            lock (_lock)
            {
                // Any event from Alipay plugin indicates it can reply.
                _alipayLastReplyAt = now;
                changed = true;

                if (string.Equals(eventName, "AppStarted", StringComparison.OrdinalIgnoreCase))
                {
                    _hasAlipayAppStarted = true;
                }
            }

            if (changed)
            {
                StatusChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnWeChatStatusAnswer(VXStatusEto eto)
        {
            lock (_lock)
            {
                _weChatLastReplyAt = DateTime.Now;
            }

            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            try
            {
                _cts.Cancel();
            }
            catch { }
        }
    }
}
