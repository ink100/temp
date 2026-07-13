using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace HRB.Payment.Client.App
{
    /// <summary>
    /// 启动后定期清理旧日志。
    /// 规则：每隔 15 天执行一次清理，删除 15 天前的日志文件，并记录每次清理时间和结果。
    /// </summary>
    internal sealed class LogCleanupService : IDisposable
    {
        private const int CleanupIntervalDays = 15;
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(12);
        private static readonly string[] LogDirectoryNames = { "Logs", "logs", "Log", "log" };
        private static readonly string[] LogFileExtensions = { ".log", ".txt", ".json", ".csv", ".zip", ".gz" };

        private readonly string _baseDirectory;
        private readonly string _controlDirectory;
        private readonly string _lastCleanupFilePath;
        private readonly string _cleanupRecordFilePath;
        private readonly object _syncRoot = new();
        private Timer? _timer;
        private bool _isRunning;

        public LogCleanupService(string baseDirectory)
        {
            _baseDirectory = string.IsNullOrWhiteSpace(baseDirectory)
                ? AppDomain.CurrentDomain.BaseDirectory
                : baseDirectory;

            _controlDirectory = Path.Combine(_baseDirectory, "Logs");
            _lastCleanupFilePath = Path.Combine(_controlDirectory, ".last-log-cleanup.txt");
            _cleanupRecordFilePath = Path.Combine(_controlDirectory, "log-cleanup-record.txt");
        }

        /// <summary>
        /// 启动定时检查。首次启动立即检查一次，后续每 12 小时检查一次；
        /// 只有距离上次清理满 15 天才真正删除旧日志。
        /// </summary>
        public void Start()
        {
            Directory.CreateDirectory(_controlDirectory);
            _timer = new Timer(_ => RunCleanupIfNeeded(), null, TimeSpan.Zero, CheckInterval);
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _timer = null;
        }

        private void RunCleanupIfNeeded()
        {
            lock (_syncRoot)
            {
                if (_isRunning)
                    return;

                _isRunning = true;
            }

            try
            {
                var now = DateTime.Now;
                var lastCleanupTime = ReadLastCleanupTime();
                if (lastCleanupTime.HasValue && now - lastCleanupTime.Value < TimeSpan.FromDays(CleanupIntervalDays))
                    return;

                CleanupOldLogs(now);
                File.WriteAllText(_lastCleanupFilePath, now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch (Exception ex)
            {
                SafeAppendRecord($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 日志清理异常：{ex}\r\n");
            }
            finally
            {
                lock (_syncRoot)
                {
                    _isRunning = false;
                }
            }
        }

        private DateTime? ReadLastCleanupTime()
        {
            try
            {
                if (!File.Exists(_lastCleanupFilePath))
                    return null;

                var text = File.ReadAllText(_lastCleanupFilePath).Trim();
                if (DateTime.TryParse(text, out var time))
                    return time;
            }
            catch
            {
                // 标记文件损坏时直接重新清理一次。
            }

            return null;
        }

        private void CleanupOldLogs(DateTime now)
        {
            var cutoff = now.AddDays(-CleanupIntervalDays);
            var logDirectories = GetLogDirectories().ToList();
            var deletedCount = 0;
            long deletedBytes = 0;
            var failedCount = 0;

            SafeAppendRecord(
                $"[{now:yyyy-MM-dd HH:mm:ss}] 开始日志清理：删除 {cutoff:yyyy-MM-dd HH:mm:ss} 之前的日志；日志目录数量：{logDirectories.Count}\r\n");

            foreach (var logDirectory in logDirectories)
            {
                foreach (var file in EnumerateLogFiles(logDirectory))
                {
                    try
                    {
                        if (IsControlFile(file))
                            continue;

                        var lastWriteTime = File.GetLastWriteTime(file);
                        if (lastWriteTime >= cutoff)
                            continue;

                        var length = 0L;
                        try { length = new FileInfo(file).Length; } catch { }

                        File.Delete(file);
                        deletedCount++;
                        deletedBytes += length;
                        SafeAppendRecord($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 已删除旧日志：{file}，最后修改时间：{lastWriteTime:yyyy-MM-dd HH:mm:ss}\r\n");
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        SafeAppendRecord($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 删除日志失败：{file}，原因：{ex.Message}\r\n");
                    }
                }
            }

            SafeAppendRecord(
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 日志清理完成：删除 {deletedCount} 个文件，释放 {deletedBytes} 字节，失败 {failedCount} 个文件；下次最早清理时间：{now.AddDays(CleanupIntervalDays):yyyy-MM-dd HH:mm:ss}\r\n\r\n");
        }

        private IEnumerable<string> GetLogDirectories()
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var name in LogDirectoryNames)
            {
                var path = Path.Combine(_baseDirectory, name);
                if (Directory.Exists(path))
                    result.Add(path);
            }

            // 兼容模块/公共目录下的 Logs 子目录，但只在程序目录内查找，避免误删用户其它文件。
            IEnumerable<string> childDirectories;
            try
            {
                childDirectories = Directory.EnumerateDirectories(_baseDirectory, "*", SearchOption.AllDirectories).ToList();
            }
            catch
            {
                childDirectories = Array.Empty<string>();
            }

            foreach (var directory in childDirectories)
            {
                var name = Path.GetFileName(directory);
                if (LogDirectoryNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                    result.Add(directory);
            }

            // 控制目录即使没有其它日志，也要返回，用于记录清理时间。
            result.Add(_controlDirectory);
            return result;
        }

        private static IEnumerable<string> EnumerateLogFiles(string directory)
        {
            if (!Directory.Exists(directory))
                yield break;

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).ToList();
            }
            catch
            {
                yield break;
            }

            foreach (var file in files)
            {
                var extension = Path.GetExtension(file);
                if (LogFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                    yield return file;
            }
        }

        private bool IsControlFile(string file)
        {
            return string.Equals(file, _lastCleanupFilePath, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(file, _cleanupRecordFilePath, StringComparison.OrdinalIgnoreCase);
        }

        private void SafeAppendRecord(string message)
        {
            try
            {
                Directory.CreateDirectory(_controlDirectory);
                File.AppendAllText(_cleanupRecordFilePath, message);
            }
            catch
            {
                // 清理记录失败不能影响主程序启动。
            }
        }
    }
}
