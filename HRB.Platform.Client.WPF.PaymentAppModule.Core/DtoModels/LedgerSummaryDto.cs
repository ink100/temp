namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.DtoModels
{
    public sealed class LedgerSummaryDto
    {
        public decimal TotalRevenue { get; set; }

        public decimal AlipayRevenue { get; set; }

        public decimal WeChatRevenue { get; set; }

        public int AlipayCount { get; set; }

        public int WeChatCount { get; set; }

        public int SuccessCount { get; set; }

        public int CancelCount { get; set; }
    }
}