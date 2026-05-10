using HRB.Platform.Client.Core.Interfaces;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Configuration;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.DboModels;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Repository;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 插件进程管理服务实现
    /// </summary>
    public class PluginProcessService : IPluginProcessService
    {
        private readonly IPaymentRepository _repository;

        private readonly IHrbLogger _log;

        public PluginProcessService(IPaymentRepository repository)
        {
            _repository = repository;
            _log = GlobalSettings.CurrentAppContext.CurrentLogger;
        }

        #region 启动方法

        /// <summary>
        /// 启动总服务（必须最先启动）
        /// </summary>
        public async Task<bool> StartMessageServiceAsync()
        {

            ////debug
            //return true;

            return await StartProcessAsync(
                PluginSettings.MessageServiceExe,
                PluginSettings.MessageServicePath,
                "启动总服务"
            );
        }

        /// <summary>
        /// 启动支付宝通知插件
        /// </summary>
        public async Task<bool> StartAlipayShellAsync()
        {

            ////debug
            //return true;

            // 检查总服务是否已启动
            if (!IsPluginRunning(PluginSettings.MessageServiceExe))
            {
                await LogAsync(PluginSettings.AlipayShellExe, "Error", "总服务未启动，无法启动支付宝通知插件", null, false, "");
                return false;
            }

            return await StartProcessAsync(
                PluginSettings.AlipayShellExe,
                PluginSettings.AlipayShellPath,
                "启动支付宝通知插件"
            );
        }

        /// <summary>
        /// 启动微信通知插件
        /// </summary>
        public async Task<bool> StartWeChatShellAsync()
        {

            ////debug
            //return true;

            // 检查总服务是否已启动
            if (!IsPluginRunning(PluginSettings.MessageServiceExe))
            {
                await LogAsync(PluginSettings.WeChatShellExe, "Error", "总服务未启动，无法启动微信通知插件", null, false, null);
                return false;
            }


            return await StartProcessAsync(
                PluginSettings.WeChatShellExe,
                PluginSettings.WeChatShellPath,
                "启动微信通知插件"
            );
        }

        #endregion

        #region 停止方法

        /// <summary>
        /// 停止所有插件进程
        /// </summary>
        public async Task StopAllProcessesAsync()
        {
            await CleanupExistingProcessesAsync();
        }

        #endregion

        #region 清理已存在进程

        /// <summary>
        /// 清理所有已存在的插件进程（应用启动前调用）
        /// </summary>
        public async Task CleanupExistingProcessesAsync()
        {
            await LogAsync("System", "Info", "开始清理已存在的插件进程", null, true, null);

            var processNames = new[]
            {
                PluginSettings.MessageServiceExe,
                PluginSettings.WeChatShellExe,
                PluginSettings.AlipayShellExe,
                PluginSettings.WeChatExe
            };

            foreach (var processName in processNames)
            {
                try
                {
                    await StopProcessAsync(processName, $"进程已停止：{processName}");
                }
                catch (Exception ex)
                {
                    _log.Info($"[PluginProcessService] 查找进程失败: {processName}, {ex.Message}");
                }
            }

            await LogAsync("System", "Info", "插件进程清理完成", null, true, null);
        }

        #endregion

        #region 状态检查

        /// <summary>
        /// 检查插件是否正在运行
        /// </summary>
        public bool IsPluginRunning(string exeName)
        {
            var process = Process.GetProcessesByName(exeName.Replace(".exe", ""));

            return process.Any();

        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 启动进程
        /// </summary>
        public async Task<bool> StartProcessAsync(string pluginName, string exePath, string logMessage, string? fileName = null)
        {
            try
            {

                // 检查文件是否存在
                if (!File.Exists(exePath))
                {
                    var errorMsg = $"插件文件不存在: {exePath}";
                    await LogAsync(pluginName, "Error", errorMsg, null, false, null);
                    await ShowErrorAndKillAllAsync(errorMsg);
                    return false;
                }

                // 检查是否已经在运行（这里必须按 exe 名称判断，不是展示名）
                var runningCheckName = string.IsNullOrWhiteSpace(fileName) ? pluginName : fileName;
                if (IsPluginRunning(runningCheckName))
                {
                    await LogAsync(pluginName, "Info", $"{logMessage} - 插件已在运行", null, true, null);
                    return true;
                }

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(exePath),

                };

                var process = Process.Start(processStartInfo);

                return process != null;

            }
            catch (Exception ex)
            {
                var errorMsg = $"启动插件异常: {pluginName}";
                await LogAsync(pluginName, "Error", errorMsg, null, false, ex.Message);
                await ShowErrorAndKillAllAsync($"{errorMsg}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 停止进程
        /// </summary>
        public async Task<bool> StopProcessAsync(string exeName, string logMessage="")
        {
            try
            {
                var process = Process.GetProcessesByName(exeName.Replace(".exe", ""));

                if (process.Any())
                {
                    foreach (Process p in process)
                    {
                        p.Kill();
                    }
                }


                return true;
            }
            catch (Exception ex)
            {
                await LogAsync(exeName, "Error", $"{logMessage} 失败", null, false, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        private async Task LogAsync(string pluginName, string operationType, string message, int? processId, bool isSuccess, string exceptionMessage)
        {
            try
            {
                var log = new PluginLogModel
                {
                    PluginName = pluginName,
                    OperationType = operationType,
                    Message = message,
                    ProcessId = processId,
                    IsSuccess = isSuccess,
                    ExceptionMessage = exceptionMessage
                };

                await _repository.AddPluginLog(log);
            }
            catch (Exception ex)
            {
                _log.Info($"[PluginProcessService] 记录日志失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示错误并杀死所有进程
        /// </summary>
        private async Task ShowErrorAndKillAllAsync(string errorMessage)
        {
            // 杀死所有已启动的进程
            await StopAllProcessesAsync();

            // 显示错误消息
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    $"插件启动失败，请重启应用。\n\n错误信息：{errorMessage}",
                    "插件启动失败",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            });
        }

        #endregion


    }
}
