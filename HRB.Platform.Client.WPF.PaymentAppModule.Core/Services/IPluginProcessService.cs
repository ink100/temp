namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 插件进程管理服务接口
    /// </summary>
    public interface IPluginProcessService
    {
        /// <summary>
        /// 启动总服务（必须最先启动）
        /// </summary>
        /// <returns>是否成功</returns>
        Task<bool> StartMessageServiceAsync();

        /// <summary>
        /// 启动支付宝通知插件
        /// </summary>
        /// <returns>是否成功</returns>
        Task<bool> StartAlipayShellAsync();

        /// <summary>
        /// 启动微信通知插件
        /// </summary>
        /// <returns>是否成功</returns>
        Task<bool> StartWeChatShellAsync();

        ///// <summary>
        ///// 停止支付宝通知插件
        ///// </summary>
        ///// <returns>是否成功</returns>
        //Task<bool> StopAlipayShellAsync();


        ///// <summary>
        ///// 停止微信插件进程
        ///// </summary>
        //void StopWeChatShell();


        /// <summary>
        /// 检查插件是否正在运行
        /// </summary>
        /// <param name="pluginName">插件名称</param>
        /// <returns>是否运行中</returns>
        bool IsPluginRunning(string pluginName);



        /// <summary>
        /// 清理所有已存在的插件进程（应用启动前调用）
        /// </summary>
        Task CleanupExistingProcessesAsync();

        /// <summary>
        /// 启动进程
        /// </summary>
        /// <param name="pluginName"></param>
        /// <param name="exePath"></param>
        /// <param name="logMessage"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        Task<bool> StartProcessAsync(string pluginName, string exePath, string logMessage, string? fileName = null);


        /// <summary>
        /// 停止指定进程
        /// </summary>
        /// <param name="exeName"></param>
        /// <param name="logMessage"></param>
        /// <returns></returns>
        Task<bool> StopProcessAsync(string exeName, string logMessage="");
    }
}
