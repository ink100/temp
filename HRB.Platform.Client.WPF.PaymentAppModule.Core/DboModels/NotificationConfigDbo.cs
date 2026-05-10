using LiteDB;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.DboModels
{
    /// <summary>
    /// 消息通知配置数据库模型
    /// </summary>
    public class NotificationConfigDbo
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [BsonId(autoId: true)]
        public int Id { get; set; }

        /// <summary>
        /// 是否启用通知
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 通知接口URL
        /// </summary>
        public string NotificationUrl { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdatedAt { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks { get; set; } = string.Empty;
    }
}
