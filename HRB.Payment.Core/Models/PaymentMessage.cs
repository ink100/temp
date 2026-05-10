namespace HRB.Payment.Core.Models
{
    public class PaymentMessage
    {
        public string PayMsgType { get; set; } = string.Empty;
        public string TransId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public string Fee { get; set; } = string.Empty;
        public string FeeType { get; set; } = string.Empty;
        public string HeadImgUrl { get; set; } = string.Empty;
        public string Scene { get; set; } = string.Empty;
        public PaymentStatus Status { get; set; }
    }
}

