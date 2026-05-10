using System;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.DboModels
{
    /// <summary>
    /// 插件日志实体
    /// </summary>
    public class PluginLogModel
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 插件名称
        /// </summary>
        public string PluginName { get; set; }

        /// <summary>
        /// 操作类型（Start, Stop, Error, Info）
        /// </summary>
        public string OperationType { get; set; }

        /// <summary>
        /// 日志消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 进程ID
        /// </summary>
        public int? ProcessId { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 异常信息
        /// </summary>
        public string ExceptionMessage { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateDateTime { get; set; }
    }
}
