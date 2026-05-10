using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;

namespace HRB.Payment.Core.Services
{
    /// <summary>
    /// 开机自启动服务 - 使用 Windows 任务计划程序
    /// </summary>
    public static class AutoStartupService
    {
        private const string TaskName = "HRBPaymentClientAutoStart";

        /// <summary>
        /// 获取应用程序可执行文件路径
        /// </summary>
        private static string GetExecutablePath()
        {
            return Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
        }

        /// <summary>
        /// 检查是否以管理员权限运行
        /// </summary>
        private static bool IsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 启用开机自启动
        /// </summary>
        public static bool Enable()
        {
            try
            {
                var exePath = GetExecutablePath();
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                {
                    return false;
                }

                // 获取当前用户名
                var userName = Environment.UserName;

                // 构建 schtasks 命令
                // /SC ONLOGON: 用户登录时触发
                // /RL HIGHEST: 以最高权限运行
                var arguments = $"/Create /TN \"{TaskName}\" /TR \"\\\"{exePath}\\\"\" /SC ONLOGON /RL HIGHEST /F";

                var startInfo = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 禁用开机自启动
        /// </summary>
        public static bool Disable()
        {
            try
            {
                var arguments = $"/Delete /TN \"{TaskName}\" /F";

                var startInfo = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                    // 退出码 0 表示成功，1 表示任务不存在（也算成功）
                    return process.ExitCode == 0 || process.ExitCode == 1;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查是否已启用开机自启动
        /// </summary>
        public static bool IsEnabled()
        {
            try
            {
                var arguments = $"/Query /TN \"{TaskName}\"";

                var startInfo = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 设置开机自启动状态
        /// </summary>
        public static bool SetEnabled(bool enabled)
        {
            return enabled ? Enable() : Disable();
        }
    }
}
