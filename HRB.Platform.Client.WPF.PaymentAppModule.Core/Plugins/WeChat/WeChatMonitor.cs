using HRB.Payment.Message.Client.BusEvents;
using HRB.Payment.Message.Core.BusEvents;
using HRB.Platform.Client.Core.Interfaces;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Extensions;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Services;
using System.Windows;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Plugins.WeChat
{
    /// <summary>
    /// 微信进程监控器 — 从 MainPageViewModel.UnifiedWeChatMonitoring 抽出。
    /// 2秒轮询循环，职责：
    ///   1. 检测 WeChat.exe 是否运行
    ///   2. 检测微信是否已登录
    ///   3. 检测 VXModule.Shell 是否运行
    ///   4. 条件满足时发送 StartVXModuleEvent 注入微信进程
    ///   5. 通过 GetVXStatusRequestEvent 查询插件工作状态
    ///
    /// 不启动/杀任何进程 — 进程管理完全由看门狗负责。
    /// </summary>
    internal sealed class WeChatMonitor
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IPluginRuntimeStatusService _statusService;
        private readonly IWeChatService _weChatService;
        private readonly IHrbLogger _log;

        private readonly IDialogService _dialogService;
        private readonly IPluginProcessService _pluginProcessService;

        private CancellationTokenSource? _cts;
        private volatile bool _isRunning;
        private int _weChatProcessId = -1;

        /// <summary>
        /// VXModule 插件是否已确认工作（由外部通过 SetPluginWorking 更新）
        /// </summary>
        internal volatile bool PluginIsWorking;

        /// <summary>
        /// 监控状态变化回调
        /// </summary>
        internal Action<WeChatMonitorState>? StateChanged;

        internal WeChatMonitor(
            IEventAggregator eventAggregator,
            IPluginRuntimeStatusService statusService,
            IWeChatService weChatService, IDialogService dialogService, IPluginProcessService pluginProcessService)
        {
            _eventAggregator = eventAggregator;
            _statusService = statusService;
            _weChatService = weChatService;
            _dialogService = dialogService;
            _pluginProcessService = pluginProcessService;
            _log = GlobalSettings.CurrentAppContext.CurrentLogger;
        }

        internal void Start()
        {
            if (_isRunning)
                return;

            _isRunning = true;
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            _ = Task.Run(() => MonitorLoopAsync(_cts.Token));
        }

        internal void Stop()
        {
            _isRunning = false;
            _cts?.Cancel();
        }

        /// <summary>
        /// 重置插件工作状态（进程消失/PID变化时调用）
        /// </summary>
        internal void ResetPluginState()
        {
            PluginIsWorking = false;
            _weChatProcessId = -1;
        }

        private async Task MonitorLoopAsync(CancellationToken token)
        {
            _log.Info("[WeChatMonitor] 监控循环启动");

            while (!token.IsCancellationRequested && _isRunning)
            {
                try
                {
                    await CheckCycleAsync(token);
                    await Task.Delay(2000, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _log.Info($"[WeChatMonitor] 监控异常: {ex.Message}");
                    try
                    { await Task.Delay(5000, token); }
                    catch (TaskCanceledException) { break; }
                }
            }

            _log.Info("[WeChatMonitor] 监控循环停止");
        }

        private async Task CheckCycleAsync(CancellationToken token)
        {
            // 1. 检查微信进程
            var processInfo = await _weChatService.GetWeChatProcessInfoAsync();

            if (processInfo == null)
            {
                if (_weChatProcessId != -1)
                {
                    Stop();
                    await _pluginProcessService.CleanupExistingProcessesAsync();
                    Application.Current.Dispatcher.Invoke(() =>
                   {
                       _dialogService.ShowError("微信进程消失，请重启软件", callback: (result) =>
                       {
                           Application.Current.Shutdown();
                       });
                   });

                    return;
                }

                PluginIsWorking = false;
                StateChanged?.Invoke(WeChatMonitorState.WeChatNotRunning);

                await _weChatService.StartWeChatAsync();

                return;
            }

            // 2. 检查是否新进程
            if (_weChatProcessId != processInfo.ProcessId)
            {
                _weChatProcessId = processInfo.ProcessId;
                PluginIsWorking = false;
            }

            // 3. 检查微信是否登录
            if (!processInfo.IsLoggedIn)
            {
                StateChanged?.Invoke(WeChatMonitorState.WaitingForLogin);
                return;
            }

            // 4. 如果插件已工作，无需操作
            if (PluginIsWorking)
                return;

            // 5. 检查 VXModule.Shell 是否运行
            if (!_statusService.IsWeChatPluginRunning)
            {
                StateChanged?.Invoke(WeChatMonitorState.WaitingForPlugin);
                return;
            }

            // 6. 查询插件状态
            StateChanged?.Invoke(WeChatMonitorState.Injecting);
            _eventAggregator.GetEvent<GetVXStatusRequestEvent>().Publish();

            // 等1秒看插件是否响应
            await Task.Delay(1000, token);

            // 7. 如果仍未工作，发送注入命令
            if (!PluginIsWorking)
            {
                _log.Info($"[WeChatMonitor] 发送注入命令, PID={_weChatProcessId}");
                _eventAggregator.GetEvent<StartVXModuleEvent>().Publish(_weChatProcessId);
            }
        }
    }

    /// <summary>
    /// 监控器观测到的状态
    /// </summary>
    internal enum WeChatMonitorState
    {
        /// <summary>WeChat.exe 未运行</summary>
        WeChatNotRunning,
        /// <summary>微信未登录，等待用户登录</summary>
        WaitingForLogin,
        /// <summary>VXModule.Shell 未运行，等待看门狗启动</summary>
        WaitingForPlugin,
        /// <summary>正在注入微信进程</summary>
        Injecting
    }
}
