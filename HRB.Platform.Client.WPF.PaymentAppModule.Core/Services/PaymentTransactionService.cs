using HRB.Payment.Core.Events;
using HRB.Payment.Core.Helpers;
using HRB.Payment.Core.Models;
using HRB.Platform.Client.Core.Interfaces;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Abstractions;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.DboModels;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Extensions;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Repository;
using System.Collections.ObjectModel;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 支付交易服务。
    /// 负责管理内存中的交易集合、订单状态流转以及数据库持久化。
    /// 语音播报、HTTP 通知等副作用由调用方根据处理结果决定。
    /// </summary>
    public sealed class PaymentTransactionService : IPaymentTransactionService
    {
        private const int MaxTransactionCount = 50;

        private readonly IPaymentRepository _repository;
        private readonly IOrderStateManager _orderStateManager;
        private readonly IHrbLogger _log;

        public ObservableCollection<TransactionRecord> Transactions { get; } = new();

        public PaymentTransactionService(
            IPaymentRepository repository,
            IOrderStateManager orderStateManager)
        {
            _repository = repository;
            _orderStateManager = orderStateManager;
            _log = GlobalSettings.CurrentAppContext.CurrentLogger;
        }

        /// <summary>
        /// 加载今日交易记录到内存集合。
        /// </summary>
        public async Task LoadTodayTransactionsAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var records = await _repository.GetTransactionsByDateRangeAsync(today, tomorrow);

            Transactions.Clear();
            foreach (var dbo in records.OrderByDescending(t => t.TransactionTime).Take(MaxTransactionCount))
            {
                Transactions.Add(dbo.ToModel());
            }
        }

        /// <summary>
        /// 处理支付开始事件。
        /// 返回处理结果，供调用方决定是否播报语音，
        /// 以及是否需要为上一笔未支付订单补发静默取消通知。
        /// </summary>
        public async Task<PaymentStartedResult> HandlePaymentStartedAsync(PaymentEventArgs args)
        {
            var existing = FindTransaction(t => t.OrderNumber == args.OrderNumber);
            if (existing != null)
            {
                if (existing.Status == PaymentStatus.Success || existing.Status == PaymentStatus.Cancel)
                    return new PaymentStartedResult { AlreadyExists = true };

                existing.DisplayName = args.DisplayName;
                existing.Amount = args.Amount;
                return new PaymentStartedResult { AlreadyExists = true };
            }

            bool hasPriorUnpaid = false;
            PaymentEventArgs? priorCancelledPayment = null;

            if (!string.IsNullOrEmpty(args.UserId))
            {
                var currentTime = PcHelper.GetNetNowTime();

                // 只看该用户“最近一笔”记录，不能在历史记录里随便找一条非成功记录。
                // 否则用户中间已经成功付款后，旧的 Scan/Cancel 记录仍会导致误报“上次未付款”。
                var lastOrder = Transactions
                    .Where(t => t.UserId == args.UserId && t.OrderNumber != args.OrderNumber)
                    .OrderByDescending(GetTransactionSortTime)
                    .FirstOrDefault();

                if (lastOrder != null)
                {
                    hasPriorUnpaid = IsPriorUnpaid(lastOrder, currentTime);

                    // 只有上一笔仍处于支付中时，才需要静默取消并补发取消通知。
                    // 已成功的最近记录绝不触发上次未支付提醒。
                    if (lastOrder.Status == PaymentStatus.Scan)
                    {
                        _orderStateManager.MarkSilentCancel(lastOrder.OrderNumber);
                        _orderStateManager.UntrackOrder(lastOrder.OrderNumber);

                        lastOrder.Status = PaymentStatus.Cancel;
                        lastOrder.TransactionTime = currentTime;

                        priorCancelledPayment = BuildPaymentEventArgs(lastOrder, currentTime, PaymentStatus.Cancel);
                        await SaveOrUpdateAsync(priorCancelledPayment, PaymentStatus.Cancel);
                    }
                }
                else
                {
                    // 查询数据库，处理上一笔订单已经不在内存中的情况。
                    // 数据库方法返回的是该用户最新一条订单；如果最新一条是 Success，则不提醒。
                    var last = await _repository.GetOrderLastOrderByUserIdAsync(args.UserId);

                    if (last != null && IsPriorUnpaid(last, currentTime))
                        hasPriorUnpaid = true;
                }
            }

            var transaction = new TransactionRecord
            {
                TransactionTime = args.PayTime,
                OrderNumber = args.OrderNumber,
                UserId = args.UserId,
                DisplayName = args.DisplayName,
                PaymentChannel = args.PaymentChannel,
                Amount = args.Amount,
                Remarks = args.Remarks,
                CreatedAt = DateTime.Now,
                Status = PaymentStatus.Scan
            };

            AddTransaction(transaction);

            // 扫码开始也立即入库。这样程序重启/崩溃后不会丢失 Scan 记录，
            // 后续支付成功、取消、超时取消再按订单号更新同一条记录。
            await SaveOrUpdateAsync(args, PaymentStatus.Scan);

            _orderStateManager.TrackOrder(args.OrderNumber);

            return new PaymentStartedResult
            {
                AlreadyExists = false,
                HasPriorUnpaid = hasPriorUnpaid,
                PriorCancelledPayment = priorCancelledPayment,
                Transaction = transaction
            };
        }

        /// <summary>
        /// 处理支付成功事件。
        /// </summary>
        public async Task<PaymentCompletedResult> HandlePaymentSuccessAsync(PaymentEventArgs args)
        {
            _orderStateManager.UntrackOrder(args.OrderNumber);

            var transaction = FindTransaction(t => t.OrderNumber == args.OrderNumber);
            if (transaction != null)
            {
                if (transaction.Status == PaymentStatus.Success)
                    return new PaymentCompletedResult { StateChanged = false, Transaction = transaction };

                transaction.Status = PaymentStatus.Success;
                transaction.TransactionTime = args.PayTime;
                transaction.DisplayName = args.DisplayName;
                transaction.Amount = args.Amount;
            }
            else
            {
                transaction = new TransactionRecord
                {
                    TransactionTime = args.PayTime,
                    OrderNumber = args.OrderNumber,
                    UserId = args.UserId,
                    DisplayName = args.DisplayName,
                    PaymentChannel = args.PaymentChannel,
                    Amount = args.Amount,
                    Remarks = args.Remarks,
                    CreatedAt = DateTime.Now,
                    Status = PaymentStatus.Success
                };
                AddTransaction(transaction);
            }

            await SaveOrUpdateAsync(args, PaymentStatus.Success);

            return new PaymentCompletedResult { StateChanged = true, Transaction = transaction };
        }

        /// <summary>
        /// 处理支付取消事件。
        /// </summary>
        public async Task<PaymentCompletedResult> HandlePaymentCancelledAsync(PaymentEventArgs args)
        {
            _orderStateManager.UntrackOrder(args.OrderNumber);

            bool isSilent = _orderStateManager.IsSilentCancel(args.OrderNumber);
            if (isSilent)
                _orderStateManager.ClearSilentCancel(args.OrderNumber);

            var transaction = FindTransaction(t => t.OrderNumber == args.OrderNumber);
            if (transaction != null)
            {
                if (transaction.Status == PaymentStatus.Cancel)
                    return new PaymentCompletedResult { StateChanged = false, IsSilentCancel = isSilent, Transaction = transaction };

                transaction.Status = PaymentStatus.Cancel;
                transaction.TransactionTime = args.PayTime;
                transaction.DisplayName = args.DisplayName;
                transaction.Amount = args.Amount;
            }
            else
            {
                transaction = new TransactionRecord
                {
                    TransactionTime = args.PayTime,
                    OrderNumber = args.OrderNumber,
                    UserId = args.UserId,
                    DisplayName = args.DisplayName,
                    PaymentChannel = args.PaymentChannel,
                    Amount = args.Amount,
                    Remarks = args.Remarks,
                    CreatedAt = DateTime.Now,
                    Status = PaymentStatus.Cancel
                };
                AddTransaction(transaction);
            }

            await SaveOrUpdateAsync(args, PaymentStatus.Cancel);

            return new PaymentCompletedResult { StateChanged = true, IsSilentCancel = isSilent, Transaction = transaction };
        }

        public TransactionRecord? FindTransaction(Func<TransactionRecord, bool> predicate)
        {
            return Transactions.FirstOrDefault(predicate);
        }

        private void AddTransaction(TransactionRecord transaction)
        {
            Transactions.Insert(0, transaction);
            while (Transactions.Count > MaxTransactionCount)
                Transactions.RemoveAt(Transactions.Count - 1);
        }

        private async Task SaveOrUpdateAsync(PaymentEventArgs args, PaymentStatus status)
        {
            try
            {
                var existing = await _repository.GetTransactionByOrderAsync(args.OrderNumber);
                if (existing != null)
                {
                    existing.Status = status;
                    existing.TransactionTime = args.PayTime;
                    existing.UserId = args.UserId;
                    existing.DisplayName = args.DisplayName;
                    existing.PaymentChannel = args.PaymentChannel;
                    existing.Amount = args.Amount;
                    existing.Remarks = args.Remarks;
                    await _repository.UpdateTransactionAsync(existing);
                }
                else
                {
                    var dbo = new TransactionRecordDbo
                    {
                        TransactionTime = args.PayTime,
                        OrderNumber = args.OrderNumber,
                        UserId = args.UserId,
                        DisplayName = args.DisplayName,
                        PaymentChannel = args.PaymentChannel,
                        Amount = args.Amount,
                        Remarks = args.Remarks,
                        CreatedAt = DateTime.Now,
                        Status = status
                    };
                    await _repository.AddTransactionAsync(dbo);
                }
            }
            catch (Exception ex)
            {
                _log.Error($"保存交易记录失败: {ex.Message}");
            }
        }

        private static DateTime GetTransactionSortTime(TransactionRecord transaction)
        {
            return transaction.TransactionTime == default ? transaction.CreatedAt : transaction.TransactionTime;
        }

        private static bool IsPriorUnpaid(TransactionRecord transaction, DateTime currentTime)
        {
            if (transaction.Status == PaymentStatus.Success)
                return false;

            var baseTime = transaction.Status == PaymentStatus.Scan
                ? transaction.CreatedAt
                : GetTransactionSortTime(transaction);

            return (currentTime - baseTime).TotalSeconds >= 120;
        }

        private static bool IsPriorUnpaid(TransactionRecordDbo transaction, DateTime currentTime)
        {
            if (transaction.Status == PaymentStatus.Success)
                return false;

            return (currentTime - transaction.TransactionTime).TotalSeconds >= 120;
        }

        private static PaymentEventArgs BuildPaymentEventArgs(
            TransactionRecord transaction,
            DateTime payTime,
            PaymentStatus status)
        {
            return new PaymentEventArgs
            {
                OrderNumber = transaction.OrderNumber,
                UserId = transaction.UserId,
                DisplayName = transaction.DisplayName,
                Amount = transaction.Amount,
                PaymentChannel = transaction.PaymentChannel,
                Remarks = transaction.Remarks,
                PayTime = payTime,
                Status = status
            };
        }
    }
}
