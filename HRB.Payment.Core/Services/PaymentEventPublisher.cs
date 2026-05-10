using HRB.Payment.Core.Events;
using Prism.Events;

namespace HRB.Payment.Core.Services
{
    public class PaymentEventPublisher : IPaymentEventPublisher
    {
        private readonly IEventAggregator _eventAggregator;

        public PaymentEventPublisher(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
        }

        /// <summary>
        /// 发布用户正在支付事件
        /// </summary>
        public void PublishPaymentStarted(PaymentEventArgs args)
        {
            _eventAggregator.GetEvent<PaymentStartedEvent>().Publish(args);
        }

        /// <summary>
        /// 发布用户取消支付事件
        /// </summary>
        public void PublishPaymentCancelled(PaymentEventArgs args)
        {
            _eventAggregator.GetEvent<PaymentCancelledEvent>().Publish(args);
        }

        /// <summary>
        /// 发布支付成功事件
        /// </summary>
        public void PublishPaymentSuccess(PaymentEventArgs args)
        {
            _eventAggregator.GetEvent<PaymentSuccessEvent>().Publish(args);
        }
    }
}

