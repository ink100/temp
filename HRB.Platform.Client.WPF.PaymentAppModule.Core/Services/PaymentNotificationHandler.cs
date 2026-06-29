using HRB.Payment.Core.Events;
using HRB.Payment.Core.Models;
using HRB.Payment.Core.Services;
using HRB.Payment.Message.Core;
using HRB.Payment.Message.Core.BusEvents;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.DtoModels;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Helpers;
using Newtonsoft.Json;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 支付通知处理服务实现
    /// 负责协调支付通知的完整处理流程：解析 → 转换 → 事件发布
    /// </summary>
    public class PaymentNotificationHandler : IPaymentNotificationHandler
    {
        private readonly IWeChatMessageParser _messageParser;
        private readonly IPaymentMessageConverter _messageConverter;
        private readonly IPaymentEventPublisher _eventPublisher;


        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="messageParser">微信消息解析服务</param>
        /// <param name="messageConverter">支付消息转换服务</param>
        /// <param name="eventPublisher">支付事件发布服务</param>
        public PaymentNotificationHandler(
            IWeChatMessageParser messageParser,
            IPaymentMessageConverter messageConverter,
            IPaymentEventPublisher eventPublisher
      
            )
        {
            _messageParser = messageParser ?? throw new ArgumentNullException(nameof(messageParser));
            _messageConverter = messageConverter ?? throw new ArgumentNullException(nameof(messageConverter));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));

        }

        /// <summary>
        /// 处理支付通知消息
        /// </summary>
        /// <param name="notification">支付通知消息对象</param>
        /// <returns>处理任务</returns>
        public async Task HandleNotificationAsync(NotificationPayMessageEto notification)
        {
            WeChatListenerConsoleDebug.Write("RECV",
                $"收到支付通知：PayMessageType={notification.PayMessageType}, PayMessageLength={notification.PayMessage?.Length ?? 0}");

            // 根据支付类型处理不同的消息
            if (notification.PayMessageType == PayMessageTypeEnum.AP) // 支付宝
            {
                await HandleAlipayNotificationAsync(notification);

            }
            else if (notification.PayMessageType == PayMessageTypeEnum.VX) // 微信
            {
                await HandleWeChatNotificationAsync(notification);
            }

            //  return Task.CompletedTask;
        }

        /// <summary>
        /// 处理微信支付通知
        /// </summary>
        /// <param name="notification">支付通知消息对象</param>
        /// <returns>处理任务</returns>
        private Task HandleWeChatNotificationAsync(NotificationPayMessageEto notification)
        {
            // 1. 反序列化微信消息
            WeChatListenerConsoleDebug.Write("WX-RECV", $"收到 VX PayMessage，Length={notification.PayMessage?.Length ?? 0}");
            WeChatListenerConsoleDebug.WriteRaw("WX-RAW", "PayMessage摘要", notification.PayMessage);
            WeChatListenerConsoleDebug.WriteBlock("WX-PAYMESSAGE", "PayMessage原始完整内容", notification.PayMessage);

            WeChatMessageResult? result;
            try
            {
                result = JsonConvert.DeserializeObject<WeChatMessageResult>(notification.PayMessage);
            }
            catch (Exception ex)
            {
                WeChatListenerConsoleDebug.WriteException("JSON-ERROR", ex, notification.PayMessage);
                return Task.CompletedTask;
            }

            if (result == null)
            {
                WeChatListenerConsoleDebug.Write("JSON-NULL", "微信 PayMessage 反序列化结果为空");
                return Task.CompletedTask;
            }

            WeChatListenerConsoleDebug.Write("JSON-OK",
                $"MessageType={result.MessageType}, MessageLength={result.Message?.Length ?? 0}");
            WeChatListenerConsoleDebug.WriteBlock("WX-MESSAGE", "WeChatMessageResult.Message原始XML", result.Message);

            // 2. 解析支付消息XML
            var payMsg = _messageParser.ParsePaymentMessage(result.Message);
            if (payMsg == null)
            {
                WeChatListenerConsoleDebug.WriteRaw("XML-FAIL", "微信 XML 解析失败，Message", result.Message);
                return Task.CompletedTask;
            }

            // 3. 转换为支付事件参数
            var paymentArgs = _messageConverter.ConvertToPaymentEventArgs(payMsg, PaymentChannel.WeChat);
            if (paymentArgs == null)
            {
                WeChatListenerConsoleDebug.Write("CONVERT-FAIL",
                    $"微信 PaymentEventArgs 转换失败/已丢弃：Status={payMsg.Status}, TransId={payMsg.TransId}, Fee={payMsg.Fee}, DisplayName={payMsg.DisplayName}, Username={payMsg.Username}, Timestamp={payMsg.Timestamp}");
                return Task.CompletedTask;
            }

            WeChatListenerConsoleDebug.Write("CONVERT-OK",
                $"Status={payMsg.Status}, Order={paymentArgs.OrderNumber}, Amount={paymentArgs.Amount}, DisplayName={paymentArgs.DisplayName}");

            // 4. 根据支付状态发布对应的事件
            switch (payMsg.Status)
            {
                case PaymentStatus.Scan:
                    // 支付扫描/开始
                    _eventPublisher.PublishPaymentStarted(paymentArgs);
                    WeChatListenerConsoleDebug.Write("PUBLISH", $"PaymentStartedEvent Order={paymentArgs.OrderNumber}, Amount={paymentArgs.Amount}");
                    
                    break;

                case PaymentStatus.Success:
                    // 支付成功
                    _eventPublisher.PublishPaymentSuccess(paymentArgs);
                    WeChatListenerConsoleDebug.Write("PUBLISH", $"PaymentSuccessEvent Order={paymentArgs.OrderNumber}, Amount={paymentArgs.Amount}");
                   
                    break;

                case PaymentStatus.Cancel:
                    // 支付取消
                    _eventPublisher.PublishPaymentCancelled(paymentArgs);
                    WeChatListenerConsoleDebug.Write("PUBLISH", $"PaymentCancelledEvent Order={paymentArgs.OrderNumber}, Amount={paymentArgs.Amount}");
                   
                    break;
            }

            return Task.CompletedTask;
        }


        /// <summary>
        ///  处理支付宝支付通知
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        private Task HandleAlipayNotificationAsync(NotificationPayMessageEto notification)
        {
            var am = JsonConvert.DeserializeObject<AlipayNotificationMessage>(notification.PayMessage);

            if (am == null)
                return Task.CompletedTask;

            var result = JsonConvert.DeserializeObject<PaymentEventArgs>(am.Message);


            if (result != null)
            {
                // 4. 根据支付状态发布对应的事件
                switch (result.Status)
                {
                    case PaymentStatus.Scan:
                        // 支付扫描/开始
                        _eventPublisher.PublishPaymentStarted(result);
             
                        break;

                    case PaymentStatus.Success:
                        // 支付成功
                        _eventPublisher.PublishPaymentSuccess(result);
                        
                        break;

                    case PaymentStatus.Cancel:
                        // 支付取消
                        _eventPublisher.PublishPaymentCancelled(result);
                    
                        break;
                }

            }

            return Task.CompletedTask;
        }

    }
}
