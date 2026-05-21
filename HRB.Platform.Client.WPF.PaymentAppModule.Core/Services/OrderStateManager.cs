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
            if (string.IsNullOrWhiteSpace(orderNumber))
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Info("跳过追踪订单：订单号为空");
                return;
            }

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
            if (string.IsNullOrWhiteSpace(orderNumber))
                return;

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
            if (string.IsNullOrWhiteSpace(orderNumber))
                return;

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
            if (string.IsNullOrWhiteSpace(orderNumber))
                return false;

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
            if (string.IsNullOrWhiteSpace(orderNumber))
                return;

            lock (_lockObject)
            {
                _silentCancelOrders.Remove(orderNumber);
            }
        }
        /// <summary>
        /// 判断订单是否仍处于追踪状态。
        /// 超时未支付后会停止追踪，避免每 3 秒重复触发超时事件。
        /// </summary>
        private bool IsTrackedOrder(string orderNumber)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
                return false;

            lock (_lockObject)
            {
                return _orderNotificationCount.ContainsKey(orderNumber);
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
                    if (order == null || string.IsNullOrWhiteSpace(order.OrderNumber))
                    {
                        GlobalSettings.CurrentAppContext.CurrentLogger.Info("订单状态检查跳过：订单对象为空或订单号为空");
                        continue;
                    }
                    if (IsSilentCancel(order.OrderNumber))
                        continue;
                    // 已经停止追踪的订单不再检查。
                    // 例如：订单已经成功、真实取消，或者已经触发过一次“超时未支付”提醒。
                    if (!IsTrackedOrder(order.OrderNumber))
                        continue;
                    var elapsedSeconds = (currentTime - order.CreatedAt).TotalSeconds;

                    var settings = GlobalSettings.CurrentAppContext.CurrentSettings;
                    var timeoutSeconds = Math.Clamp(settings.ScanTimeoutSeconds <= 0 ? 15 : settings.ScanTimeoutSeconds, 1, 3600);
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
            if (order == null || string.IsNullOrWhiteSpace(order.OrderNumber))
                return;

            lock (_lockObject)
            {
                if (!_orderNotificationCount.TryGetValue(order.OrderNumber, out var notificationCount))
                {
                    // 订单已经停止追踪，可能已经成功、取消或已经触发过超时未支付提醒。
                    return;
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
            if (order == null || string.IsNullOrWhiteSpace(order.OrderNumber))
                return;

            // 快照可能已经过期。
            // 如果订单已经支付成功、真实取消或被其他流程停止追踪，则不再触发超时未支付。
            if (!IsTrackedOrder(order.OrderNumber))
                return;

            // 超时未支付不是“取消支付”。
            // 这里只停止继续追踪，避免后续每 3 秒重复触发超时事件。
            UntrackOrder(order.OrderNumber);

            GlobalSettings.CurrentAppContext.CurrentLogger.Info(
                $"订单超时未支付: {order.OrderNumber}, 已等待 {(currentTime - order.CreatedAt).TotalSeconds:F0} 秒");

            // 触发“超时未支付”事件。
            // 注意：该事件不应该再被转成 PaymentCancelled。
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
