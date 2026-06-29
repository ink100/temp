using HRB.Payment.Core.Models;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Helpers;
using System.Diagnostics;
using System.Text;
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
                if (string.IsNullOrWhiteSpace(xmlContent))
                {
                    WeChatListenerConsoleDebug.Write("XML-EMPTY", "微信 Message XML 为空");
                    return null;
                }

                WeChatListenerConsoleDebug.Write("XML-RECV", $"MessageXmlLength={xmlContent.Length}");
                WeChatListenerConsoleDebug.WriteBlock("XML-RAW", "微信Message XML完整内容", xmlContent);
                var doc = XDocument.Parse(xmlContent);
                var sysmsg = doc.Element("sysmsg");

                // 验证消息类型是否为支付消息
                if (sysmsg == null || sysmsg.Attribute("type")?.Value != "paymsg")
                {
                    WeChatListenerConsoleDebug.Write("XML-NOT-PAYMSG", $"Root={doc.Root?.Name.LocalName ?? "null"}, Type={sysmsg?.Attribute("type")?.Value ?? "null"}");
                    return null;
                }

                var paymsg = sysmsg.Element("paymsg");
                if (paymsg == null)
                {
                    WeChatListenerConsoleDebug.Write("XML-NO-PAYMSG", "sysmsg 中缺少 paymsg 节点");
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
                var message = new PaymentMessage
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

                if (IsInvalidEmptyPayMessage(message))
                {
                    WeChatListenerConsoleDebug.Write("XML-DROP-EMPTY",
                        $"丢弃微信空支付消息：Status={message.Status}, TransId={message.TransId}, Fee={message.Fee}, DisplayName={message.DisplayName}, Username={message.Username}, Timestamp={message.Timestamp}");
                    WeChatListenerConsoleDebug.WriteBlock("XML-DROP-FIELDS", "被丢弃的paymsg字段明细", BuildPayMsgFields(paymsg));
                    return null;
                }

                WeChatListenerConsoleDebug.Write("XML-OK",
                    $"Status={message.Status}, TransId={message.TransId}, Fee={message.Fee}, DisplayName={message.DisplayName}, Username={message.Username}");
                WeChatListenerConsoleDebug.WriteBlock("XML-FIELDS", "paymsg字段明细", BuildPayMsgFields(paymsg));

                return message;
            }
            catch (Exception ex)
            {
                WeChatListenerConsoleDebug.WriteException("XML-ERROR", ex, xmlContent);
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


        private static bool IsInvalidEmptyPayMessage(PaymentMessage message)
        {
            if (message == null)
                return true;

            var hasOrder = !string.IsNullOrWhiteSpace(message.TransId);
            var hasUser = !string.IsNullOrWhiteSpace(message.Username) ||
                          !string.IsNullOrWhiteSpace(message.DisplayName);

            // 金额不参与空数据判断。
            // 只要存在真实流水号，或存在 wxid/昵称，就继续向后处理。
            // 只有订单、wxid、昵称都为空时，才认为是 VXModule.Shell 的空 paymsg/状态噪声。
            return !hasOrder && !hasUser;
        }

        private string BuildPayMsgFields(XElement paymsg)
        {
            var builder = new StringBuilder();
            foreach (var element in paymsg.Elements())
            {
                builder
                    .Append(element.Name.LocalName)
                    .Append(" = ")
                    .AppendLine(element.Value ?? string.Empty);
            }
            return builder.ToString();
        }
    }
}
