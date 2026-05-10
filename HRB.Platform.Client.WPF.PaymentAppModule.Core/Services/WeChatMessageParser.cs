using HRB.Payment.Core.Models;
using System.Diagnostics;
using System.Xml.Linq;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 微信消息解析服务实现
    /// 负责解析微信支付通知的XML消息
    /// </summary>
    public class WeChatMessageParser : IWeChatMessageParser
    {
        /// <summary>
        /// 解析微信支付消息XML
        /// </summary>
        /// <param name="xmlContent">XML格式的支付消息内容</param>
        /// <returns>解析后的支付消息对象，解析失败返回null</returns>
        public PaymentMessage? ParsePaymentMessage(string xmlContent)
        {
            try
            {
                var doc = XDocument.Parse(xmlContent);
                var sysmsg = doc.Element("sysmsg");

                // 验证消息类型是否为支付消息
                if (sysmsg == null || sysmsg.Attribute("type")?.Value != "paymsg")
                {
                    return null;
                }

                var paymsg = sysmsg.Element("paymsg");
                if (paymsg == null)
                {
                    return null;
                }

                // 解析支付状态
                var statusValue = GetElementValue(paymsg, "status");
                PaymentStatus status = PaymentStatus.Scan; // 默认值
                if (int.TryParse(statusValue, out int statusInt))
                {
                    status = (PaymentStatus)statusInt;
                }

                // 构建支付消息对象
                return new PaymentMessage
                {
                    PayMsgType = GetElementValue(paymsg, "PayMsgType"),
                    TransId = GetElementValue(paymsg, "transid"),
                    Username = GetElementValue(paymsg, "username"),
                    DisplayName = GetElementValue(paymsg, "displayname"),
                    Timestamp = GetElementValue(paymsg, "timestamp"),
                    Fee = GetElementValue(paymsg, "fee"),
                    FeeType = GetElementValue(paymsg, "feetype"),
                    HeadImgUrl = GetElementValue(paymsg, "headimgurl"),
                    Scene = GetElementValue(paymsg, "scene"),
                    Status = status
                };
            }
            catch (Exception ex)
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Error($"XML 解析异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取XML元素的值
        /// </summary>
        /// <param name="parent">父元素</param>
        /// <param name="elementName">子元素名称</param>
        /// <returns>元素值，不存在返回空字符串</returns>
        private string GetElementValue(XElement parent, string elementName)
        {
            var element = parent.Element(elementName);
            return element?.Value ?? string.Empty;
        }
    }
}
