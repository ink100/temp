using HRB.Payment.Core.Models;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 订单状态管理服务实现
    /// 负责订单超时检查、播报次数追踪、静默取消管理
    /// </summary>
    public class OrderStateManager : IOrderStateManager
    {
        // 订单播报次数追踪（订单号 -> 播报次数）
        private readonly Dictionary<string, int> _orderNotificationCount = new();

        // 静默取消的订单号集合（超时取消或重复扫码取消）
        private readonly HashSet<string> _silentCancelOrders = new();

        // 线程锁
        private readonly object _lockObject = new();

        /// <summary>
        /// 订单超时事件（超过2分钟未支付）
        /// </summary>
        public event EventHandler<OrderTimeoutEventArgs>? OrderTimeout;

        /// <summary>
        /// 订单需要播报提示事件（20秒、40秒、60秒）
        /// </summary>
        public event EventHandler<OrderNotificationEventArgs>? OrderNotification;

        /// <summary>
        /// 开始追踪订单
        /// </summary>
        public void TrackOrder(string orderNumber)
        {
            lock (_lockObject)
            {
                _orderNotificationCount[orderNumber] = 0;
            }
        }

        /// <summary>
        /// 停止追踪订单
        /// </summary>
        public void UntrackOrder(string orderNumber)
        {
            lock (_lockObject)
            {
                _orderNotificationCount.Remove(orderNumber);
            }
        }

        /// <summary>
        /// 标记订单为静默取消（超时或重复扫码导致的取消）
        /// </summary>
        public void MarkSilentCancel(string orderNumber)
        {
            lock (_lockObject)
            {
                _silentCancelOrders.Add(orderNumber);
            }
        }

        /// <summary>
        /// 判断订单是否为静默取消
        /// </summary>
        public bool IsSilentCancel(string orderNumber)
        {
            lock (_lockObject)
            {
                return _silentCancelOrders.Contains(orderNumber);
            }
        }

        /// <summary>
        /// 清除静默取消标记
        /// </summary>
        public void ClearSilentCancel(string orderNumber)
        {
            lock (_lockObject)
            {
                _silentCancelOrders.Remove(orderNumber);
            }
        }

        /// <summary>
        /// 检查订单状态（由定时器调用）
        /// </summary>
        public void CheckOrderStates(IEnumerable<TransactionRecord> scanningOrders, DateTime currentTime)
        {
            try
            {
                foreach (var order in scanningOrders)
                {
                    if (IsSilentCancel(order.OrderNumber))
                        continue;

                    var elapsedSeconds = (currentTime - order.CreatedAt).TotalSeconds;

                    var settings = GlobalSettings.CurrentAppContext.CurrentSettings;
                    var timeoutSeconds = Math.Clamp(settings.ScanTimeoutSeconds <= 0 ? 120 : settings.ScanTimeoutSeconds, 1, 3600);
                    var notifyIntervalSeconds = Math.Clamp(settings.ScanNotPayNotifyIntervalSeconds <= 0 ? 10 : settings.ScanNotPayNotifyIntervalSeconds, 1, timeoutSeconds);

                    // 按设置的间隔提醒“扫码未支付”，到达超时时间后停止提醒并进入超时取消。
                    if (settings.IsScanNotPayVoiceEnabled && elapsedSeconds >= notifyIntervalSeconds && elapsedSeconds < timeoutSeconds)
                    {
                        var maxNotifyCount = Math.Clamp(settings.ScanNotPayVoiceRepeatCount <= 0 ? 1 : settings.ScanNotPayVoiceRepeatCount, 1, 20);
                        CheckAndNotify(order, elapsedSeconds, notifyIntervalSeconds, maxNotifyCount);
                    }

                    // 超过用户设置的秒数，触发超时事件
                    if (elapsedSeconds >= timeoutSeconds)
                    {
                        TriggerTimeout(order, currentTime);
                    }
                }
            }
            catch (Exception ex)
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Error($"订单状态检查错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查并触发通知事件
        /// </summary>
        private void CheckAndNotify(TransactionRecord order, double elapsedSeconds, int notifyIntervalSeconds, int maxNotifyCount)
        {
            lock (_lockObject)
            {
                // 获取当前播报次数
                if (!_orderNotificationCount.TryGetValue(order.OrderNumber, out var notificationCount))
                {
                    notificationCount = 0;
                    _orderNotificationCount[order.OrderNumber] = 0;
                }

                // 提醒间隔、最多次数和超时秒数均可配置。
                // 默认提醒间隔 10 秒、超时 10 秒、最多提醒 1 次，即第 10 秒提醒一次并触发超时取消。
                var expectedCount = Math.Min((int)(elapsedSeconds / notifyIntervalSeconds), maxNotifyCount);

                // 如果还没播报到这个次数，触发通知事件
                if (notificationCount < expectedCount)
                {
                    _orderNotificationCount[order.OrderNumber] = expectedCount;

                    GlobalSettings.CurrentAppContext.CurrentLogger.Info($"订单 {order.OrderNumber} 第 {expectedCount} 次扫码未支付提示，已等待 {elapsedSeconds:F0} 秒");

                    // 触发通知事件
                    OrderNotification?.Invoke(this, new OrderNotificationEventArgs
                    {
                        OrderNumber = order.OrderNumber,
                        NotificationCount = expectedCount,
                        ElapsedSeconds = elapsedSeconds
                    });
                }
            }
        }

        /// <summary>
        /// 触发超时事件
        /// </summary>
        private void TriggerTimeout(TransactionRecord order, DateTime currentTime)
        {
            // 标记为静默取消
            MarkSilentCancel(order.OrderNumber);

            GlobalSettings.CurrentAppContext.CurrentLogger.Error($"订单超时自动取消: {order.OrderNumber}, 已超时 {(currentTime - order.CreatedAt).TotalSeconds:F0} 秒");

            // 触发超时事件
            OrderTimeout?.Invoke(this, new OrderTimeoutEventArgs
            {
                OrderNumber = order.OrderNumber,
                UserId = order.UserId,
                DisplayName = string.IsNullOrWhiteSpace(order.DisplayName) ? order.Remarks : order.DisplayName,
                Amount = order.Amount,
                PaymentChannel = order.PaymentChannel,
                Remarks = order.Remarks,
                CreatedAt = order.CreatedAt
            });
        }
    }
}
