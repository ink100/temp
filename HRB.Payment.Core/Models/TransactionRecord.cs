namespace HRB.Payment.Core.Models
{
    public class TransactionRecord : BindableBase
    {
        private DateTime _transactionTime;
        private PaymentStatus _status = PaymentStatus.Scan;
        private decimal amount;

        public int Id { get; set; }

        public DateTime TransactionTime
        {
            get => _transactionTime;
            set
            {
                if (SetProperty(ref _transactionTime, value))
                {
                    RaisePropertyChanged(nameof(Time));
                    RaisePropertyChanged(nameof(TimeOnly));
                }
            }
        }

        public string OrderNumber { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public PaymentChannel PaymentChannel { get; set; }
        public decimal Amount
        {
            get => amount;
            set
            {
                SetProperty(ref amount, value);

                RaisePropertyChanged(nameof(AmountDisplay));

            }
        }
        public string Remarks { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 支付状态
        /// </summary>
        public PaymentStatus Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    RaisePropertyChanged(nameof(StatusDisplay));
                }
            }
        }

        // 用于显示的格式化属性
        public string Time => TransactionTime.ToString("yyyy-MM-dd HH:mm:ss");
        public string TimeOnly => TransactionTime.ToString("HH:mm:ss");
        public string AmountDisplay => $"¥{Amount:F2}";
        public string PaymentChannelDisplay => PaymentChannel switch
        {
            PaymentChannel.Alipay => "支付宝",
            PaymentChannel.WeChat => "微信",
            _ => PaymentChannel.ToString()
        };

        /// <summary>
        /// 台账界面使用的渠道唯一标识。
        /// 支付宝显示流水单号/订单号；微信显示 wxid。
        /// </summary>
        public string LedgerIdentifierDisplay => PaymentChannel switch
        {
            PaymentChannel.Alipay => OrderNumber,
            PaymentChannel.WeChat => UserId,
            _ => OrderNumber
        };

        /// <summary>
        /// 状态显示文本
        /// </summary>
        public string StatusDisplay => Status switch
        {
            PaymentStatus.Scan => "支付中...",
            PaymentStatus.Success => "支付成功",
            PaymentStatus.Cancel => "取消支付",
            _ => Status.ToString()
        };
    }
}

