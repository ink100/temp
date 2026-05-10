using HRB.Payment.Core.Models;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 支付语音播报服务接口
    /// 负责所有支付相关的语音播报逻辑
    /// </summary>
    public interface IPaymentVoiceService
    {
        /// <summary>
        /// 播放支付开始提示音
        /// </summary>
        /// <param name="channel">支付渠道</param>
        /// <param name="nickname">用户昵称（可选）</param>
        /// <param name="orderNumber">订单号（用于去重，可选）</param>
        Task PlayPaymentStartedAsync(PaymentChannel channel, string? nickname = null, string? orderNumber = null);

        /// <summary>
        /// 播放支付开始提示音（带"上次未付款"提示）
        /// </summary>
        /// <param name="channel">支付渠道</param>
        /// <param name="nickname">用户昵称（可选）</param>
        /// <param name="orderNumber">订单号（用于去重，可选）</param>
        Task PlayPaymentStartedWithBeforeNotPayAsync(PaymentChannel channel, string? nickname = null, string? orderNumber = null);

        /// <summary>
        /// 播放支付取消提示音
        /// </summary>
        /// <param name="nickname">用户昵称（可选）</param>
        /// <param name="orderNumber">订单号（用于去重和抑制旧的未支付提示，可选）</param>
        Task PlayPaymentCancelledAsync(string? nickname = null, string? orderNumber = null);

        /// <summary>
        /// 播放支付成功语音（金额播报）
        /// </summary>
        /// <param name="amount">支付金额</param>
        /// <param name="channel">支付渠道</param>
        /// <param name="orderNumber">订单号（用于去重和抑制旧的未支付提示，可选）</param>
        Task PlayPaymentSuccessAsync(decimal amount, PaymentChannel channel, string? orderNumber = null);

        /// <summary>
        /// 播放"扫码未支付"提示音
        /// </summary>
        /// <param name="orderNumber">订单号（用于在订单已成功/取消后跳过过期提示，可选）</param>
        Task PlayScanNotPayAsync(string? orderNumber = null);

        /// <summary>
        /// 标记订单已经完成，用于跳过队列中尚未播放的过期“扫码未支付”提示。
        /// </summary>
        /// <param name="orderNumber">订单号</param>
        void MarkOrderCompleted(string? orderNumber);
    }
}
