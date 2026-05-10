using HRB.Payment.Core.Events;
using HRB.Payment.Core.Models;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 支付消息转换服务接口
    /// 负责将支付消息转换为支付事件参数
    /// </summary>
    public interface IPaymentMessageConverter
    {
        /// <summary>
        /// 将支付消息转换为支付事件参数
        /// </summary>
        /// <param name="paymentMessage">支付消息对象</param>
        /// <param name="paymentChannel">支付渠道</param>
        /// <returns>支付事件参数对象，转换失败返回null</returns>
        PaymentEventArgs? ConvertToPaymentEventArgs(PaymentMessage paymentMessage, PaymentChannel paymentChannel);
    }
}
