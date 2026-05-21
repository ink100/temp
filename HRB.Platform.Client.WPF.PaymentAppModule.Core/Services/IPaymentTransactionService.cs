using HRB.Payment.Core.Events;
using HRB.Payment.Core.Models;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Abstractions;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.DboModels;
using System.Collections.ObjectModel;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 支付交易服务接口。
    /// 职责：内存交易集合管理、订单状态流转、数据库持久化。
    /// 不涉及语音播报、UI 通知、HTTP 推送等副作用 — 调用方根据返回结果自行决定。
    /// </summary>
    public interface IPaymentTransactionService
    {
        /// <summary>
        /// 当前内存中的交易记录集合，供 UI 绑定。
        /// 默认加载最近 20 条，向下滚动时可继续加载更旧记录。
        /// </summary>
        ObservableCollection<TransactionRecord> Transactions { get; }

        /// <summary>
        /// 从数据库加载今日交易记录到内存集合
        /// </summary>
        Task LoadTodayTransactionsAsync();
        /// <summary>
        /// 加载更旧的交易记录，追加到首页列表底部。
        /// </summary>
        /// <returns>是否成功加载到新记录</returns>
        Task<bool> LoadMoreTransactionsAsync();
        /// <summary>
        /// 处理支付开始（扫码）事件。
        /// 包含：重复订单检测、同用户未支付订单标记静默取消、创建新交易记录、启动订单超时追踪。
        /// </summary>
        /// <param name="args">支付事件参数</param>
        /// <returns>处理结果，调用方据此决定是否播报"上次未支付"语音等</returns>
        Task<PaymentStartedResult> HandlePaymentStartedAsync(PaymentEventArgs args);

        /// <summary>
        /// 处理支付成功事件。
        /// 包含：取消超时追踪、更新交易状态、持久化到数据库。
        /// </summary>
        /// <param name="args">支付事件参数</param>
        /// <returns>处理结果，调用方据此决定是否播报成功语音</returns>
        Task<PaymentCompletedResult> HandlePaymentSuccessAsync(PaymentEventArgs args);

        /// <summary>
        /// 处理支付取消（超时/用户取消）事件。
        /// 包含：取消超时追踪、检测是否静默取消、更新交易状态、持久化到数据库。
        /// </summary>
        /// <param name="args">支付事件参数</param>
        /// <returns>处理结果，IsSilentCancel=true 时调用方应跳过播报</returns>
        Task<PaymentCompletedResult> HandlePaymentCancelledAsync(PaymentEventArgs args);

        /// <summary>
        /// 按条件查找内存集合中的交易记录
        /// </summary>
        TransactionRecord? FindTransaction(Func<TransactionRecord, bool> predicate);
     

    }
}
