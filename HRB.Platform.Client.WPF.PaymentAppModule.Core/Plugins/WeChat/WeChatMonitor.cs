using HRB.Payment.Message.Client.BusEvents;
using HRB.Payment.Message.Core.BusEvents;
using HRB.Platform.Client.Core.Interfaces;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Configuration;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Extensions;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Services;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
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
        private int _autoLoginRetryCount;
        private int _autoLoginCooldown = -1;
        private int _autoLoginTransitionWaitCycles;

        // 自动隐藏
        private const int AutoHideDelayCycles = 3;        // 首次检测到登录后再等2个完整周期(≈4s)，等待主窗口渲染完成
        private bool _autoHideDone;
        private int _autoHideDelayCycleCount;

        // 二维码语音提醒
        private int _qrVoiceCycleCount = -1;
        private const int MonitorIntervalSeconds = 2;

        // 心跳检测：插件报告"工作中"后，仍定期查询 VXModule.Shell 是否有应答
        private const int HeartbeatIntervalCycles = 30;   // 每30周期(≈60s)发一次心跳
        private int _heartbeatCycleCount;
        internal volatile bool HeartbeatPending;           // 心跳等待中；OnVXStatusAnswer 应答后清零

        // 微信模块自恢复：仅在 VXModule.Shell 心跳无响应 / IsWork=false 后恢复。
        // 不能按"多久没有收款消息"恢复，因为店铺无订单是正常业务状态。
        private const int ModuleRecoveryCooldownCycles = 90;   // 恢复动作后至少≈3分钟内不重复恢复
        private int _moduleRecoveryCooldownCycles;
        private volatile bool _isRecoveringWeChatModule;

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
            _autoLoginCooldown = -1;
            _autoLoginTransitionWaitCycles = 0;
            _autoHideDone = false;
            _autoHideDelayCycleCount = 0;
            _heartbeatCycleCount = 0;
            _qrVoiceCycleCount = -1;
            _moduleRecoveryCooldownCycles = 0;
            _isRecoveringWeChatModule = false;
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
                    await Task.Delay(TimeSpan.FromSeconds(MonitorIntervalSeconds), token);
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
                        await EnsureAndPlayLocalVoiceAsync("请重新登录微信", "relogin_reminder.mp3");
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
                _autoLoginCooldown = -1;
                _autoLoginTransitionWaitCycles = 0;
                _autoHideDone = false;
                _autoHideDelayCycleCount = 0;
                _heartbeatCycleCount = 0;
                _qrVoiceCycleCount = -1;
                _moduleRecoveryCooldownCycles = 0;
                HeartbeatPending = false;
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

                    try { await EnsureAndPlayLocalVoiceAsync("请重新登录微信", "relogin_reminder.mp3"); } catch { }

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
                var configuredAutoLoginIntervalSeconds = settings.WeChatAutoLoginRetryIntervalSeconds <= 0
                    ? 6
                    : settings.WeChatAutoLoginRetryIntervalSeconds;
                var configuredMaxAutoLoginRetries = settings.WeChatAutoLoginMaxRetries <= 0
                    ? 30
                    : settings.WeChatAutoLoginMaxRetries;
                var autoLoginIntervalCycles = SecondsToMonitorCycles(
                    Math.Clamp(configuredAutoLoginIntervalSeconds, 2, 300));
                var maxAutoLoginRetries = Math.Clamp(configuredMaxAutoLoginRetries, 1, 300);
                if (settings.IsWeChatEnabled && settings.IsWeChatAutoLoginEnabled
                    && _autoLoginRetryCount < maxAutoLoginRetries)
                {
                    if (_autoLoginTransitionWaitCycles > 0)
                    {
                        _autoLoginTransitionWaitCycles--;
                    }
                    else
                    {
                        _autoLoginCooldown++;

                        if (_autoLoginCooldown >= autoLoginIntervalCycles)
                        {
                            _autoLoginCooldown = 0;
                            _autoLoginRetryCount++;

                            var clicked = await _weChatService.TryAutoLoginAsync(_weChatProcessId);
                            if (clicked)
                            {
                                // 点击动作成功不等于微信已完成登录；先等待状态转换，
                                // 超时后仍未登录才重新检测并点击，避免转换期间连续抢焦点。
                                _autoLoginTransitionWaitCycles = Math.Max(0, autoLoginIntervalCycles - 1);
                                _autoLoginCooldown = autoLoginIntervalCycles;
                                _log.Info("[WeChatMonitor] 自动登录：已点击登录按钮，等待微信登录状态确认");
                            }
                        }
                    }
                }
                else if (!settings.IsWeChatAutoLoginEnabled)
                {
                    // 再次启用时重新等待完整配置间隔，避免立刻点击。
                    _autoLoginCooldown = -1;
                    _autoLoginTransitionWaitCycles = 0;
                }

                // —— 二维码语音提醒 ——
                if (settings.IsWeChatQrLoginVoiceEnabled)
                {
                    var configuredQrVoiceIntervalSeconds = settings.WeChatQrLoginVoiceIntervalSeconds <= 0
                        ? 16
                        : settings.WeChatQrLoginVoiceIntervalSeconds;
                    var qrVoiceIntervalCycles = SecondsToMonitorCycles(
                        Math.Clamp(configuredQrVoiceIntervalSeconds, 5, 3600));
                    _qrVoiceCycleCount++;
                    if (_qrVoiceCycleCount >= qrVoiceIntervalCycles)
                    {
                        _qrVoiceCycleCount = 0;
                        var _ = EnsureAndPlayLocalVoiceAsync("请扫码登录微信", "qr_login_reminder.mp3");
                    }
                }
                else
                {
                    // 再次启用时重新等待完整配置间隔，避免立刻播报。
                    _qrVoiceCycleCount = -1;
                }

                return;
            }

            // —— 已登录 ——
            // 同一 PID 后续若重新回到未登录状态，必须从全新重试窗口开始。
            _autoLoginRetryCount = 0;
            _autoLoginCooldown = -1;
            _autoLoginTransitionWaitCycles = 0;
            _qrVoiceCycleCount = -1;

            // —— 自动隐藏逻辑 ——
            if (!_autoHideDone)
            {
                var settings = _appContext.CurrentSettings;
                if (settings.IsWeChatAutoHideEnabled)
                {
                    _autoHideDelayCycleCount++;
                    if (_autoHideDelayCycleCount < AutoHideDelayCycles)
                    {
                        _log.Info($"[WeChatMonitor] 自动隐藏：等待微信主窗口就绪({_autoHideDelayCycleCount}/{AutoHideDelayCycles})");
                        return;
                    }

                    var hidden = await _weChatService.HideWeChatWindowsAsync(_weChatProcessId);
                    if (hidden)
                    {
                        _autoHideDone = true;
                        _log.Info("[WeChatMonitor] 自动隐藏：微信主窗口已隐藏");
                    }
                    else
                    {
                        _log.Info("[WeChatMonitor] 自动隐藏：未找到微信主窗口，下一轮重试");
                    }
                }
                else
                {
                    _autoHideDone = true;
                }
            }

            // 4. 心跳检测：插件报告"工作中"后，仍需定期确认 VXModule.Shell 是否有应答。
            //    注意：不能按"多久没有收款消息"触发恢复，因为店铺长时间无订单是正常业务状态。
            if (PluginIsWorking)
            {
                if (_moduleRecoveryCooldownCycles > 0)
                    _moduleRecoveryCooldownCycles--;

                _heartbeatCycleCount++;

                if (_heartbeatCycleCount >= HeartbeatIntervalCycles)
                {
                    _heartbeatCycleCount = 0;

                    // 发查询；用 HeartbeatPending 标记，不碰 PluginIsWorking
                    // 如果插件正常应答 → OnVXStatusAnswer 在 WeChatChannelPlugin 中清零 HeartbeatPending
                    // 如果超时无应答 → 清理 VXModule.Shell 并重新注入，避免界面显示已注入但模块实际失活
                    HeartbeatPending = true;
                    _eventAggregator.GetEvent<GetVXStatusRequestEvent>().Publish();
                    await Task.Delay(2000, token);

                    if (HeartbeatPending)
                    {
                        await RecoverWeChatModuleAsync("心跳检测超时无应答，Hook 或通信可能已失活", token);
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

        /// <summary>
        /// 微信模块自恢复：只清理 VXModule.Shell.exe 并重新注入，不杀微信、不重启主程序。
        /// </summary>
        private async Task RecoverWeChatModuleAsync(string reason, CancellationToken token)
        {
            if (_isRecoveringWeChatModule || _moduleRecoveryCooldownCycles > 0)
                return;

            _isRecoveringWeChatModule = true;
            try
            {
                _log.Info($"[WeChatMonitor] 微信模块自恢复：{reason}");

                HeartbeatPending = false;
                PluginIsWorking = false;
                _heartbeatCycleCount = 0;
                _moduleRecoveryCooldownCycles = ModuleRecoveryCooldownCycles;

                // 关键：只清理微信模块进程，避免 CleanupExistingProcessesAsync 误杀 WeChat.exe / 总服务 / 支付宝模块。
                await _pluginProcessService.StopProcessAsync(PluginSettings.WeChatShellExe, "微信模块自恢复：停止 VXModule.Shell");
                await Task.Delay(1000, token);

                await _pluginProcessService.StartWeChatShellAsync();
                await Task.Delay(1000, token);

                if (_weChatProcessId > 0)
                {
                    _log.Info($"[WeChatMonitor] 微信模块自恢复：重新发送注入命令, PID={_weChatProcessId}");
                    _eventAggregator.GetEvent<StartVXModuleEvent>().Publish(_weChatProcessId);
                }
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log.Info($"[WeChatMonitor] 微信模块自恢复异常: {ex.Message}");
            }
            finally
            {
                _isRecoveringWeChatModule = false;
            }
        }

        private static int SecondsToMonitorCycles(int seconds)
        {
            return Math.Max(1, (int)Math.Ceiling(seconds / (double)MonitorIntervalSeconds));
        }

        /// <summary>
        /// 确保本地有固定语音 MP3，没有则 TTS 生成，再用 MediaPlayer 本地播放。
        /// 不阻塞监控循环。
        /// </summary>
        private async Task EnsureAndPlayLocalVoiceAsync(string text, string fileName)
        {
            try
            {
                var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds");
                Directory.CreateDirectory(dir);
                var filePath = Path.Combine(dir, fileName);

                // ① 本地文件不存在 → TTS 生成到本地
                if (!File.Exists(filePath))
                {
                    var ok = await _ttsService.SaveToFileAsync(text, filePath);
                    if (!ok) return; // 生成失败，跳过
                }

                // ② UI 线程播放本地 MP3
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var player = new MediaPlayer();
                    var tcs = new TaskCompletionSource<bool>();

                    player.MediaEnded += (_, _) =>
                    {
                        player.Close();
                        tcs.TrySetResult(true);
                    };
                    player.MediaFailed += (_, _) =>
                    {
                        player.Close();
                        tcs.TrySetResult(false);
                    };

                    player.Open(new Uri(filePath));
                    player.Play();

                    return tcs.Task;
                });
            }
            catch
            {
                // 语音播放失败不影响监控
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
