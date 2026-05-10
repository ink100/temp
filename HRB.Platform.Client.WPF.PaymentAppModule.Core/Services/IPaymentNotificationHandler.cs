using HRB.Payment.Message.Core.BusEvents;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 支付通知处理服务接口
    /// 负责协调支付通知的完整处理流程：解析 → 转换 → 事件发布
    /// </summary>
    public interface IPaymentNotificationHandler
    {
        /// <summary>
        /// 处理支付通知消息
        /// </summary>
        /// <param name="notification">支付通知消息对象</param>
        /// <returns>处理任务</returns>
        Task HandleNotificationAsync(NotificationPayMessageEto notification);
    }
}
