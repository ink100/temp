using HRB.Payment.Message.Core.BusEvents;
using System.Collections.Concurrent;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 插件保活服务实现。
    /// 优化点：增加重启防抖，避免进程检测和心跳检测同时触发导致重复启动插件。
    /// </summary>
    public class PluginKeepAliveService : IPluginKeepAliveService, IDisposable
    {
        private readonly IPluginProcessService _pluginProcessService;
        private readonly ConcurrentDictionary<string, MonitoringContext> _monitoringContexts;
        private bool _disposed;

        public PluginKeepAliveService(IPluginProcessService pluginProcessService)
        {
            _pluginProcessService = pluginProcessService;
            _monitoringContexts = new ConcurrentDictionary<string, MonitoringContext>();
        }

        /// <summary>
        /// 启动保活监控（包含进程检测，可选心跳检测）
        /// </summary>
        public void StartMonitoring(string pluginName, Func<Task> onPluginDied, IEventAggregator eventAggregator = null)
        {
            if (string.IsNullOrWhiteSpace(pluginName) || onPluginDied == null)
            {
                return;
            }

            StopMonitoring(pluginName);

            var context = new MonitoringContext
            {
                PluginName = pluginName,
                OnPluginDied = onPluginDied,
                EventAggregator = eventAggregator,
                CancellationTokenSource = new CancellationTokenSource(),
                LastHeartbeatResponse = DateTime.Now,
                LastRecoveryTime = DateTime.MinValue,
                EnableHeartbeat = eventAggregator != null
            };

            _monitoringContexts[pluginName] = context;

            context.ProcessMonitorTask = Task.Run(
                () => MonitorProcessAsync(context),
                context.CancellationTokenSource.Token);

            if (context.EnableHeartbeat)
            {
                context.HeartbeatTask = Task.Run(
                    () => HeartbeatLoopAsync(context),
                    context.CancellationTokenSource.Token);
            }
        }

        /// <summary>
        /// 停止保活监控
        /// </summary>
        public void StopMonitoring(string pluginName)
        {
            if (_monitoringContexts.TryRemove(pluginName, out var context))
            {
                context.CancellationTokenSource?.Cancel();
                context.CancellationTokenSource?.Dispose();
            }
        }

        /// <summary>
        /// 停止所有监控
        /// </summary>
        public void StopAll()
        {
            foreach (var kvp in _monitoringContexts)
            {
                kvp.Value.CancellationTokenSource?.Cancel();
                kvp.Value.CancellationTokenSource?.Dispose();
            }

            _monitoringContexts.Clear();
        }

        /// <summary>
        /// 通知收到心跳响应
        /// </summary>
        public void NotifyHeartbeatResponse(string pluginName)
        {
            if (_monitoringContexts.TryGetValue(pluginName, out var context))
            {
                lock (context.HeartbeatLock)
                {
                    context.LastHeartbeatResponse = DateTime.Now;
                }
            }
        }

        private async Task MonitorProcessAsync(MonitoringContext context)
        {
            var cancellationToken = context.CancellationTokenSource.Token;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(5000, cancellationToken);

                    if (!_pluginProcessService.IsPluginRunning(context.PluginName))
                    {
                        await TriggerRecoveryAsync(context, "插件进程不存在");
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    GlobalSettings.CurrentAppContext.CurrentLogger.Error($"插件进程监控出错: {context.PluginName}, {ex.Message}");
                }
            }
        }

        private async Task HeartbeatLoopAsync(MonitoringContext context)
        {
            if (!context.EnableHeartbeat || context.EventAggregator == null)
            {
                return;
            }

            var cancellationToken = context.CancellationTokenSource.Token;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    context.EventAggregator.GetEvent<UIToAPModuleEvent>().Publish("Heartbeat");

                    DateTime lastHeartbeatResponse;
                    lock (context.HeartbeatLock)
                    {
                        lastHeartbeatResponse = context.LastHeartbeatResponse;
                    }

                    if ((DateTime.Now - lastHeartbeatResponse).TotalSeconds > 10)
                    {
                        await TriggerRecoveryAsync(context, "插件心跳超时");
                    }

                    await Task.Delay(3000, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    GlobalSettings.CurrentAppContext.CurrentLogger.Error($"心跳检测出错: {context.PluginName}, {ex.Message}");

                    try
                    {
                        await Task.Delay(3000, cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private async Task TriggerRecoveryAsync(MonitoringContext context, string reason)
        {
            if (Interlocked.Exchange(ref context.IsRecovering, 1) == 1)
            {
                return;
            }

            try
            {
                var now = DateTime.Now;
                if ((now - context.LastRecoveryTime).TotalSeconds < 8)
                {
                    return;
                }

                context.LastRecoveryTime = now;
                GlobalSettings.CurrentAppContext.CurrentLogger.Error($"触发插件恢复: {context.PluginName}, 原因: {reason}");

                await context.OnPluginDied();

                lock (context.HeartbeatLock)
                {
                    context.LastHeartbeatResponse = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Error($"插件恢复失败: {context.PluginName}, {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref context.IsRecovering, 0);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            StopAll();
            _disposed = true;
        }

        private class MonitoringContext
        {
            public string PluginName { get; set; } = string.Empty;
            public Func<Task> OnPluginDied { get; set; } = default!;
            public IEventAggregator EventAggregator { get; set; }
            public CancellationTokenSource CancellationTokenSource { get; set; } = default!;
            public Task ProcessMonitorTask { get; set; }
            public Task HeartbeatTask { get; set; }
            public DateTime LastHeartbeatResponse { get; set; }
            public DateTime LastRecoveryTime { get; set; }
            public int IsRecovering;
            public bool EnableHeartbeat { get; set; }
            public object HeartbeatLock { get; } = new();
        }
    }
}
