using HRB.Payment.Core.Helpers;
using Lanymy.Common.ExtensionFunctions;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 微信服务实现类。
    /// 优化点：释放 Process 句柄、启动前校验目录和压缩包、避免重复枚举进程导致句柄泄露。
    /// </summary>
    public class WeChatService : IWeChatService
    {
        private const string WECHAT_PROCESS_NAME = "WeChat";
        private const string WECHAT_VERSION = "3.9.12.54";

        /// <summary>
        /// 检查并准备指定版本的微信运行目录。
        /// </summary>
        public Task<bool> CheckWeChatVersionAsync()
        {
            try
            {
                EnvironmentSettings.VX_START_FILE_FULL_PATH = Path.Combine(
                    EnvironmentSettings.VX_ROOT_DIRECTORY_FULL_PATH,
                    EnvironmentSettings.VX_START_FILE_FULL_NAME);

                var sourceVxFileVersion = PcHelper.GetFileVersion(EnvironmentSettings.VX_ROOT_DIRECTORY_EXE_FILE_FULL_PATH);
                if (!sourceVxFileVersion.IfIsNullOrEmpty() && sourceVxFileVersion == EnvironmentSettings.VX_VERSION)
                {
                    return Task.FromResult(true);
                }

                EnvironmentSettings.VX_START_FILE_FULL_PATH = Path.Combine(
                    EnvironmentSettings.VX_SHADOW_ROOT_DIRECTORY_FULL_PATH,
                    EnvironmentSettings.VX_START_FILE_FULL_NAME);

                var shadowVxFileVersion = PcHelper.GetFileVersion(EnvironmentSettings.VX_SHADOW_ROOT_DIRECTORY_EXE_FILE_FULL_PATH);
                if (!shadowVxFileVersion.IfIsNullOrEmpty() && shadowVxFileVersion == EnvironmentSettings.VX_VERSION)
                {
                    return Task.FromResult(true);
                }

                if (!File.Exists(EnvironmentSettings.VX_ZIP_FILE_FULL_PATH))
                {
                    GlobalSettings.CurrentAppContext.CurrentLogger.Error($"微信备份压缩包不存在: {EnvironmentSettings.VX_ZIP_FILE_FULL_PATH}");
                    return Task.FromResult(false);
                }

                if (Directory.Exists(EnvironmentSettings.VX_SHADOW_ROOT_DIRECTORY_FULL_PATH))
                {
                    Directory.Delete(EnvironmentSettings.VX_SHADOW_ROOT_DIRECTORY_FULL_PATH, true);
                }

                Directory.CreateDirectory(EnvironmentSettings.VX_SHADOW_ROOT_DIRECTORY_FULL_PATH);
                ZipFile.ExtractToDirectory(
                    EnvironmentSettings.VX_ZIP_FILE_FULL_PATH,
                    EnvironmentSettings.VX_SHADOW_ROOT_DIRECTORY_FULL_PATH,
                    overwriteFiles: true);

                shadowVxFileVersion = PcHelper.GetFileVersion(EnvironmentSettings.VX_SHADOW_ROOT_DIRECTORY_EXE_FILE_FULL_PATH);
                var isReady = !shadowVxFileVersion.IfIsNullOrEmpty() && shadowVxFileVersion == EnvironmentSettings.VX_VERSION;
                if (!isReady)
                {
                    GlobalSettings.CurrentAppContext.CurrentLogger.Error($"微信影子目录版本不匹配: 当前={shadowVxFileVersion}, 需要={EnvironmentSettings.VX_VERSION}");
                }

                return Task.FromResult(isReady);
            }
            catch (Exception ex)
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Error($"检查微信版本失败: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// 检测微信版本是否符合要求
        /// </summary>
        public async Task<bool> CheckWeChatVersionAsync(string requiredVersion = WECHAT_VERSION)
        {
            if (string.IsNullOrWhiteSpace(requiredVersion))
            {
                return false;
            }

            var currentVersion = await GetCurrentWeChatVersionAsync();
            return !string.IsNullOrEmpty(currentVersion)
                   && string.Equals(currentVersion, requiredVersion, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 检测微信登录状态
        /// </summary>
        public async Task<bool> CheckWeChatLoginStatusAsync()
        {
            var processInfo = await GetWeChatProcessInfoAsync();
            return processInfo?.IsLoggedIn ?? false;
        }

        /// <summary>
        /// 判断微信进程是否存在
        /// </summary>
        public Task<bool> IsWeChatProcessRunningAsync()
        {
            try
            {
                var processes = Process.GetProcessesByName(WECHAT_PROCESS_NAME);
                var isRunning = processes.Length > 0;
                foreach (var process in processes)
                {
                    process.Dispose();
                }

                return Task.FromResult(isRunning);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// 获取微信进程信息，如果存在多个进程，优先返回最早启动的主进程。
        /// </summary>
        public Task<WeChatProcessInfo?> GetWeChatProcessInfoAsync()
        {
            return Task.Run(() =>
            {
                Process? mainProcess = null;
                var processes = Array.Empty<Process>();
                try
                {
                    processes = Process.GetProcessesByName(WECHAT_PROCESS_NAME);
                    mainProcess = processes.OrderBy(p => SafeGetStartTime(p)).FirstOrDefault();
                    return mainProcess == null ? null : BuildProcessInfo(mainProcess);
                }
                catch
                {
                    return null;
                }
                finally
                {
                    foreach (var process in processes)
                    {
                        process.Dispose();
                    }
                }
            });
        }

        /// <summary>
        /// 获取当前微信版本号
        /// </summary>
        public Task<string?> GetCurrentWeChatVersionAsync()
        {
            return Task.Run(() =>
            {
                var processes = Array.Empty<Process>();
                try
                {
                    processes = Process.GetProcessesByName(WECHAT_PROCESS_NAME);
                    var mainProcess = processes.OrderBy(p => SafeGetStartTime(p)).FirstOrDefault();
                    return mainProcess?.MainModule?.FileVersionInfo.FileVersion;
                }
                catch
                {
                    return null;
                }
                finally
                {
                    foreach (var process in processes)
                    {
                        process.Dispose();
                    }
                }
            });
        }

        /// <summary>
        /// 获取所有微信进程信息
        /// </summary>
        public Task<List<WeChatProcessInfo>> GetAllWeChatProcessesAsync()
        {
            return Task.Run(() =>
            {
                var result = new List<WeChatProcessInfo>();
                var processes = Array.Empty<Process>();
                try
                {
                    processes = Process.GetProcessesByName(WECHAT_PROCESS_NAME);
                    foreach (var process in processes)
                    {
                        try
                        {
                            result.Add(BuildProcessInfo(process));
                        }
                        catch
                        {
                            // 跳过无法读取的进程
                        }
                    }
                }
                catch
                {
                    // 进程获取失败
                }
                finally
                {
                    foreach (var process in processes)
                    {
                        process.Dispose();
                    }
                }

                return result;
            });
        }

        /// <summary>
        /// 检查微信是否安装/运行
        /// </summary>
        public async Task<bool> IsWeChatInstalledAsync()
        {
            var version = await GetCurrentWeChatVersionAsync();
            return !string.IsNullOrEmpty(version);
        }

        /// <summary>
        /// 强制终止所有微信进程
        /// </summary>
        public Task<bool> KillAllWeChatProcessesAsync()
        {
            return Task.Run(() =>
            {
                var processes = Array.Empty<Process>();
                try
                {
                    processes = Process.GetProcessesByName(WECHAT_PROCESS_NAME);
                    foreach (var process in processes)
                    {
                        try
                        {
                            if (!process.HasExited)
                            {
                                process.Kill(entireProcessTree: true);
                                process.WaitForExit(3000);
                            }
                        }
                        catch
                        {
                            // 忽略单个进程终止失败
                        }
                    }

                    var remainingProcesses = Process.GetProcessesByName(WECHAT_PROCESS_NAME);
                    var hasRemaining = remainingProcesses.Length > 0;
                    foreach (var process in remainingProcesses)
                    {
                        process.Dispose();
                    }

                    return !hasRemaining;
                }
                catch
                {
                    return false;
                }
                finally
                {
                    foreach (var process in processes)
                    {
                        process.Dispose();
                    }
                }
            });
        }

        /// <summary>
        /// 启动微信
        /// </summary>
        public async Task<bool> StartWeChatAsync()
        {
            var versionReady = await CheckWeChatVersionAsync();
            if (!versionReady)
            {
                return false;
            }

            if (!File.Exists(EnvironmentSettings.VX_START_FILE_FULL_PATH))
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Error($"微信启动文件不存在: {EnvironmentSettings.VX_START_FILE_FULL_PATH}");
                return false;
            }

            if (await IsWeChatProcessRunningAsync())
            {
                return true;
            }

            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = EnvironmentSettings.VX_START_FILE_FULL_PATH,
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(EnvironmentSettings.VX_START_FILE_FULL_PATH)
                };

                using var process = Process.Start(processStartInfo);
                return process != null;
            }
            catch (Exception ex)
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Error($"启动微信失败: {ex.Message}");
                return false;
            }
        }

        private static DateTime SafeGetStartTime(Process process)
        {
            try
            {
                return process.StartTime;
            }
            catch
            {
                return DateTime.MaxValue;
            }
        }

        private WeChatProcessInfo BuildProcessInfo(Process process)
        {
            var processInfo = new WeChatProcessInfo
            {
                ProcessId = process.Id,
                ProcessName = process.ProcessName,
                MainWindowHandle = process.MainWindowHandle,
                WindowTitle = SafeGetWindowTitle(process),
                IsLoggedIn = DetectWeChatLoginStatus(process)
            };

            try
            {
                var executablePath = process.MainModule?.FileName;
                if (!string.IsNullOrWhiteSpace(executablePath) && File.Exists(executablePath))
                {
                    processInfo.ExecutablePath = executablePath;
                    processInfo.FileVersion = FileVersionInfo.GetVersionInfo(executablePath).FileVersion;
                }
            }
            catch
            {
                // 获取模块信息失败，继续其他检测
            }

            return processInfo;
        }

        private static string SafeGetWindowTitle(Process process)
        {
            try
            {
                return process.MainWindowTitle;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 检测微信登录状态（通过窗口类名判断）。
        /// WeChatLoginWndForPC: 未登录；WeChatMainWndForPC: 已登录。
        /// </summary>
        private bool DetectWeChatLoginStatus(Process process)
        {
            try
            {
                var isLoggedIn = false;

                bool EnumWindowCallback(IntPtr hWnd, IntPtr lParam)
                {
                    GetWindowThreadProcessId(hWnd, out var pid);
                    if (pid != process.Id)
                    {
                        return true;
                    }

                    var classNameBuilder = new StringBuilder(256);
                    var result = GetClassName(hWnd, classNameBuilder, classNameBuilder.Capacity);
                    if (result <= 0)
                    {
                        return true;
                    }

                    var className = classNameBuilder.ToString();
                    if (className.Contains("WeChatLoginWndForPC"))
                    {
                        isLoggedIn = false;
                        return false;
                    }

                    if (className.Contains("WeChatMainWndForPC"))
                    {
                        isLoggedIn = true;
                        return false;
                    }

                    return true;
                }

                EnumWindows(EnumWindowCallback, IntPtr.Zero);
                return isLoggedIn;
            }
            catch
            {
                return false;
            }
        }

        #region Win32 API 导入

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        #endregion
    }
}
