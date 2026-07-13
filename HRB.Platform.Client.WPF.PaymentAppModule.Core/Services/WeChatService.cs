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
        private delegate bool EnumChildWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowEnabled(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            return IntPtr.Size == 8
                ? GetWindowLongPtr64(hWnd, nIndex)
                : new IntPtr(GetWindowLong32(hWnd, nIndex));
        }

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            return IntPtr.Size == 8
                ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
                : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        [DllImport("kernel32.dll")]
        private static extern void Sleep(uint dwMilliseconds);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP = 0x0202;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const int SW_HIDE = 0;
        private const int SW_MINIMIZE = 6;
        private const int GWL_STYLE = -16;
        private const long WS_VISIBLE = 0x10000000L;

        #endregion

        #region 自动登录 / 自动隐藏

        /// <inheritdoc />
        public Task<bool> TryAutoLoginAsync(int processId)
        {
            return Task.Run(() =>
            {
                try
                {
                    IntPtr loginWindow = IntPtr.Zero;
                    bool foundLoginWindow = false;

                    // 1. EnumWindows → 找到 WeChatLoginWndForPC 窗口
                    EnumWindows((hWnd, lParam) =>
                    {
                        GetWindowThreadProcessId(hWnd, out var pid);
                        if ((int)pid != processId) return true;

                        var classNameBuilder = new StringBuilder(256);
                        GetClassName(hWnd, classNameBuilder, classNameBuilder.Capacity);
                        if (classNameBuilder.ToString().Contains("WeChatLoginWndForPC"))
                        {
                            loginWindow = hWnd;
                            foundLoginWindow = true;
                            return false; // 停止枚举
                        }
                        return true;
                    }, IntPtr.Zero);

                    if (!foundLoginWindow || loginWindow == IntPtr.Zero)
                        return false;

                    // 2. 优先尝试标准子控件按钮（某些环境可枚举到）
                    IntPtr loginButton = IntPtr.Zero;

                    EnumChildWindows(loginWindow, (hWnd, lParam) =>
                    {
                        if (!IsWindowVisible(hWnd) || !IsWindowEnabled(hWnd))
                            return true;

                        var btnTextBuilder = new StringBuilder(256);
                        GetWindowText(hWnd, btnTextBuilder, btnTextBuilder.Capacity);
                        var btnText = btnTextBuilder.ToString();

                        if (btnText.Contains("登录") || btnText.Contains("登錄") || btnText.Contains("进入微信"))
                        {
                            loginButton = hWnd;
                            return false; // 停止枚举
                        }
                        return true;
                    }, IntPtr.Zero);

                    if (loginButton != IntPtr.Zero)
                    {
                        SetForegroundWindow(loginWindow);
                        GetWindowRect(loginButton, out var buttonRect);
                        int buttonX = (buttonRect.Left + buttonRect.Right) / 2;
                        int buttonY = (buttonRect.Top + buttonRect.Bottom) / 2;
                        IntPtr lParamClick = (IntPtr)((buttonY << 16) | (buttonX & 0xFFFF));

                        PostMessage(loginButton, WM_LBUTTONDOWN, IntPtr.Zero, lParamClick);
                        Sleep(50);
                        PostMessage(loginButton, WM_LBUTTONUP, IntPtr.Zero, lParamClick);
                        return true;
                    }

                    // 3. 微信 3.9.x 登录窗口是 DirectUI，自绘按钮通常枚举不到。
                    // 不能再用窗口标题“微信”判断二维码页，因为“进入微信”确认页标题也叫“微信”。
                    // 这里改为检测窗口下半部是否存在微信绿色大按钮：
                    // - 有绿色按钮：说明是“进入微信”页，坐标点击按钮中心；
                    // - 没有绿色按钮：说明大概率是二维码页，只让上层播报“请扫码登录微信”。
                    SetForegroundWindow(loginWindow);
                    Sleep(100);

                    if (!GetWindowRect(loginWindow, out var rect))
                        return false;

                    if (!TryFindGreenLoginButtonCenter(rect, out var x, out var y))
                        return false;

                    SetCursorPos(x, y);
                    Sleep(50);
                    mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)x, (uint)y, 0, UIntPtr.Zero);
                    Sleep(80);
                    mouse_event(MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, UIntPtr.Zero);

                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// 在登录窗口中检测“进入微信”绿色按钮，并返回按钮中心坐标。
        /// 二维码页没有这个绿色大按钮，因此不会误点二维码页。
        /// </summary>
        private static bool TryFindGreenLoginButtonCenter(RECT rect, out int centerX, out int centerY)
        {
            centerX = 0;
            centerY = 0;

            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;
            if (width <= 120 || height <= 180)
                return false;

            // “进入微信”按钮通常在窗口下半部，中间横向区域。
            var startX = rect.Left + (int)(width * 0.18);
            var endX = rect.Left + (int)(width * 0.82);
            var startY = rect.Top + (int)(height * 0.58);
            var endY = rect.Top + (int)(height * 0.82);

            var hdc = GetDC(IntPtr.Zero);
            if (hdc == IntPtr.Zero)
                return false;

            try
            {
                long sumX = 0;
                long sumY = 0;
                int greenCount = 0;

                // 低频采样，避免低配机器上过重；微信按钮绿色面积较大，步长 4 足够识别。
                for (var y = startY; y <= endY; y += 4)
                {
                    for (var x = startX; x <= endX; x += 4)
                    {
                        var color = GetPixel(hdc, x, y);
                        if (color == 0xFFFFFFFF)
                            continue;

                        var r = (int)(color & 0xFF);
                        var g = (int)((color >> 8) & 0xFF);
                        var b = (int)((color >> 16) & 0xFF);

                        // WeChat 绿色按钮常见色接近 #07C160，允许抗锯齿/主题差异。
                        if (g >= 140 && r <= 80 && b <= 120 && g - r >= 70 && g - b >= 40)
                        {
                            greenCount++;
                            sumX += x;
                            sumY += y;
                        }
                    }
                }

                // 二维码页可能有少量绿色装饰/图标，要求足够面积才认为是按钮。
                if (greenCount < 80)
                    return false;

                centerX = (int)(sumX / greenCount);
                centerY = (int)(sumY / greenCount);
                return true;
            }
            finally
            {
                ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        /// <inheritdoc />
        public Task<bool> HideWeChatWindowsAsync(int processId)
        {
            return Task.Run(() =>
            {
                try
                {
                    var hidden = false;

                    EnumWindows((hWnd, lParam) =>
                    {
                        GetWindowThreadProcessId(hWnd, out var pid);
                        if ((int)pid != processId) return true;

                        var classNameBuilder = new StringBuilder(256);
                        GetClassName(hWnd, classNameBuilder, classNameBuilder.Capacity);
                        var className = classNameBuilder.ToString();

                        // 只隐藏微信主窗口，避免误处理登录/弹窗窗口
                        if (!className.Contains("WeChatMainWndForPC"))
                            return true;

                        ShowWindow(hWnd, SW_MINIMIZE);
                        Sleep(150);
                        ShowWindow(hWnd, SW_HIDE);

                        // 移除 WS_VISIBLE，防止微信登录后短时间内把窗口重新显示出来
                        var style = GetWindowLongPtr(hWnd, GWL_STYLE);
                        var newStyle = new IntPtr(style.ToInt64() & ~WS_VISIBLE);
                        SetWindowLongPtr(hWnd, GWL_STYLE, newStyle);

                        hidden = true;
                        return true;
                    }, IntPtr.Zero);

                    return hidden;
                }
                catch
                {
                    // 隐藏窗口是尽力而为的操作
                    return false;
                }
            });
        }

        #endregion

        #region 重新登录对话框关闭

        /// <inheritdoc />
        public Task<bool> TryDismissReLoginDialogAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    // 枚举所有顶层窗口，找 WeChat 进程的可见弹窗
                    var weChatPids = new HashSet<int>();
                    var processes = Array.Empty<Process>();
                    try
                    {
                        processes = Process.GetProcessesByName(WECHAT_PROCESS_NAME);
                        foreach (var p in processes)
                            weChatPids.Add(p.Id);
                    }
                    finally
                    {
                        foreach (var p in processes)
                            p.Dispose();
                    }

                    if (weChatPids.Count == 0)
                    {
                        // 微信已退，可能会有残留弹窗；也搜一次 MessageBox 类窗口
                    }

                    bool dismissed = false;

                    EnumWindows((hWnd, lParam) =>
                    {
                        GetWindowThreadProcessId(hWnd, out var pid);

                        // 只处理 WeChat 进程 OR MessageBox(#32770) 弹窗
                        bool isWeChatWindow = weChatPids.Contains((int)pid);
                        bool isMessageBox = false;
                        var clsBuilder = new StringBuilder(256);
                        GetClassName(hWnd, clsBuilder, clsBuilder.Capacity);
                        if (clsBuilder.ToString() == "#32770")
                            isMessageBox = true;

                        if (!isWeChatWindow && !isMessageBox)
                            return true;

                        if (!IsWindowVisible(hWnd))
                            return true;

                        // 枚举子控件，找"确定"按钮
                        IntPtr confirmButton = IntPtr.Zero;

                        EnumChildWindows(hWnd, (child, _) =>
                        {
                            if (!IsWindowVisible(child) || !IsWindowEnabled(child))
                                return true;

                            var btnText = new StringBuilder(256);
                            GetWindowText(child, btnText, btnText.Capacity);
                            var text = btnText.ToString();

                            if (text.Contains("确定") || text.Contains("確定") ||
                                text.Contains("是") || text.Contains("确认"))
                            {
                                confirmButton = child;
                                return false;
                            }
                            return true;
                        }, IntPtr.Zero);

                        if (confirmButton == IntPtr.Zero)
                            return true;

                        // 模拟点击
                        SetForegroundWindow(hWnd);
                        GetWindowRect(confirmButton, out var rect);
                        int x = (rect.Left + rect.Right) / 2;
                        int y = (rect.Top + rect.Bottom) / 2;
                        IntPtr lParamClick = (IntPtr)((y << 16) | (x & 0xFFFF));

                        PostMessage(confirmButton, WM_LBUTTONDOWN, IntPtr.Zero, lParamClick);
                        Sleep(50);
                        PostMessage(confirmButton, WM_LBUTTONUP, IntPtr.Zero, lParamClick);

                        dismissed = true;
                        return false; // 找到一个就够了，停止枚举
                    }, IntPtr.Zero);

                    return dismissed;
                }
                catch
                {
                    return false;
                }
            });
        }

        #endregion

        #region 重登检测

        /// <inheritdoc />
        public Task<bool> IsWeChatReLoginAsync(int processId)
        {
            return Task.Run(() =>
            {
                try
                {
                    bool isReLogin = false;

                    EnumWindows((hWnd, lParam) =>
                    {
                        GetWindowThreadProcessId(hWnd, out var pid);
                        if ((int)pid != processId)
                            return true;

                        var clsBuilder = new StringBuilder(256);
                        GetClassName(hWnd, clsBuilder, clsBuilder.Capacity);
                        if (!clsBuilder.ToString().Contains("WeChatLoginWndForPC"))
                            return true;

                        // 检查窗口标题
                        var titleBuilder = new StringBuilder(256);
                        GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);
                        var title = titleBuilder.ToString();

                        if (ContainsReLoginKeyword(title))
                        {
                            isReLogin = true;
                            return false;
                        }

                        // 枚举子控件文字。注意：微信登录页多数是 DirectUI，自绘文字可能枚举不到。
                        EnumChildWindows(hWnd, (child, _) =>
                        {
                            var childText = new StringBuilder(256);
                            GetWindowText(child, childText, childText.Capacity);
                            if (ContainsReLoginKeyword(childText.ToString()))
                            {
                                isReLogin = true;
                                return false;
                            }
                            return true;
                        }, IntPtr.Zero);

                        if (isReLogin)
                            return false;

                        // DirectUI 自绘弹层兜底：如截图中的“你已退出微信 + 确定”灰色按钮，文字读不到，改用像素特征判断。
                        if (TryDetectReLoginOverlayByPixels(hWnd))
                        {
                            isReLogin = true;
                            return false;
                        }

                        return true;
                    }, IntPtr.Zero);

                    return isReLogin;
                }
                catch
                {
                    return false;
                }
            });
        }

        private static bool ContainsReLoginKeyword(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return text.Contains("已退出")
                || text.Contains("登录过期")
                || text.Contains("登录已过期")
                || text.Contains("重新登录")
                || text.Contains("在其他设备登录")
                || text.Contains("账号在别处");
        }

        /// <summary>
        /// DirectUI 自绘重登弹层兜底检测。
        /// 截图中的“你已退出微信”弹层文字无法通过 GetWindowText 读取，
        /// 但中部会出现一块浅灰色“确定”按钮；普通二维码页/进入微信页在该区域没有这块灰按钮。
        /// </summary>
        private static bool TryDetectReLoginOverlayByPixels(IntPtr loginWindow)
        {
            try
            {
                SetForegroundWindow(loginWindow);
                Sleep(80);

                if (!GetWindowRect(loginWindow, out var rect))
                    return false;

                var width = rect.Right - rect.Left;
                var height = rect.Bottom - rect.Top;
                if (width <= 120 || height <= 180)
                    return false;

                // “确定”灰按钮大致位于登录窗口中部偏下，横向居中。
                var startX = rect.Left + (int)(width * 0.32);
                var endX = rect.Left + (int)(width * 0.68);
                var startY = rect.Top + (int)(height * 0.50);
                var endY = rect.Top + (int)(height * 0.68);

                var hdc = GetDC(IntPtr.Zero);
                if (hdc == IntPtr.Zero)
                    return false;

                try
                {
                    var grayCount = 0;
                    var greenTextCount = 0;

                    for (var y = startY; y <= endY; y += 3)
                    {
                        for (var x = startX; x <= endX; x += 3)
                        {
                            var color = GetPixel(hdc, x, y);
                            if (color == 0xFFFFFFFF)
                                continue;

                            var r = (int)(color & 0xFF);
                            var g = (int)((color >> 8) & 0xFF);
                            var b = (int)((color >> 16) & 0xFF);

                            // 确定按钮背景为浅灰，允许抗锯齿和显示缩放差异。
                            var max = Math.Max(r, Math.Max(g, b));
                            var min = Math.Min(r, Math.Min(g, b));
                            if (r >= 215 && g >= 215 && b >= 215 && r <= 250 && g <= 250 && b <= 250 && max - min <= 18)
                            {
                                grayCount++;
                            }

                            // 按钮文字通常是微信绿色，作为辅助特征，不强制要求。
                            if (g >= 120 && r <= 120 && b <= 140 && g - r >= 35 && g - b >= 15)
                            {
                                greenTextCount++;
                            }
                        }
                    }

                    // 普通“进入微信”页在这个区域通常是白底/头像/昵称，不会有大量浅灰按钮块。
                    return grayCount >= 180 || (grayCount >= 100 && greenTextCount >= 3);
                }
                finally
                {
                    ReleaseDC(IntPtr.Zero, hdc);
                }
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
