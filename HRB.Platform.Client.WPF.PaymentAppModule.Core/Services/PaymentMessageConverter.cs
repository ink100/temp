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

                var userId = !string.IsNullOrEmpty(paymentMessage.Username)
                    ? paymentMessage.Username
                    : paymentMessage.DisplayName;

                // 构建支付事件参数
                return new PaymentEventArgs
                {
                    UserId = userId,
                    DisplayName = paymentMessage.DisplayName,
                    Amount = amount,
                    OrderNumber = paymentMessage.TransId,
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
