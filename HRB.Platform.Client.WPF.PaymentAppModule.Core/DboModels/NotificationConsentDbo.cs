using LiteDB;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.DboModels
{
    /// <summary>
    /// 消息通知知情同意书记录数据库模型
    /// </summary>
    public class NotificationConsentDbo
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [BsonId(autoId: true)]
        public int Id { get; set; }

        /// <summary>
        /// 同意时间
        /// </summary>
        public DateTime ConsentTime { get; set; }

        /// <summary>
        /// 是否同意
        /// </summary>
        public bool IsAgreed { get; set; }

        /// <summary>
        /// 机器标识
        /// </summary>
        public string MachineId { get; set; } = string.Empty;

        /// <summary>
        /// 备注信息
        /// </summary>
        public string Remarks { get; set; } = string.Empty;
    }
}
