using LiteDB;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.DboModels
{
    /// <summary>
    /// 用户同意条款记录数据库模型
    /// </summary>
    public class UserAgreementDbo
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [BsonId(autoId: true)]
        public int Id { get; set; }

        /// <summary>
        /// 首次使用时间
        /// </summary>
        public DateTime FirstUseTime { get; set; }

        /// <summary>
        /// 确认时间
        /// </summary>
        public DateTime AgreementTime { get; set; }

        /// <summary>
        /// 是否同意条款
        /// </summary>
        public bool IsAgreed { get; set; }

        /// <summary>
        /// 条款版本号
        /// </summary>
        public string Version { get; set; } = "V1.0";

        /// <summary>
        /// 机器标识(用于识别不同设备)
        /// </summary>
        public string MachineId { get; set; } = string.Empty;

        /// <summary>
        /// 备注信息
        /// </summary>
        public string Remarks { get; set; } = string.Empty;
    }
}
