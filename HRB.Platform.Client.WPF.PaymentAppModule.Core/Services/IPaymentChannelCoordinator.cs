namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 支付渠道协调器接口
    /// 负责微信和支付宝的启动、停止和状态管理
    /// </summary>
    public interface IPaymentChannelCoordinator
    {
        /// <summary>
        /// 启动微信客户端/插件（根据新旧模式不同处理）
        /// </summary>
        /// <param name="onLoginPollingStart">登录轮询启动回调</param>
        Task StartWeChatAsync(Action? onLoginPollingStart = null);

        ///// <summary>
        ///// 停止微信支付渠道
        ///// </summary>
        //Task StopWeChatAsync();

        ///// <summary>
        ///// 启动支付宝支付渠道
        ///// </summary>
        //Task StartAlipayAsync();

        /// <summary>
        /// 启动支付宝插件（仅启动插件进程，等待插件就绪）
        /// </summary>
        Task StartAlipayShellAsync();

        /// <summary>
        /// 启动支付宝业务轮询（发送StartAlipayPolling命令）
        /// </summary>
        Task StartAlipayPollingAsync();

        /// <summary>
        /// 停止支付宝支付渠道
        /// </summary>
        Task StopAlipayAsync();




        /// <summary>
        /// 给支付宝插件发送开始事件，直到插件响应已启动（AppStarted）为止
        /// </summary>
        /// <returns></returns>
        Task SendAlipayAppStart();
    }
}
