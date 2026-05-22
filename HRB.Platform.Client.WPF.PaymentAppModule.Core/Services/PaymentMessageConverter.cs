using HRB.Payment.Core.Events;
using HRB.Payment.Core.Models;

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
                // 解析金额：Fee 通常以"分"为单位，需要转换为"元"
                decimal amount = 0;
                if (!string.IsNullOrEmpty(paymentMessage.Fee) && decimal.TryParse(paymentMessage.Fee, out decimal feeInCents))
                {
                    amount = feeInCents / 100m; // 转换为元
                }

                // 解析时间戳：Unix时间戳转换为本地时间
                DateTime transactionTime = DateTime.Now;
                if (!string.IsNullOrEmpty(paymentMessage.Timestamp) && long.TryParse(paymentMessage.Timestamp, out long timestamp))
                {
                    transactionTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
                }

                var userId = !string.IsNullOrWhiteSpace(paymentMessage.Username)
     ? paymentMessage.Username.Trim()
     : paymentMessage.DisplayName?.Trim() ?? string.Empty;

                var orderNumber = BuildOrderNumber(paymentMessage, paymentChannel, transactionTime, amount);

                // 构建支付事件参数
                return new PaymentEventArgs
                {
                    UserId = userId,
                    DisplayName = paymentMessage.DisplayName,
                    Amount = amount,
                    OrderNumber = orderNumber,
                    PaymentChannel = paymentChannel,
                    Remarks = paymentMessage.DisplayName,
                    PayTime = transactionTime,
                    Status = PaymentStatus.Scan
                };
            }
            catch (Exception ex)
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Error($"创建支付事件参数失败: {ex.Message}");
                return null;
            }
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
            var channelPrefix = paymentChannel switch
            {
                PaymentChannel.WeChat => "WX",
                PaymentChannel.Alipay => "ALI",
                _ => paymentChannel.ToString().ToUpperInvariant()
            };

            var userPart = !string.IsNullOrWhiteSpace(paymentMessage.Username)
                ? paymentMessage.Username.Trim()
                : paymentMessage.DisplayName?.Trim();

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

        private static string NormalizeOrderPart(string? value, string fallback)
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

        ///// <summary>
        ///// 获取支付渠道名称
        ///// </summary>
        ///// <param name="channel">支付渠道枚举</param>
        ///// <returns>支付渠道中文名称</returns>
        //private string GetPaymentChannelName(PaymentChannel channel)
        //{
        //    return channel switch
        //    {
        //        PaymentChannel.WeChat => "微信",
        //        PaymentChannel.Alipay => "支付宝",
        //        _ => "未知"
        //    };
        //}
    }
}
