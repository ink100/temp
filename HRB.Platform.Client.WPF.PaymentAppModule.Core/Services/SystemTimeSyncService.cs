using HRB.Platform.Client.Core.ExtensionFunctions;
using HRB.Platform.Client.WPF.PaymentAppModule.Core;
using System.Diagnostics;
using ThreadingTimer = System.Threading.Timer;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// Windows 系统时间同步服务。
    /// 说明：w32tm 同步是否成功取决于 Windows Time 服务、网络和当前账号权限。
    /// </summary>
    public static class SystemTimeSyncService
    {
        private static readonly object Locker = new();
        private static ThreadingTimer? _dailyTimer;
        private static PaymentAppContext? _appContext;
        private static bool _isSyncing;

        public static void Start(PaymentAppContext appContext)
        {
            lock (Locker)
            {
                _appContext = appContext;
                ScheduleNextLocked();
            }
        }

        public static void RefreshSchedule()
        {
            lock (Locker)
            {
                ScheduleNextLocked();
            }
        }

        private static void ScheduleNextLocked()
        {
            _dailyTimer?.Dispose();
            _dailyTimer = null;

            if (_appContext?.CurrentSettings?.IsAutoSyncSystemTimeEnabled != true)
                return;

            var now = DateTime.Now;
            var nextRun = now.Date.AddHours(1);
            if (now >= nextRun)
                nextRun = nextRun.AddDays(1);

            var dueTime = nextRun - now;
            _dailyTimer = new ThreadingTimer(async _ =>
            {
                await AutoSyncAsync();
                RefreshSchedule();
            }, null, dueTime, Timeout.InfiniteTimeSpan);
        }

        private static async Task AutoSyncAsync()
        {
            PaymentAppContext? context;
            lock (Locker)
            {
                context = _appContext;
            }

            if (context?.CurrentSettings?.IsAutoSyncSystemTimeEnabled != true)
                return;

            var result = await SyncNowAsync();
            var settings = context.CurrentSettings;
            settings.LastSystemTimeSyncTime = DateTime.Now;
            settings.LastSystemTimeSyncResult = result.Message;
            settings.LastUpdateDateTime = DateTime.Now;
            context.SaveCurrentSettings(settings);
        }

        public static async Task<SystemTimeSyncResult> SyncNowAsync()
        {
            lock (Locker)
            {
                if (_isSyncing)
                    return new SystemTimeSyncResult(false, "正在同步系统时间，请稍后再试");

                _isSyncing = true;
            }

            try
            {
                // 先尝试启动 Windows Time 服务；服务已启动时该命令会返回提示，不影响后续同步。
                await RunCommandAsync("net", "start w32time", 15000);

                var result = await RunCommandAsync("w32tm", "/resync /force", 30000);
                var output = string.Join(" ", new[] { result.Output, result.Error }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

                if (result.ExitCode == 0)
                    return new SystemTimeSyncResult(true, $"同步成功：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                if (string.IsNullOrWhiteSpace(output))
                    output = $"退出码：{result.ExitCode}";

                return new SystemTimeSyncResult(false, $"同步失败：{output}");
            }
            catch (Exception ex)
            {
                return new SystemTimeSyncResult(false, $"同步失败：{ex.Message}");
            }
            finally
            {
                lock (Locker)
                {
                    _isSyncing = false;
                }
            }
        }

        private static async Task<CommandResult> RunCommandAsync(string fileName, string arguments, int timeoutMilliseconds)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            var exitedTask = process.WaitForExitAsync();
            var completedTask = await Task.WhenAny(exitedTask, Task.Delay(timeoutMilliseconds));

            if (completedTask != exitedTask)
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(true);
                }
                catch
                {
                    // ignore
                }

                return new CommandResult(-1, string.Empty, "命令执行超时");
            }

            return new CommandResult(process.ExitCode, await outputTask, await errorTask);
        }

        private sealed record CommandResult(int ExitCode, string Output, string Error);
    }

    public sealed record SystemTimeSyncResult(bool Success, string Message);
}
