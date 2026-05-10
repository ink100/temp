using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace HRB.Payment.Core.Helpers
{


    public class PcHelper
    {
        // 时间偏移量（网络时间 - 本地时间）
        private static TimeSpan? _timeOffset = null;
        private static readonly object _timeOffsetLock = new object();


        public static DateTime GetNetNowTime()
        {
            // 如果已经有偏移量，直接返回本地时间+偏移
            if (_timeOffset.HasValue)
            {
                return DateTime.Now.Add(_timeOffset.Value);
            }

            // 首次调用，获取网络时间并计算偏移
            lock (_timeOffsetLock)
            {
                // 双重检查
                if (_timeOffset.HasValue)
                {
                    return DateTime.Now.Add(_timeOffset.Value);
                }

                WebRequest request = null;
                WebResponse response = null;
                try
                {
                    // 创建指向百度的WebRequest
                    request = WebRequest.Create("https://www.baidu.com");
                    request.Timeout = 5000; // 设置5秒超时
                    request.Credentials = CredentialCache.DefaultCredentials;
                    response = request.GetResponse();

                    // 遍历响应头，寻找"Date"字段
                    var headerCollection = response.Headers;
                    foreach (var key in headerCollection.AllKeys)
                    {
                        if (key == "Date")
                        {
                            string dateString = headerCollection[key];
                            // 解析找到的日期字符串
                            var networkTime = DateTime.Parse(dateString);
                            var localTime = DateTime.Now;

                            // 计算偏移量
                            _timeOffset = networkTime - localTime;

                            return networkTime;
                        }
                    }

                    // 未找到日期字段，使用本地时间
                    _timeOffset = TimeSpan.Zero;
                    return DateTime.Now;
                }
                catch (Exception)
                {
                    // 请求失败，使用本地时间
                    _timeOffset = TimeSpan.Zero;
                    return DateTime.Now;
                }
                finally
                {
                    // 清理资源
                    request?.Abort();
                    response?.Close();
                }
            }
        }


        public static string GetPhysicalSerialNumber()
        {

            var sn = Guid.NewGuid().ToString();

            try
            {


                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMedia");
                foreach (ManagementObject mo in searcher.Get())
                {
                    sn = mo["SerialNumber"]?.ToString().Trim();
                    break;
                }


                return sn;

            }
            catch
            {
                return sn;
            }

        }


        public static string GetFileVersion(string fileFullPath)
        {

            if (!File.Exists(fileFullPath))
            {
                return string.Empty;
            }

            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(fileFullPath);
            string fileVersion = versionInfo.FileVersion;
            return fileVersion;

        }


    }

}
