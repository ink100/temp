using HRB.Payment.Core.Events;
using HRB.Payment.Core.Models;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Helpers;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Repository;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// HTTP通知服务接口
    /// </summary>
    public interface IHttpNotificationService
    {
        /// <summary>
        /// 发送支付事件通知
        /// </summary>
        /// <param name="paymentEvent">支付事件参数</param>
        /// <returns>是否发送成功</returns>
        Task<bool> SendPaymentNotificationAsync(PaymentEventArgs paymentEvent);
    }

    /// <summary>
    /// HTTP通知服务实现。
    /// 优化点：去掉嵌套 Task.Run，增加地址校验、超时、轻量重试和响应码日志，避免通知失败时无感知。
    /// </summary>
    public class HttpNotificationService(IPaymentRepository repository) : IHttpNotificationService
    {
        private const int MaxRetryCount = 2;
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

        private static readonly HttpClient HttpClient = new()
        {
            Timeout = RequestTimeout
        };

        private readonly IPaymentRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        /// <summary>
        /// 发送支付事件通知。
        /// 调用方仍可使用 fire-and-forget；服务内部会真实执行请求并记录失败原因。
        /// </summary>
        public async Task<bool> SendPaymentNotificationAsync(PaymentEventArgs paymentEvent)
        {
            try
            {
                var config = await _repository.GetNotificationConfigAsync();
                if (config == null || !config.IsEnabled || string.IsNullOrWhiteSpace(config.NotificationUrl))
                {
                    return false;
                }

                if (!Uri.TryCreate(config.NotificationUrl.Trim(), UriKind.Absolute, out var notificationUri)
                    || (notificationUri.Scheme != Uri.UriSchemeHttp && notificationUri.Scheme != Uri.UriSchemeHttps))
                {
                    GlobalSettings.CurrentAppContext.CurrentLogger.Error($"支付通知地址无效: {config.NotificationUrl}");
                    return false;
                }

                var notificationData = BuildMaskedNotificationData(paymentEvent);
                var jsonContent = JsonConvert.SerializeObject(notificationData, Formatting.None);

                for (var attempt = 1; attempt <= MaxRetryCount; attempt++)
                {
                    try
                    {
                        using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                        using var response = await HttpClient.PostAsync(notificationUri, content);

                        if (response.IsSuccessStatusCode)
                        {
                            GlobalSettings.CurrentAppContext.CurrentLogger.Info($"支付通知发送成功: {(int)response.StatusCode}");
                            return true;
                        }

                        GlobalSettings.CurrentAppContext.CurrentLogger.Error($"支付通知发送失败: HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
                    }
                    catch (TaskCanceledException ex)
                    {
                        GlobalSettings.CurrentAppContext.CurrentLogger.Error($"支付通知发送超时，第 {attempt} 次: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        GlobalSettings.CurrentAppContext.CurrentLogger.Error($"支付通知发送异常，第 {attempt} 次: {ex.Message}");
                    }

                    if (attempt < MaxRetryCount)
                    {
                        await Task.Delay(300 * attempt);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Error($"发送支付通知失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 构建脱敏后的通知数据
        /// </summary>
        private static object BuildMaskedNotificationData(PaymentEventArgs paymentEvent)
        {
            return new
            {
                EventType = GetEventTypeName(paymentEvent.Status),
                PaymentChannel = paymentEvent.PaymentChannelDisplay,
                paymentEvent.Amount,
                OrderNumber = DataMaskingHelper.MaskOrderNumber(paymentEvent.OrderNumber),
                UserNickname = DataMaskingHelper.MaskNickname(paymentEvent.DisplayName),
                UserId = DataMaskingHelper.MaskUserId(paymentEvent.UserId),
                PaymentTime = paymentEvent.PayTime.ToString("yyyy-MM-dd HH:mm:ss"),
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        /// <summary>
        /// 获取事件类型名称
        /// </summary>
        private static string GetEventTypeName(PaymentStatus status)
        {
            return status switch
            {
                PaymentStatus.Scan => "用户正在支付",
                PaymentStatus.Success => "用户支付成功",
                PaymentStatus.Cancel => "用户取消支付",
                _ => "未知事件"
            };
        }
    }
}
