using HRB.Payment.Core.Models;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Abstractions
{
    /// <summary>
    /// 支付渠道插件契约。
    /// 每个支付渠道（支付宝、微信）实现此接口，通过 DI 注册到主程序。
    /// 主程序不感知具体渠道逻辑，只通过此接口交互。
    /// </summary>
    public interface IPaymentChannelPlugin : IDisposable
    {
        string ChannelId { get; }

        string DisplayName { get; }

        PaymentChannel Channel { get; }

        /// <summary>
        /// 插件进程是否就绪（由看门狗管理，主程序只读）
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// 业务是否已启用（用户在设置中开启）
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// 消息中心连接就绪后调用，订阅事件总线、发送初始命令等
        /// </summary>
        Task InitializeAsync(CancellationToken ct = default);

        /// <summary>
        /// 启用渠道业务（用户在设置页开启）
        /// </summary>
        Task EnableAsync();

        /// <summary>
        /// 禁用渠道业务（用户在设置页关闭）
        /// </summary>
        Task DisableAsync();

        /// <summary>
        /// 主程序退出时调用，发送业务停止命令（不杀进程）
        /// </summary>
        Task ShutdownAsync();

        event EventHandler<ChannelStatusChangedEventArgs>? StatusChanged;
    }
}
