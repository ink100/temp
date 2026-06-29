using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Helpers
{
    /// <summary>
    /// 微信监听控制台调试输出。
    /// 由设置项 IsWeChatListenerConsoleOutputEnabled 控制，默认关闭。
    /// 只输出到控制台/VS 输出窗口，不写本地日志文件，避免客户现场长期运行产生日志文件。
    /// </summary>
    public static class WeChatListenerConsoleDebug
    {
        private const int MaxRawLength = 20000;
        private static readonly object SyncRoot = new();
        private static bool _consoleAllocatedBySelf;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        public static bool IsEnabled
        {
            get
            {
                try
                {
                    return GlobalSettings.CurrentAppContext?.CurrentSettings?.IsWeChatListenerConsoleOutputEnabled == true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public static void RefreshConsoleState()
        {
            try
            {
                if (IsEnabled)
                {
                    EnsureConsole();
                    Write("SWITCH", "微信监听控制台输出已开启");
                }
                else
                {
                    Debug.WriteLine("[WX-LISTENER][SWITCH] 微信监听控制台输出已关闭");
                    ReleaseConsoleIfOwned();
                }
            }
            catch
            {
                // 调试输出不能影响收银主流程。
            }
        }

        public static void Write(string stage, string message)
        {
            if (!IsEnabled) return;

            try
            {
                EnsureConsole();
                var line = $"[WX-LISTENER][{stage}] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}";
                Console.WriteLine(line);
                Debug.WriteLine(line);
            }
            catch
            {
                // 调试输出不能影响收银主流程。
            }
        }

        public static void WriteRaw(string stage, string title, string? raw)
        {
            if (!IsEnabled) return;
            var safeRaw = Truncate(raw);
            Write(stage, $"{title}: {safeRaw}");
        }

        /// <summary>
        /// 输出多行详细内容，适合查看 PayMessage JSON / 微信 Message XML。
        /// 注意：仍会做长度限制，避免客户现场控制台被超长内容刷爆。
        /// </summary>
        public static void WriteBlock(string stage, string title, string? raw, int maxLength = MaxRawLength)
        {
            if (!IsEnabled) return;

            try
            {
                EnsureConsole();
                var content = NormalizeBlock(raw, maxLength);
                var header = $"[WX-LISTENER][{stage}] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ===== {title} START Length={raw?.Length ?? 0} =====";
                var footer = $"[WX-LISTENER][{stage}] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ===== {title} END =====";

                Console.WriteLine(header);
                Debug.WriteLine(header);

                foreach (var line in SplitLongLines(content, 1800))
                {
                    Console.WriteLine(line);
                    Debug.WriteLine(line);
                }

                Console.WriteLine(footer);
                Debug.WriteLine(footer);
            }
            catch
            {
                // 调试输出不能影响收银主流程。
            }
        }

        public static void WriteException(string stage, Exception ex, string? raw = null)
        {
            if (!IsEnabled) return;
            var builder = new StringBuilder();
            builder.Append(ex.GetType().Name).Append(": ").Append(ex.Message);
            if (!string.IsNullOrWhiteSpace(raw))
            {
                builder.Append(" Raw=").Append(Truncate(raw));
            }
            Write(stage, builder.ToString());
        }

        public static string Truncate(string? value, int maxLength = MaxRawLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var normalized = value.Replace("\r", " ").Replace("\n", " ").Trim();
            return normalized.Length <= maxLength ? normalized : normalized[..maxLength] + $"...【已截断，原始长度={normalized.Length}】";
        }

        private static string NormalizeBlock(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var normalized = value.Replace("\0", string.Empty).Trim();
            return normalized.Length <= maxLength
                ? normalized
                : normalized[..maxLength] + $"\r\n...【已截断，原始长度={normalized.Length}，当前输出前{maxLength}字符】";
        }

        private static IEnumerable<string> SplitLongLines(string content, int chunkSize)
        {
            if (string.IsNullOrEmpty(content))
            {
                yield return string.Empty;
                yield break;
            }

            if (chunkSize <= 0)
            {
                chunkSize = 1800;
            }

            using (var reader = new StringReader(content))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length <= chunkSize)
                    {
                        yield return line;
                        continue;
                    }

                    for (var i = 0; i < line.Length; i += chunkSize)
                    {
                        var length = Math.Min(chunkSize, line.Length - i);
                        yield return line.Substring(i, length);
                    }
                }
            }
        }

        private static void EnsureConsole()
        {
            lock (SyncRoot)
            {
                if (GetConsoleWindow() != IntPtr.Zero) return;

                if (AllocConsole())
                {
                    _consoleAllocatedBySelf = true;
                    Console.OutputEncoding = Encoding.UTF8;
                }
            }
        }

        private static void ReleaseConsoleIfOwned()
        {
            lock (SyncRoot)
            {
                if (!_consoleAllocatedBySelf) return;
                if (GetConsoleWindow() == IntPtr.Zero) return;

                FreeConsole();
                _consoleAllocatedBySelf = false;
            }
        }
    }
}
