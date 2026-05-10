using HRB.Payment.Core.Models;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 定时器协调器接口
    /// 负责统一管理所有定时器的生命周期
    /// </summary>
    public interface ITimerCoordinator
    {
        /// <summary>
        /// 启动时间更新定时器
        /// </summary>
        /// <param name="onUpdate">时间更新回调</param>
        void StartTimeUpdate(Action<DateTime> onUpdate);

        /// <summary>
        /// 启动订单监控定时器
        /// </summary>
        /// <param name="getOrders">获取扫码中订单的回调</param>
        /// <param name="getCurrentTime">获取当前时间的回调</param>
        /// <param name="onCheck">检查订单状态的回调</param>
        void StartOrderMonitoring(
            Func<IEnumerable<TransactionRecord>> getOrders,
            Func<DateTime> getCurrentTime,
            Action<IEnumerable<TransactionRecord>, DateTime> onCheck);

        /// <summary>
        /// 启动微信登录轮询定时器
        /// </summary>
        /// <param name="checkLogin">检查登录状态的回调</param>
        /// <param name="onLoginSuccess">登录成功的回调</param>
        void StartWeChatLoginPolling(
            Func<Task<bool>> checkLogin,
            Func<Task> onLoginSuccess);

        /// <summary>
        /// 停止微信登录轮询
        /// </summary>
        void StopWeChatLoginPolling();

        /// <summary>
        /// 停止所有定时器
        /// </summary>
        void StopAll();
    }
}
