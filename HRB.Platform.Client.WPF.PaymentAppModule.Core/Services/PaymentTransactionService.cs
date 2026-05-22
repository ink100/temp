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
        private const int TransactionPageSize = 20;
        // 首页最多缓存 100 条，避免用户一直滚动导致内存集合无限增长。
        // DataGrid 已开启虚拟化，100 条在低配机器上仍然可控。
        private const int MaxCachedTransactionCount = 100;
        private readonly IPaymentRepository _repository;
        private readonly IOrderStateManager _orderStateManager;
        private readonly IHrbLogger _log;
        private bool _isLoadingMoreTransactions;
        private bool _hasMoreTransactions = true;
        public ObservableCollection<TransactionRecord> Transactions { get; } = new();

        public PaymentTransactionService(
            IPaymentRepository repository,
            IOrderStateManager orderStateManager)
        {
            _repository = repository;
            _orderStateManager = orderStateManager;
            _log = GlobalSettings.CurrentAppContext.CurrentLogger;
        }

        private static DateTime GetEffectiveTransactionTime(TransactionRecord transaction)
        {
            return transaction.TransactionTime == default
                ? transaction.CreatedAt
                : transaction.TransactionTime;
        }

        private static DateTime GetEffectiveTransactionTime(TransactionRecordDbo transaction)
        {
            return transaction.TransactionTime == default
                ? transaction.CreatedAt
                : transaction.TransactionTime;
        }
        /// <summary>
        /// 加载首页交易记录。
        /// 当天记录优先；
        /// 如果当天记录不足 50 条，则使用最近历史记录补足到 50 条。
        /// 同时临时弹窗显示加载诊断信息，便于排查页面不显示问题。
        /// </summary>
        public async Task LoadTodayTransactionsAsync()
        {
            _hasMoreTransactions = true;

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var todayRecords = await _repository.GetRecentTransactionsByDateRangeAsync(
                today,
                tomorrow,
                TransactionPageSize);

            var mergedRecords = todayRecords?.ToList() ?? new List<TransactionRecordDbo>();

            if (mergedRecords.Count < TransactionPageSize)
            {
                var recentRecords = await _repository.GetRecentTransactionsAsync(TransactionPageSize);

                mergedRecords = mergedRecords
                    .Concat(recentRecords ?? new List<TransactionRecordDbo>())
                    .GroupBy(t => t.Id)
                    .Select(g => g.First())
                    .ToList();
            }

            var finalRecords = mergedRecords
                .OrderByDescending(t => t.TransactionTime == default ? t.CreatedAt : t.TransactionTime)
                .ThenByDescending(t => t.Id)
                .Take(TransactionPageSize)
                .ToList();

            Transactions.Clear();
            foreach (var dbo in finalRecords)
            {
                Transactions.Add(dbo.ToModel());
            }

            if (finalRecords.Count < TransactionPageSize)
                _hasMoreTransactions = false;
        }
        /// <summary>
        /// 处理支付开始事件。
        /// 返回处理结果，供调用方决定是否播报语音，
        /// 以及是否需要为上一笔未支付订单补发静默取消通知。
        /// </summary>
        public async Task<PaymentStartedResult> HandlePaymentStartedAsync(PaymentEventArgs args)
        {
            if (args == null || string.IsNullOrWhiteSpace(args.OrderNumber))
            {
                _log.Error(
                    $"忽略订单号为空的支付开始事件，渠道:{args?.PaymentChannel}，用户:{args?.UserId}，昵称:{args?.DisplayName}");

                return new PaymentStartedResult { AlreadyExists = true };
            }
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

            if (!string.IsNullOrWhiteSpace(args.UserId))
            {
                var currentTime = PcHelper.GetNetNowTime();

                // “上次未支付”判断统一以数据库为准，
                // 并且必须区分支付渠道，避免微信与支付宝记录互相影响。
                //
                // 注意：
                // 本次扫码订单此时还没有入库，
                // 因此这里查到的一定是“历史上一笔订单”，不会误拿到当前订单。
                var lastOrder = await _repository.GetOrderLastOrderByUserIdAndChannelAsync(
                    args.UserId,
                    args.PaymentChannel);

                if (lastOrder != null && lastOrder.OrderNumber != args.OrderNumber)
                {
                    // 最近一笔订单如果是 Success，
                    // IsPriorUnpaid 会直接返回 false，
                    // 因此不会误播“上次未支付”。
                    hasPriorUnpaid = IsPriorUnpaid(lastOrder, currentTime);

                    // 如果同一用户、同一渠道的上一笔订单仍处于 Scan，
                    // 说明该订单还在等待支付。
                    // 此时用户又重新扫码，静默取消旧 Scan，只保留新订单继续处理。
                    if (lastOrder.Status == PaymentStatus.Scan)
                    {
                        _orderStateManager.MarkSilentCancel(lastOrder.OrderNumber);
                        _orderStateManager.UntrackOrder(lastOrder.OrderNumber);

                        lastOrder.Status = PaymentStatus.Cancel;
                        lastOrder.TransactionTime = currentTime;

                        await _repository.UpdateTransactionAsync(lastOrder);

                        priorCancelledPayment = new PaymentEventArgs
                        {
                            OrderNumber = lastOrder.OrderNumber,
                            UserId = lastOrder.UserId,
                            DisplayName = lastOrder.DisplayName,
                            Amount = lastOrder.Amount,
                            PaymentChannel = lastOrder.PaymentChannel,
                            Remarks = lastOrder.Remarks,
                            PayTime = currentTime,
                            Status = PaymentStatus.Cancel
                        };

                        // 同步刷新首页内存列表中的对应记录，
                        // 这只是为了 UI 立即显示为取消，
                        // 不再参与“上次未支付”的业务判断。
                        var memoryOrder = FindTransaction(t =>
                            t.OrderNumber == lastOrder.OrderNumber);

                        if (memoryOrder != null)
                        {
                            memoryOrder.Status = PaymentStatus.Cancel;
                            memoryOrder.TransactionTime = currentTime;
                        }
                    }
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
            if (args == null || string.IsNullOrWhiteSpace(args.OrderNumber))
            {
                _log.Error(
                    $"忽略订单号为空的支付成功事件，渠道:{args?.PaymentChannel}，用户:{args?.UserId}，昵称:{args?.DisplayName}");

                return new PaymentCompletedResult { StateChanged = false };
            }
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
            if (args == null || string.IsNullOrWhiteSpace(args.OrderNumber))
            {
                _log.Error(
                    $"忽略订单号为空的支付取消事件，渠道:{args?.PaymentChannel}，用户:{args?.UserId}，昵称:{args?.DisplayName}");

                return new PaymentCompletedResult { StateChanged = false };
            }
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

            TrimTransactionCache();
        }

        private void TrimTransactionCache()
        {
            while (Transactions.Count > MaxCachedTransactionCount)
                Transactions.RemoveAt(Transactions.Count - 1);
        }

        private async Task SaveOrUpdateAsync(PaymentEventArgs args, PaymentStatus status)
        {
            if (args == null || string.IsNullOrWhiteSpace(args.OrderNumber))
            {
                _log.Error(
                    $"跳过保存订单号为空的交易记录，状态:{status}，渠道:{args?.PaymentChannel}，用户:{args?.UserId}，昵称:{args?.DisplayName}");

                return;
            }
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

        private int GetScanTimeoutSeconds()
        {
            var settings = GlobalSettings.CurrentAppContext.CurrentSettings;
            var seconds = settings.ScanTimeoutSeconds <= 0 ? 10 : settings.ScanTimeoutSeconds;
            return Math.Clamp(seconds, 1, 3600);
        }

        private bool IsPriorUnpaid(TransactionRecord transaction, DateTime currentTime)
        {
            if (transaction.Status == PaymentStatus.Success)
                return false;

            var baseTime = transaction.Status == PaymentStatus.Scan
                ? transaction.CreatedAt
                : GetTransactionSortTime(transaction);

            return (currentTime - baseTime).TotalSeconds >= GetScanTimeoutSeconds();
        }

        private bool IsPriorUnpaid(TransactionRecordDbo transaction, DateTime currentTime)
        {
            if (transaction.Status == PaymentStatus.Success)
                return false;

            var baseTime = transaction.TransactionTime == default
                ? transaction.CreatedAt
                : transaction.TransactionTime;

            return (currentTime - baseTime).TotalSeconds >= GetScanTimeoutSeconds();
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

        public async Task<bool> LoadMoreTransactionsAsync()
        {
            if (_isLoadingMoreTransactions || !_hasMoreTransactions)
                return false;

            if (Transactions.Count == 0)
                return false;

            _isLoadingMoreTransactions = true;

            try
            {
                var lastTransaction = Transactions
                    .OrderBy(GetEffectiveTransactionTime)
                    .ThenBy(t => t.Id)
                    .FirstOrDefault();

                if (lastTransaction == null)
                    return false;

                var beforeTime = GetEffectiveTransactionTime(lastTransaction);
                var beforeId = lastTransaction.Id;

                var records = await _repository.GetRecentTransactionsBeforeAsync(
                    beforeTime,
                    beforeId,
                    TransactionPageSize);

                if (records == null || records.Count == 0)
                {
                    _hasMoreTransactions = false;
                    return false;
                }

                var existingIds = Transactions
                    .Select(t => t.Id)
                    .ToHashSet();

                var newRecords = records
                    .Where(t => !existingIds.Contains(t.Id))
                    .OrderByDescending(GetEffectiveTransactionTime)
                    .ThenByDescending(t => t.Id)
                    .Take(TransactionPageSize)
                    .ToList();

                if (newRecords.Count == 0)
                {
                    if (records.Count < TransactionPageSize)
                        _hasMoreTransactions = false;

                    return false;
                }

                foreach (var dbo in newRecords)
                {
                    Transactions.Add(dbo.ToModel());
                }

                TrimTransactionCache();

                if (records.Count < TransactionPageSize)
                    _hasMoreTransactions = false;

                return true;
            }
            catch (Exception ex)
            {
                _log.Info($"加载更多交易记录失败: {ex.Message}");
                return false;
            }
            finally
            {
                _isLoadingMoreTransactions = false;
            }

        }


    }
}
