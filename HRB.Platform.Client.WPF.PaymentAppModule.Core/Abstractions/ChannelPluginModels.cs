using HRB.Payment.Core.Events;
using HRB.Payment.Core.Models;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Abstractions
{
    /// <summary>
    /// 渠道状态变化事件参数
    /// </summary>
    public sealed class ChannelStatusChangedEventArgs : EventArgs
    {
        public required string ChannelId { get; init; }
        public required ChannelStatus Status { get; init; }
        public string? Message { get; init; }
    }

    /// <summary>
    /// 渠道运行状态
    /// </summary>
    public enum ChannelStatus
    {
        /// <summary>插件进程未就绪</summary>
        Unavailable,
        /// <summary>插件进程就绪，业务未启用</summary>
        Available,
        /// <summary>业务启用中（正在配置/连接）</summary>
        Enabling,
        /// <summary>业务已启用，正常工作</summary>
        Running,
        /// <summary>业务已禁用</summary>
        Disabled,
        /// <summary>出错</summary>
        Faulted
    }

    /// <summary>
    /// 渠道设置项定义
    /// </summary>
    public sealed class ChannelSettingItem
    {
        public required string Key { get; init; }
        public required string Label { get; init; }
        public required SettingType Type { get; init; }
        public object? CurrentValue { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string? DisabledHint { get; set; }
        public int Order { get; init; }

        /// <summary>
        /// Toggle 类型的便捷布尔访问（供 XAML DataTrigger 绑定，避免 object? 装箱问题）
        /// </summary>
        public bool IsToggleOn => Type == SettingType.Toggle && CurrentValue is true;
    }

    /// <summary>
    /// 设置项类型
    /// </summary>
    public enum SettingType
    {
        Toggle,
        Text,
        Select
    }

    /// <summary>
    /// 支付开始处理结果 — 供 ViewModel 决定语音/通知行为
    /// </summary>
    public sealed class PaymentStartedResult
    {
        /// <summary>订单已存在（重复推送），无需新处理</summary>
        public bool AlreadyExists { get; init; }

        /// <summary>该用户有上一笔未支付订单</summary>
        public bool HasPriorUnpaid { get; init; }

        /// <summary>因重复扫码被静默取消的上一笔订单，调用方可用于补发通知</summary>
        public PaymentEventArgs? PriorCancelledPayment { get; init; }

        /// <summary>新创建的交易记录（AlreadyExists=true 时为 null）</summary>
        public TransactionRecord? Transaction { get; init; }
    }

    /// <summary>
    /// 支付完成处理结果 — 供 ViewModel 决定语音/通知行为
    /// </summary>
    public sealed class PaymentCompletedResult
    {
        /// <summary>是否为静默取消（超时/重复扫码），不播报</summary>
        public bool IsSilentCancel { get; init; }

        /// <summary>状态是否实际发生了变更</summary>
        public bool StateChanged { get; init; }

        public TransactionRecord? Transaction { get; init; }
    }
}
