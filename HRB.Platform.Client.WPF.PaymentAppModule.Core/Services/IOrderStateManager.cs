using HRB.Payment.Core.Models;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 订单超时事件参数
    /// </summary>
    public class OrderTimeoutEventArgs : EventArgs
    {
        public string OrderNumber { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentChannel PaymentChannel { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// 订单通知事件参数
    /// </summary>
    public class OrderNotificationEventArgs : EventArgs
    {
        public string OrderNumber { get; set; } = string.Empty;
        public int NotificationCount { get; set; }
        public double ElapsedSeconds { get; set; }
    }

    /// <summary>
    /// 订单状态管理服务接口
    /// 负责订单超时检查、播报次数追踪、静默取消管理
    /// </summary>
    public interface IOrderStateManager
    {
        /// <summary>
        /// 订单超时事件（超过2分钟未支付）
        /// </summary>
        event EventHandler<OrderTimeoutEventArgs>? OrderTimeout;

        /// <summary>
        /// 订单需要播报提示事件（20秒、40秒、60秒）
        /// </summary>
        event EventHandler<OrderNotificationEventArgs>? OrderNotification;

        /// <summary>
        /// 开始追踪订单
        /// </summary>
        /// <param name="orderNumber">订单号</param>
        void TrackOrder(string orderNumber);

        /// <summary>
        /// 停止追踪订单
        /// </summary>
        /// <param name="orderNumber">订单号</param>
        void UntrackOrder(string orderNumber);

        /// <summary>
        /// 标记订单为静默取消（超时或重复扫码导致的取消）
        /// </summary>
        /// <param name="orderNumber">订单号</param>
        void MarkSilentCancel(string orderNumber);

        /// <summary>
        /// 判断订单是否为静默取消
        /// </summary>
        /// <param name="orderNumber">订单号</param>
        /// <returns>是否为静默取消</returns>
        bool IsSilentCancel(string orderNumber);

        /// <summary>
        /// 清除静默取消标记
        /// </summary>
        /// <param name="orderNumber">订单号</param>
        void ClearSilentCancel(string orderNumber);

        /// <summary>
        /// 检查订单状态（由定时器调用）
        /// </summary>
        /// <param name="scanningOrders">当前扫码中的订单列表</param>
        /// <param name="currentTime">当前时间</param>
        void CheckOrderStates(IEnumerable<TransactionRecord> scanningOrders, DateTime currentTime);
    }
}
