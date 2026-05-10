namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 微信连接管理服务接口
    /// 负责管理微信客户端的连接生命周期和状态
    /// </summary>
    public interface IWeChatConnectionService
    {
        /// <summary>
        /// 获取当前连接状态
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 获取连接状态描述
        /// </summary>
        string ConnectionStatus { get; }

        /// <summary>
        /// 连接状态变化事件
        /// </summary>
        event EventHandler<ConnectionStatusChangedEventArgs>? ConnectionStatusChanged;

        /// <summary>
        /// 启动微信连接服务
        /// 在调试模式下直接连接，在生产模式下监听微信进程
        /// </summary>
        /// <returns>启动任务</returns>
        Task StartAsync();

        /// <summary>
        /// 停止微信连接服务
        /// </summary>
        /// <returns>停止任务</returns>
        Task StopAsync();
    }

    /// <summary>
    /// 连接状态变化事件参数
    /// </summary>
    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// 连接状态描述
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }
}
