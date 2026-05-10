using HRB.Payment.Core.Events;

namespace HRB.Payment.Core.Services
{
    public interface IPaymentEventPublisher
    {
        /// <summary>
        /// 发布用户正在支付事件
        /// </summary>
        void PublishPaymentStarted(PaymentEventArgs args);

        /// <summary>
        /// 发布用户取消支付事件
        /// </summary>
        void PublishPaymentCancelled(PaymentEventArgs args);

        /// <summary>
        /// 发布支付成功事件
        /// </summary>
        void PublishPaymentSuccess(PaymentEventArgs args);
    }
}

