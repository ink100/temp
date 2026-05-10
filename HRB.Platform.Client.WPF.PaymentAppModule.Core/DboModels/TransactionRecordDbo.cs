using HRB.Payment.Core.Models;
using LiteDB;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.DboModels
{
    /// <summary>
    /// 交易记录数据库模型（LiteDB）
    /// </summary>
    public class TransactionRecordDbo
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [BsonId(autoId: true)]
        public int Id { get; set; }

        /// <summary>
        /// 交易时间
        /// </summary>
        public DateTime TransactionTime { get; set; }

        /// <summary>
        /// 订单号
        /// </summary>
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// 用户昵称
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 支付渠道（存储为整数）
        /// </summary>
        public PaymentChannel PaymentChannel { get; set; }

        /// <summary>
        /// 金额
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 支付状态（存储为整数）
        /// </summary>
        public PaymentStatus Status { get; set; }

        /// <summary>
        /// 从业务模型转换为数据库模型
        /// </summary>
        public static TransactionRecordDbo FromModel(TransactionRecord model)
        {
            return new TransactionRecordDbo
            {
                Id = model.Id,
                TransactionTime = model.TransactionTime,
                OrderNumber = model.OrderNumber,
                UserId = model.UserId,
                DisplayName = model.DisplayName,
                PaymentChannel = model.PaymentChannel,
                Amount = model.Amount,
                Remarks = model.Remarks,
                CreatedAt = model.CreatedAt,
                Status = model.Status
            };
        }

        /// <summary>
        /// 转换为业务模型
        /// </summary>
        public TransactionRecord ToModel()
        {
            return new TransactionRecord
            {
                Id = this.Id,
                TransactionTime = this.TransactionTime,
                OrderNumber = this.OrderNumber,
                UserId = this.UserId,
                DisplayName = this.DisplayName,
                PaymentChannel = this.PaymentChannel,
                Amount = this.Amount,
                Remarks = this.Remarks,
                CreatedAt = this.CreatedAt,
                Status = this.Status
            };
        }
    }
}
