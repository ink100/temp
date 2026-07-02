using HRB.Payment.Message.Client.BusEvents;
using HRB.Payment.Message.Core.BusEvents;
using HRB.Platform.Client.Core.Interfaces;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Extensions;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Services;
using System.Diagnostics;
using System.Windows;
using Application = System.Windows.Application;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Plugins.WeChat
{
    /// <summary>
    /// 微信进程监控器 — 从 MainPageViewModel.UnifiedWeChatMonitoring 抽出。
    /// 2秒轮询循环，职责：
    ///   1. 检测 WeChat.exe 是否运行
    ///   2. 检测微信是否已登录
    ///   3. 自动登录（扫码后点"登录"按钮）+ 自动隐藏窗口
    ///   4. 检测 VXModule.Shell 是否运行
    ///   5. 条件满足时发送 StartVXModuleEvent 注入微信进程
    ///   6. 通过 GetVXStatusRequestEvent 查询插件工作状态
    ///   7. 微信意外退出时语音播报
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
        private readonly PaymentAppContext _appContext;
        private readonly ITtsService _ttsService;

        private CancellationTokenSource? _cts;
        private volatile bool _isRunning;
        private int _weChatProcessId = -1;

        // 自动登录
        private const int AutoLoginCooldownCycles = 3;    // 每3个周期(≈6s)执行一次
        private const int MaxAutoLoginRetries = 30;        // 最多重试30次(≈60s)
        private int _autoLoginRetryCount;
        private int _autoLoginCooldown;

        // 自动隐藏
        private bool _autoHideDone;

        // 二维码语音提醒
        private const int QrVoiceIntervalCycles = 8;      // 每8个周期(≈16s)提醒一次
        private int _qrVoiceCycleCount;

        // 心跳检测：插件报告"工作中"不代表消息还在流动，定期发查询验证
        private const int HeartbeatIntervalCycles = 30;   // 每30周期(≈60s)发一次心跳
        private int _heartbeatCycleCount;
        internal volatile bool HeartbeatPending;           // 心跳等待中；OnVXStatusAnswer 应答后清零

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
            IWeChatService weChatService,
            IDialogService dialogService,
            IPluginProcessService pluginProcessService,
            PaymentAppContext appContext,
            ITtsService ttsService)
        {
            _eventAggregator = eventAggregator;
            _statusService = statusService;
            _weChatService = weChatService;
            _dialogService = dialogService;
            _pluginProcessService = pluginProcessService;
            _appContext = appContext;
            _ttsService = ttsService;
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
            _autoLoginRetryCount = 0;
            _autoLoginCooldown = 0;
            _autoHideDone = false;
            _heartbeatCycleCount = 0;
            _qrVoiceCycleCount = 0;
            HeartbeatPending = false;
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
                // WeChat 进程不存在
                if (_weChatProcessId != -1)
                {
                    // 微信之前在运行，现在突然消失 → 意外退出
                    Stop();
                    ResetPluginState();

                    // ① 尝试关闭"确定"弹窗（登录过期/二维码重登对话框）
                    await _weChatService.TryDismissReLoginDialogAsync();

                    // ② 强制杀掉微信残留进程
                    await _weChatService.KillAllWeChatProcessesAsync();

                    // ③ 清理注入插件进程
                    await _pluginProcessService.CleanupExistingProcessesAsync();

                    // ④ 语音播报
                    try
                    {
                        await _ttsService.SpeakAsync("请重新登录微信");
                    }
                    catch
                    {
                        // 语音播报失败不影响后续流程
                    }

                    // ⑤ 重启 HRB 客户端
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var exePath = Environment.ProcessPath;
                        if (!string.IsNullOrEmpty(exePath))
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = exePath,
                                UseShellExecute = true
                            });
                        }
                        Application.Current.Shutdown();
                    });

                    return;
                }

                // 微信从未运行过，尝试启动
                PluginIsWorking = false;
                StateChanged?.Invoke(WeChatMonitorState.WeChatNotRunning);

                await _weChatService.StartWeChatAsync();

                return;
            }

            // 2. 检查是否新进程（PID 变化）
            if (_weChatProcessId != processInfo.ProcessId)
            {
                _weChatProcessId = processInfo.ProcessId;
                PluginIsWorking = false;
                _autoLoginRetryCount = 0;
                _autoLoginCooldown = 0;
                _autoHideDone = false;
                _heartbeatCycleCount = 0;
                _qrVoiceCycleCount = 0;
            }

            // 3. 检查微信是否已登录
            if (!processInfo.IsLoggedIn)
            {
                // ★ 先判断是否为"已退出微信"重登场景（非首次扫码）
                if (await _weChatService.IsWeChatReLoginAsync(_weChatProcessId))
                {
                    _log.Info("[WeChatMonitor] 检测到重登场景（已退出微信），走退出流程");

                    Stop();
                    ResetPluginState();

                    await _weChatService.TryDismissReLoginDialogAsync();
                    await _weChatService.KillAllWeChatProcessesAsync();
                    await _pluginProcessService.CleanupExistingProcessesAsync();

                    try { await _ttsService.SpeakAsync("请重新登录微信"); } catch { }

                    // 重启 HRB 客户端
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var exePath = Environment.ProcessPath;
                        if (!string.IsNullOrEmpty(exePath))
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = exePath,
                                UseShellExecute = true
                            });
                        }
                        Application.Current.Shutdown();
                    });

                    return;
                }

                StateChanged?.Invoke(WeChatMonitorState.WaitingForLogin);

                // —— 自动登录逻辑 ——
                var settings = _appContext.CurrentSettings;
                if (settings.IsWeChatEnabled && settings.IsWeChatAutoLoginEnabled
                    && _autoLoginRetryCount < MaxAutoLoginRetries)
                {
                    _autoLoginCooldown++;

                    if (_autoLoginCooldown >= AutoLoginCooldownCycles)
                    {
                        _autoLoginCooldown = 0;
                        _autoLoginRetryCount++;

                        var clicked = await _weChatService.TryAutoLoginAsync(_weChatProcessId);
                        if (clicked)
                        {
                            _autoLoginRetryCount = MaxAutoLoginRetries; // 成功，停止重试
                            _log.Info("[WeChatMonitor] 自动登录：点击登录按钮成功");
                        }
                    }
                }

                // —— 二维码语音提醒 ——
                _qrVoiceCycleCount++;
                if (_qrVoiceCycleCount >= QrVoiceIntervalCycles)
                {
                    _qrVoiceCycleCount = 0;
                    try
                    {
                        await _ttsService.SpeakAsync("请扫码登录微信");
                    }
                    catch
                    {
                        // 语音播报失败不影响后续流程
                    }
                }

                return;
            }

            // —— 已登录 ——

            // —— 自动隐藏逻辑 ——
            if (!_autoHideDone)
            {
                var settings = _appContext.CurrentSettings;
                if (settings.IsWeChatAutoHideEnabled)
                {
                    await _weChatService.HideWeChatWindowsAsync(_weChatProcessId);
                    _log.Info("[WeChatMonitor] 自动隐藏：微信窗口已隐藏");
                }
                _autoHideDone = true;
            }

            // 4. 心跳检测：插件报告"工作中"不代表消息还在流动
            //    每 HeartbeatIntervalCycles(≈60s) 发一次查询，确认 Hook 仍在工作
            if (PluginIsWorking)
            {
                _heartbeatCycleCount++;

                if (_heartbeatCycleCount >= HeartbeatIntervalCycles)
                {
                    _heartbeatCycleCount = 0;

                    // 发查询；用 HeartbeatPending 标记，不碰 PluginIsWorking
                    // 如果插件正常应答 → OnVXStatusAnswer 在 WeChatChannelPlugin 中清零 HeartbeatPending
                    // 如果超时无应答 → Hook 已失活，设 PluginIsWorking=false 触发下轮重注入
                    HeartbeatPending = true;
                    _eventAggregator.GetEvent<GetVXStatusRequestEvent>().Publish();
                    await Task.Delay(2000, token);

                    if (HeartbeatPending)
                    {
                        HeartbeatPending = false;
                        PluginIsWorking = false;
                        _log.Info("[WeChatMonitor] 心跳检测：插件超时无应答，Hook 可能已失活，下一轮自动重新注入");
                    }
                }

                return;
            }

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
