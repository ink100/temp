using HRB.Payment.Core.Models;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 微信消息解析服务接口
    /// 负责解析微信支付通知的XML消息
    /// </summary>
    public interface IWeChatMessageParser
    {
        /// <summary>
        /// 解析微信支付消息XML
        /// </summary>
        /// <param name="xmlContent">XML格式的支付消息内容</param>
        /// <returns>解析后的支付消息对象，解析失败返回null</returns>
        PaymentMessage? ParsePaymentMessage(string xmlContent);
    }
}
