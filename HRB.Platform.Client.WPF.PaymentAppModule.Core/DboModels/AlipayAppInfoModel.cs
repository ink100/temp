using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.DboModels
{

    /// <summary>
    /// 支付宝App信息（数据库实体，存储加密数据）
    /// </summary>
    public class AlipayAppInfoModel
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [BsonId]
        public Guid Id { get; set; }

        /// <summary>
        /// 登录账号（明文存储，用作唯一标识）
        /// </summary>
        public string LoginAccount { get; set; }

        /// <summary>
        /// 应用ID（加密存储）
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// 支付宝公钥（加密存储）
        /// </summary>
        public string AlipayPublicKey { get; set; }

        /// <summary>
        /// APP公钥（加密存储）
        /// </summary>
        public string AppPublicKey { get; set; }

        /// <summary>
        /// APP私钥（加密存储）
        /// </summary>
        public string AppPrivateKey { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateDateTime { get; set; }

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdateDateTime { get; set; }
    }
}
