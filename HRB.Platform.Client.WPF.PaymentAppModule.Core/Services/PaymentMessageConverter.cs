using HRB.Payment.Core.Events;
using HRB.Payment.Core.Models;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Helpers;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 支付消息转换服务实现
    /// 负责将支付消息转换为支付事件参数
    /// </summary>
    public class PaymentMessageConverter : IPaymentMessageConverter
    {
        /// <summary>
        /// 将支付消息转换为支付事件参数
        /// </summary>
        /// <param name="paymentMessage">支付消息对象</param>
        /// <param name="paymentChannel">支付渠道</param>
        /// <returns>支付事件参数对象，转换失败返回null</returns>
        public PaymentEventArgs? ConvertToPaymentEventArgs(PaymentMessage paymentMessage, PaymentChannel paymentChannel)
        {
            try
            {
                if (paymentMessage == null)
                {
                    WeChatListenerConsoleDebug.Write("CONVERT-DROP", "支付消息为空，已丢弃");
                    return null;
                }

                // 解析金额：Fee 通常以“分”为单位，需要转换为“元”。
                decimal amount = 0;
                decimal feeInCents;
                if (!string.IsNullOrWhiteSpace(paymentMessage.Fee) &&
                    decimal.TryParse(paymentMessage.Fee.Trim(), out feeInCents))
                {
                    amount = feeInCents / 100m;
                }

                string invalidReason;
                if (!IsValidPaymentMessage(paymentMessage, amount, out invalidReason))
                {
                    WeChatListenerConsoleDebug.Write("CONVERT-DROP",
                        $"丢弃无效支付消息：{invalidReason}，Status={paymentMessage.Status}, TransId={paymentMessage.TransId}, Fee={paymentMessage.Fee}, Amount={amount}, DisplayName={paymentMessage.DisplayName}, Username={paymentMessage.Username}, Timestamp={paymentMessage.Timestamp}");
                    GlobalSettings.CurrentAppContext.CurrentLogger.Error(
                        $"丢弃无效支付消息：{invalidReason}，渠道:{paymentChannel}，订单:{paymentMessage.TransId}，用户:{paymentMessage.Username}，昵称:{paymentMessage.DisplayName}，金额:{amount}");
                    return null;
                }

                // 解析时间戳：Unix时间戳转换为本地时间。
                DateTime transactionTime = DateTime.Now;
                long timestamp;
                if (!string.IsNullOrWhiteSpace(paymentMessage.Timestamp) &&
                    long.TryParse(paymentMessage.Timestamp.Trim(), out timestamp) &&
                    timestamp > 0)
                {
                    transactionTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
                }

                var userId = !string.IsNullOrWhiteSpace(paymentMessage.Username)
                    ? paymentMessage.Username.Trim()
                    : (paymentMessage.DisplayName == null ? string.Empty : paymentMessage.DisplayName.Trim());

                var orderNumber = BuildOrderNumber(paymentMessage, paymentChannel, transactionTime, amount);
                if (string.IsNullOrWhiteSpace(orderNumber))
                {
                    WeChatListenerConsoleDebug.Write("CONVERT-DROP",
                        $"订单号为空，已丢弃：Status={paymentMessage.Status}, TransId={paymentMessage.TransId}, Fee={paymentMessage.Fee}, Amount={amount}, DisplayName={paymentMessage.DisplayName}, Username={paymentMessage.Username}");
                    return null;
                }

                // 构建支付事件参数。
                return new PaymentEventArgs
                {
                    UserId = userId,
                    DisplayName = paymentMessage.DisplayName,
                    Amount = amount,
                    OrderNumber = orderNumber,
                    PaymentChannel = paymentChannel,
                    Remarks = paymentMessage.DisplayName,
                    PayTime = transactionTime,
                    Status = paymentMessage.Status
                };
            }
            catch (Exception ex)
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Error($"创建支付事件参数失败: {ex.Message}");
                WeChatListenerConsoleDebug.WriteException("CONVERT-ERROR", ex,
                    paymentMessage == null ? string.Empty : $"TransId={paymentMessage.TransId}, Fee={paymentMessage.Fee}, DisplayName={paymentMessage.DisplayName}, Username={paymentMessage.Username}");
                return null;
            }
        }

        private static bool IsValidPaymentMessage(PaymentMessage paymentMessage, decimal amount, out string reason)
        {
            var hasOrder = !string.IsNullOrWhiteSpace(paymentMessage.TransId);
            var hasUser = !string.IsNullOrWhiteSpace(paymentMessage.Username) ||
                          !string.IsNullOrWhiteSpace(paymentMessage.DisplayName);

            // 金额不参与空数据判断。
            // 有些扫码/通知阶段金额可能为空或为 0，但只要有真实流水号，或有 wxid/昵称，
            // 就不能因为金额为 0 而丢弃。
            // 真正需要丢弃的是：没有订单号，同时没有 wxid，也没有昵称的空 paymsg。
            if (!hasOrder && !hasUser)
            {
                reason = "订单、wxid、昵称均为空";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private static string BuildOrderNumber(
            PaymentMessage paymentMessage,
            PaymentChannel paymentChannel,
            DateTime transactionTime,
            decimal amount)
        {
            // 有真实流水号时，必须优先使用真实流水号。
            if (!string.IsNullOrWhiteSpace(paymentMessage.TransId))
                return paymentMessage.TransId.Trim();

            // 没有真实流水号时，只生成临时订单号，且必须带 TMP 前缀，避免和真实流水号混淆。
            var channelPrefix = GetChannelPrefix(paymentChannel);

            var userPart = !string.IsNullOrWhiteSpace(paymentMessage.Username)
                ? paymentMessage.Username.Trim()
                : (paymentMessage.DisplayName == null ? string.Empty : paymentMessage.DisplayName.Trim());

            // 没有真实流水号时，必须至少有 wxid 或昵称，才能生成 TMP 单号。
            // 金额不参与这个判断，避免误丢扫码阶段金额为 0 的正常消息。
            if (string.IsNullOrWhiteSpace(userPart))
                return string.Empty;

            var safeUserPart = NormalizeOrderPart(userPart, "UNKNOWN");

            // 优先使用微信消息时间戳，保证同一条消息重复处理时临时订单号尽量一致。
            var timePart = !string.IsNullOrWhiteSpace(paymentMessage.Timestamp)
                ? paymentMessage.Timestamp.Trim()
                : transactionTime.ToString("yyyyMMddHHmmssfff");

            var feePart = !string.IsNullOrWhiteSpace(paymentMessage.Fee)
                ? paymentMessage.Fee.Trim()
                : Math.Round(amount * 100m, 0).ToString("0");

            var hash = BuildShortHash($"{channelPrefix}|{safeUserPart}|{timePart}|{feePart}|{paymentMessage.DisplayName}");
            return $"TMP_{channelPrefix}_SCAN_{safeUserPart}_{timePart}_{feePart}_{hash}";
        }

        private static string GetChannelPrefix(PaymentChannel paymentChannel)
        {
            switch (paymentChannel)
            {
                case PaymentChannel.WeChat:
                    return "WX";
                case PaymentChannel.Alipay:
                    return "ALI";
                default:
                    return paymentChannel.ToString().ToUpperInvariant();
            }
        }

        private static string NormalizeOrderPart(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            var chars = value
                .Trim()
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
                .ToArray();

            var result = new string(chars);
            return result.Length <= 40
                ? result
                : result.Substring(0, 40);
        }

        private static string BuildShortHash(string value)
        {
            unchecked
            {
                var hash = 23;
                foreach (var ch in value)
                    hash = hash * 31 + ch;

                return Math.Abs(hash).ToString("X");
            }
        }
    }
}
