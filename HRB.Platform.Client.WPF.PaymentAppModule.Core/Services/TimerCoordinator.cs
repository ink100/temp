using HRB.Payment.Core.Models;
using HRB.Platform.Client.Core.Interfaces;
using System.Windows.Threading;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 定时器协调器实现
    /// 负责统一管理所有定时器的生命周期
    /// </summary>
    public class TimerCoordinator : ITimerCoordinator
    {
        // 时间更新定时器
        private DispatcherTimer? _timeUpdateTimer;

        // 订单监控定时器（使用 Task 循环代替 DispatcherTimer）
        private CancellationTokenSource? _orderMonitoringCts;
        private Task? _orderMonitoringTask;

        // 微信登录轮询定时器
        private DispatcherTimer? _wechatLoginPollingTimer;
        private volatile bool _isPollingWeChatLogin = false;
        private volatile bool _hasTriggeredLoginSuccess = false;

        private readonly IHrbLogger _log;

        public TimerCoordinator()
        {
            _log = GlobalSettings.CurrentAppContext.CurrentLogger;
        }

        /// <summary>
        /// 启动时间更新定时器
        /// </summary>
        public void StartTimeUpdate(Action<DateTime> onUpdate)
        {
            // 停止现有定时器
            _timeUpdateTimer?.Stop();

            // 立即执行一次更新
            onUpdate(DateTime.Now);

            // 创建定时器，每秒更新一次
            _timeUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timeUpdateTimer.Tick += (s, e) =>
            {
                try
                {
                    onUpdate(DateTime.Now);
                }
                catch (Exception ex)
                {
                    _log.Info($"时间更新错误: {ex.Message}");
                }
            };
            _timeUpdateTimer.Start();

            _log.Info("时间更新定时器已启动");
        }

        /// <summary>
        /// 启动订单监控定时器
        /// </summary>
        public void StartOrderMonitoring(
            Func<IEnumerable<TransactionRecord>> getOrders,
            Func<DateTime> getCurrentTime,
            Action<IEnumerable<TransactionRecord>, DateTime> onCheck)
        {
            // 停止现有任务
            StopOrderMonitoring();

            // 创建新的取消令牌
            _orderMonitoringCts = new CancellationTokenSource();
            var token = _orderMonitoringCts.Token;

            // 启动异步循环任务
            _orderMonitoringTask = Task.Run(async () =>
            {
                _log.Info("订单监控定时器已启动");

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var orders = getOrders();
                        var currentTime = getCurrentTime();
                        onCheck(orders, currentTime);
                    }
                    catch (Exception ex)
                    {
                        _log.Info($"订单监控错误: {ex.Message}");
                    }

                    try
                    {
                        await Task.Delay(3000, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }

                _log.Info("订单监控定时器已停止");
            }, token);
        }

        /// <summary>
        /// 停止订单监控
        /// </summary>
        private void StopOrderMonitoring()
        {
            if (_orderMonitoringCts != null)
            {
                _orderMonitoringCts.Cancel();
                _orderMonitoringCts.Dispose();
                _orderMonitoringCts = null;
            }

            _orderMonitoringTask = null;
        }

        /// <summary>
        /// 启动微信登录轮询定时器
        /// </summary>
        public void StartWeChatLoginPolling(
            Func<Task<bool>> checkLogin,
            Func<Task> onLoginSuccess)
        {
            // 停止现有定时器
            StopWeChatLoginPolling();

            _isPollingWeChatLogin = true;
            _hasTriggeredLoginSuccess = false;

            _wechatLoginPollingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };

            _wechatLoginPollingTimer.Tick += async (s, e) =>
            {
                if (!_isPollingWeChatLogin || _hasTriggeredLoginSuccess)
                    return;

                try
                {
                    var isLoggedIn = await checkLogin();
                    if (isLoggedIn)
                    {
                        _hasTriggeredLoginSuccess = true;
                        _wechatLoginPollingTimer?.Stop();

                        await onLoginSuccess();

                        _log.Info("微信登录成功，停止轮询");
                    }
                }
                catch (Exception ex)
                {
                    _log.Info($"微信登录轮询错误: {ex.Message}");
                }
            };

            _wechatLoginPollingTimer.Start();
            _log.Info("微信登录轮询定时器已启动");
        }

        /// <summary>
        /// 停止微信登录轮询
        /// </summary>
        public void StopWeChatLoginPolling()
        {
            _isPollingWeChatLogin = false;
            _hasTriggeredLoginSuccess = true;

            if (_wechatLoginPollingTimer != null)
            {
                _wechatLoginPollingTimer.Stop();
                _wechatLoginPollingTimer = null;
                _log.Info("微信登录轮询定时器已停止");
            }
        }

        /// <summary>
        /// 停止所有定时器
        /// </summary>
        public void StopAll()
        {
            _timeUpdateTimer?.Stop();
            _timeUpdateTimer = null;

            StopOrderMonitoring();
            StopWeChatLoginPolling();

            _log.Info("所有定时器已停止");
        }
    }
}
