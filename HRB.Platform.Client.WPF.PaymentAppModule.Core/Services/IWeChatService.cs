using System.Collections.Generic;
using System.Threading.Tasks;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 微信服务接口
    /// 提供微信版本检测、登录状态检测和进程判断功能
    /// </summary>
    public interface IWeChatService
    {
        /// <summary>
        /// 检测微信版本是否符合要求
        /// </summary>
        /// <param name="requiredVersion">要求的微信版本号，如 "3.9.12.54"</param>
        /// <returns>版本是否匹配</returns>
        Task<bool> CheckWeChatVersionAsync(string requiredVersion);
        Task<bool> CheckWeChatVersionAsync();

        /// <summary>
        /// 检测微信登录状态
        /// </summary>
        /// <returns>是否已登录</returns>
        Task<bool> CheckWeChatLoginStatusAsync();

        /// <summary>
        /// 判断微信进程是否存在
        /// </summary>
        /// <returns>进程是否存在</returns>
        Task<bool> IsWeChatProcessRunningAsync();

        /// <summary>
        /// 获取微信进程信息
        /// </summary>
        /// <returns>进程信息对象，如果进程不存在则返回null</returns>
        Task<WeChatProcessInfo?> GetWeChatProcessInfoAsync();

        /// <summary>
        /// 获取当前微信版本号
        /// </summary>
        /// <returns>版本号字符串，如果未安装则返回null</returns>
        Task<string?> GetCurrentWeChatVersionAsync();

        /// <summary>
        /// 获取所有微信进程信息
        /// </summary>
        /// <returns>微信进程信息列表</returns>
        Task<List<WeChatProcessInfo>> GetAllWeChatProcessesAsync();

        /// <summary>
        /// 检查微信是否安装
        /// </summary>
        /// <returns>是否已安装</returns>
        Task<bool> IsWeChatInstalledAsync();

        /// <summary>
        /// 强制终止所有微信进程
        /// </summary>
        /// <returns>是否成功</returns>
        Task<bool> KillAllWeChatProcessesAsync();


        /// <summary>
        /// 启动微信
        /// </summary>
        /// <returns></returns>
        Task<bool> StartWeChatAsync();

    }

    /// <summary>
    /// 微信进程信息
    /// </summary>
    public class WeChatProcessInfo
    {
        /// <summary>
        /// 进程ID
        /// </summary>
        public int ProcessId { get; set; }

        /// <summary>
        /// 进程名称
        /// </summary>
        public string ProcessName { get; set; } = string.Empty;

        /// <summary>
        /// 可执行文件完整路径
        /// </summary>
        public string? ExecutablePath { get; set; }

        /// <summary>
        /// 文件版本号
        /// </summary>
        public string? FileVersion { get; set; }

        /// <summary>
        /// 是否已登录（通过内存特征判断）
        /// </summary>
        public bool IsLoggedIn { get; set; }

        /// <summary>
        /// 窗口句柄
        /// </summary>
        public nint MainWindowHandle { get; set; }

        /// <summary>
        /// 窗口标题
        /// </summary>
        public string? WindowTitle { get; set; }
    }

    /// <summary>
    /// 微信版本检测结果
    /// </summary>
    public class WeChatVersionCheckResult
    {
        /// <summary>
        /// 是否匹配
        /// </summary>
        public bool IsMatch { get; set; }

        /// <summary>
        /// 当前版本
        /// </summary>
        public string? CurrentVersion { get; set; }

        /// <summary>
        /// 要求的版本
        /// </summary>
        public string RequiredVersion { get; set; } = string.Empty;

        /// <summary>
        /// 检测消息
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 微信登录状态检测结果
    /// </summary>
    public class WeChatLoginStatusResult
    {
        /// <summary>
        /// 是否已登录
        /// </summary>
        public bool IsLoggedIn { get; set; }

        /// <summary>
        /// 登录状态描述
        /// </summary>
        public string StatusDescription { get; set; } = string.Empty;

        /// <summary>
        /// 检测时间
        /// </summary>
        public DateTime CheckTime { get; set; } = DateTime.Now;
    }
}