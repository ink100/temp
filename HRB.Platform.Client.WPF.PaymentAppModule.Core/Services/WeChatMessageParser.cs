using HRB.Payment.Core.Models;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Helpers;
using System.Diagnostics;
using System.Text;
using System.Xml;
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
                XDocument doc;
                var wasRecoveredFromTruncatedCData = false;
                try
                {
                    doc = XDocument.Parse(xmlContent);
                }
                catch (XmlException ex) when (
                    WeChatXmlRecovery.TryRepairTruncatedCData(
                        xmlContent,
                        out var repairedXml,
                        out var omittedElementName))
                {
                    // VXModule.Shell 偶尔会在最后一个 CDATA 字段中途截断消息。
                    // 只丢弃这个未完成字段，其余内容仍交给标准 XML 解析器和业务校验，
                    // 避免通过字符串拼接直接构造支付对象。
                    doc = XDocument.Parse(repairedXml);
                    wasRecoveredFromTruncatedCData = true;
                    WeChatListenerConsoleDebug.Write(
                        "XML-RECOVERED",
                        $"已恢复末尾CDATA截断消息：OmittedElement={omittedElementName}, OriginalLength={xmlContent.Length}, Error={ex.Message}");
                    GlobalSettings.CurrentAppContext.CurrentLogger.Info(
                        $"收到末尾CDATA截断的微信XML，已安全忽略未完成字段: {omittedElementName}");
                }

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
                var parsedStatus = int.TryParse(statusValue, out int statusInt);
                var hasValidStatus = parsedStatus && Enum.IsDefined(typeof(PaymentStatus), statusInt);
                if (parsedStatus)
                {
                    status = (PaymentStatus)statusInt;
                }

                // 缺失、非数字或未知状态不能回退为扫码事件，否则畸形消息可能触发 PaymentStarted。
                if (!hasValidStatus)
                {
                    WeChatListenerConsoleDebug.Write(
                        "XML-DROP-STATUS",
                        $"丢弃缺少有效状态的微信支付消息：StatusRaw={statusValue}");
                    return null;
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

                if (wasRecoveredFromTruncatedCData &&
                    !IsSafeRecoveredPaymentMessage(message, out var recoveryRejectReason))
                {
                    WeChatListenerConsoleDebug.Write(
                        "XML-DROP-RECOVERED",
                        $"丢弃关键字段不完整的CDATA恢复消息：{recoveryRejectReason}, Status={message.Status}, TransId={message.TransId}, Fee={message.Fee}");
                    return null;
                }

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
                GlobalSettings.CurrentAppContext.CurrentLogger.Error(
                    $"XML 解析异常: {ex.Message}{Environment.NewLine}" +
                    $"===== 微信XML原始完整内容 START Length={xmlContent?.Length ?? 0} ====={Environment.NewLine}" +
                    $"{xmlContent ?? "<null>"}{Environment.NewLine}" +
                    "===== 微信XML原始完整内容 END =====");
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


        private static bool IsSafeRecoveredPaymentMessage(PaymentMessage message, out string reason)
        {
            // 恢复消息采用比正常消息更严格的门槛，避免把关键字段已丢失的截断数据
            // 降级为 TMP 订单或金额为 0 的支付成功事件。
            if (string.IsNullOrWhiteSpace(message.TransId))
            {
                reason = "缺少完整订单号";
                return false;
            }

            if (message.Status == PaymentStatus.Success &&
                (!decimal.TryParse(message.Fee?.Trim(), out var feeInCents) || feeInCents <= 0))
            {
                reason = "支付成功消息缺少有效金额";
                return false;
            }

            reason = string.Empty;
            return true;
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
