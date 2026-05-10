using HRB.Payment.Core.Models;

namespace HRB.Payment.Core.Events
{
    public class PaymentEventArgs
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentChannel PaymentChannel { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;

        public PaymentStatus Status { get; set; } = PaymentStatus.Scan;

        public DateTime PayTime { get; set; }



        public string PaymentChannelDisplay => PaymentChannel switch
        {
            PaymentChannel.Alipay => "支付宝",
            PaymentChannel.WeChat => "微信",
            _ => PaymentChannel.ToString()
        };
    }
}

