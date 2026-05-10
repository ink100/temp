using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace HRB.Payment.Core.Helpers
{
    /// <summary>
    /// 开机自启动帮助类
    /// </summary>
    public static class AutoStartupHelper
    {
        private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private static readonly string AppName = "HRBPaymentClient";

        /// <summary>
        /// 获取应用程序可执行文件路径
        /// </summary>
        private static string GetExecutablePath()
        {
            return Process.GetCurrentProcess().MainModule?.FileName ?? Assembly.GetEntryAssembly()?.Location ?? string.Empty;
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

                using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true))
                {
                    if (key != null)
                    {
                        key.SetValue(AppName, $"\"{exePath}\"");
                        return true;
                    }
                }
            }
            catch
            {
                // 注册表操作失败
            }

            return false;
        }

        /// <summary>
        /// 禁用开机自启动
        /// </summary>
        public static bool Disable()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true))
                {
                    if (key != null)
                    {
                        var value = key.GetValue(AppName);
                        if (value != null)
                        {
                            key.DeleteValue(AppName);
                        }
                        return true;
                    }
                }
            }
            catch
            {
                // 注册表操作失败
            }

            return false;
        }

        /// <summary>
        /// 检查是否已启用开机自启动
        /// </summary>
        public static bool IsEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false))
                {
                    if (key != null)
                    {
                        var value = key.GetValue(AppName);
                        return value != null;
                    }
                }
            }
            catch
            {
                // 注册表读取失败
            }

            return false;
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
