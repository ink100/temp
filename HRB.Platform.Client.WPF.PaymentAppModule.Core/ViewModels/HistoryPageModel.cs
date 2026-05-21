using HRB.Payment.Core.Models;
using HRB.Payment.Core.Services;
using HRB.Platform.Client.WPF.Core.Services.IServices;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.DboModels;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.DtoModels;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Repository;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.ViewModels
{
    public class HistoryPageModel : BasePaymentRegionViewModel, INavigationAware
    {
        private readonly IPaymentRepository _transactionService;
        private readonly IRegionManager _regionManager;
        private ObservableCollection<TransactionRecord> _transactions = new();
        private DateTime _startDate = DateTime.Parse("1900-01-01 00:00:00");
        private DateTime _endDate = DateTime.Today.AddDays(1);
        private string _searchText = string.Empty;
        private bool _isLoading;
        private const int LedgerPageSize = 50;

        private bool _isLoadingMore;
        private bool _hasMoreTransactions = true;
        private bool _suppressAutoLoad;

        private DateTime? _lastCursorTime;
        private int _lastCursorId;
        private string _selectedPaymentChannel = string.Empty;
        private string _selectedPaymentStatus = string.Empty;
        private decimal _totalRevenue;
        private decimal _alipayRevenue;
        private decimal _weChatRevenue;
        private int _alipayCount;
        private int _weChatCount;
        private int _successCount;
        private int _cancelCount;

        public HistoryPageModel(PaymentAppContext appContext, IEventAggregator eventAggregator, IRegionManager regionManager, IWpfDeviceRequestService deviceRequestService, IPaymentRepository transactionService) : base(appContext, eventAggregator, regionManager, deviceRequestService)
        {
            _transactionService = transactionService;
            _regionManager = regionManager;
            LoadTransactionsCommand = new DelegateCommand(async () => await LoadTransactionsAsync(), () => !IsLoading)
                .ObservesProperty(() => IsLoading);
            RefreshCommand = new DelegateCommand(async () => await LoadTransactionsAsync());
            NavigateToMainCommand = new DelegateCommand(NavigateToMain);

            // 快捷日期命令
            SetTodayCommand = new DelegateCommand(SetToday);
            SetThisWeekCommand = new DelegateCommand(SetThisWeek);
            SetLastWeekCommand = new DelegateCommand(SetLastWeek);
            SetThisMonthCommand = new DelegateCommand(SetThisMonth);
            SetThisQuarterCommand = new DelegateCommand(SetThisQuarter);
            SetThisYearCommand = new DelegateCommand(SetThisYear);
            SetAllCommand = new DelegateCommand(SetAll);
        }

        public ObservableCollection<TransactionRecord> Transactions
        {
            get => _transactions;
            set => SetProperty(ref _transactions, value);
        }

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value) && !_suppressAutoLoad)
                {
                    _ = LoadTransactionsAsync();
                }
            }
        }
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value) && !_suppressAutoLoad)
                {
                    _ = LoadTransactionsAsync();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public string SelectedPaymentChannel
        {
            get => _selectedPaymentChannel;
            set
            {
                if (SetProperty(ref _selectedPaymentChannel, value))
                {
                    _ = LoadTransactionsAsync();
                }
            }
        }

        public string SelectedPaymentStatus
        {
            get => _selectedPaymentStatus;
            set
            {
                if (SetProperty(ref _selectedPaymentStatus, value))
                {
                    _ = LoadTransactionsAsync();
                }
            }
        }

        public decimal TotalRevenue
        {
            get => _totalRevenue;
            set => SetProperty(ref _totalRevenue, value);
        }

        public decimal AlipayRevenue
        {
            get => _alipayRevenue;
            set => SetProperty(ref _alipayRevenue, value);
        }

        public decimal WeChatRevenue
        {
            get => _weChatRevenue;
            set => SetProperty(ref _weChatRevenue, value);
        }

        public int AlipayCount
        {
            get => _alipayCount;
            set => SetProperty(ref _alipayCount, value);
        }

        public int WeChatCount
        {
            get => _weChatCount;
            set => SetProperty(ref _weChatCount, value);
        }

        public int SuccessCount
        {
            get => _successCount;
            set => SetProperty(ref _successCount, value);
        }

        public int CancelCount
        {
            get => _cancelCount;
            set => SetProperty(ref _cancelCount, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                SetProperty(ref _isLoading, value);
                LoadTransactionsCommand.RaiseCanExecuteChanged();
            }
        }

        public DelegateCommand LoadTransactionsCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand NavigateToMainCommand { get; }
        public DelegateCommand SetTodayCommand { get; }
        public DelegateCommand SetThisWeekCommand { get; }
        public DelegateCommand SetLastWeekCommand { get; }
        public DelegateCommand SetThisMonthCommand { get; }
        public DelegateCommand SetThisQuarterCommand { get; }
        public DelegateCommand SetThisYearCommand { get; }
        public DelegateCommand SetAllCommand { get; }



        public async Task LoadTransactionsAsync()
        {
            if (IsLoading)
                return;

            IsLoading = true;

            try
            {
                ResetCursorState();
                Transactions.Clear();

                await LoadLedgerSummaryAsync();

                await LoadMoreTransactionsCoreAsync(isInitialLoad: true);
            }
            finally
            {
                IsLoading = false;
            }
        }
        private async Task LoadLedgerSummaryAsync()
        {
            var summary = await _transactionService.GetLedgerSummaryAsync(
                StartDate,
                EndDate,
                GetSelectedPaymentChannel(),
                GetSelectedPaymentStatus(),
                SearchText);

            TotalRevenue = summary.TotalRevenue;
            AlipayRevenue = summary.AlipayRevenue;
            WeChatRevenue = summary.WeChatRevenue;
            AlipayCount = summary.AlipayCount;
            WeChatCount = summary.WeChatCount;
            SuccessCount = summary.SuccessCount;
            CancelCount = summary.CancelCount;
        }
        public async Task<bool> LoadMoreTransactionsAsync()
        {
            if (_isLoadingMore || !_hasMoreTransactions)
                return false;

            return await LoadMoreTransactionsCoreAsync(isInitialLoad: false);
        }
        private async Task<bool> LoadMoreTransactionsCoreAsync(bool isInitialLoad)
        {
            if (_isLoadingMore)
                return false;

            _isLoadingMore = true;

            try
            {
                var totalAdded = 0;
                var maxAttempts = isInitialLoad ? 10 : 3;

                for (var attempt = 0; attempt < maxAttempts; attempt++)
                {
                    var result = await _transactionService.GetLedgerTransactionsBeforeAsync(
                        StartDate,
                        EndDate,
                        GetSelectedPaymentChannel(),
                        GetSelectedPaymentStatus(),
                        SearchText,
                        isInitialLoad && !_lastCursorTime.HasValue ? null : _lastCursorTime,
                        _lastCursorId,
                        LedgerPageSize);

                    UpdateCursor(result);

                    if (result.Items.Count > 0)
                    {
                        var existingIds = new HashSet<int>(
                            Transactions
                                .Where(t => t.Id > 0)
                                .Select(t => t.Id));

                        foreach (var dbo in result.Items)
                        {
                            if (dbo.Id > 0 && existingIds.Contains(dbo.Id))
                                continue;

                            Transactions.Add(dbo.ToModel());
                            existingIds.Add(dbo.Id);
                            totalAdded++;
                        }
                    }

                    _hasMoreTransactions = result.HasMore;

                    if (totalAdded > 0 || !_hasMoreTransactions)
                        break;
                }

                return totalAdded > 0;
            }
            finally
            {
                _isLoadingMore = false;
            }
        }
        private void UpdateCursor(CursorPageResult<TransactionRecordDbo> result)
        {
            if (result.NextCursorTime.HasValue)
            {
                _lastCursorTime = result.NextCursorTime.Value;
                _lastCursorId = result.NextCursorId;
            }
        }

        private void ResetCursorState()
        {
            _hasMoreTransactions = true;
            _lastCursorTime = null;
            _lastCursorId = 0;
        }

        public void ClearLedgerState()
        {
            _isLoadingMore = false;
            _hasMoreTransactions = true;
            _lastCursorTime = null;
            _lastCursorId = 0;

            Transactions.Clear();

            TotalRevenue = 0;
            AlipayRevenue = 0;
            WeChatRevenue = 0;
            AlipayCount = 0;
            WeChatCount = 0;
            SuccessCount = 0;
            CancelCount = 0;
        }
        private void SetDateRange(DateTime startDate, DateTime endDate)
        {
            _suppressAutoLoad = true;

            StartDate = startDate;
            EndDate = endDate;

            _suppressAutoLoad = false;

            _ = LoadTransactionsAsync();
        }
        private PaymentChannel? GetSelectedPaymentChannel()
        {
            if (string.IsNullOrWhiteSpace(SelectedPaymentChannel))
                return null;

            return Enum.TryParse<PaymentChannel>(SelectedPaymentChannel, out var channel)
                ? channel
                : null;
        }

        private PaymentStatus? GetSelectedPaymentStatus()
        {
            if (string.IsNullOrWhiteSpace(SelectedPaymentStatus))
                return null;

            return Enum.TryParse<PaymentStatus>(SelectedPaymentStatus, out var status)
                ? status
                : null;
        }
        private void NavigateToMain()
        {
            PaymentGoBack();
        }

        private void SetToday()
        {
            SetDateRange(DateTime.Today, DateTime.Today.AddDays(1));
        }

        private void SetThisWeek()
        {
            var today = DateTime.Today;
            var dayOfWeek = (int)today.DayOfWeek;
            var monday = dayOfWeek == 0 ? today.AddDays(-6) : today.AddDays(1 - dayOfWeek);

            SetDateRange(monday, monday.AddDays(7));
        }

        private void SetLastWeek()
        {
            var today = DateTime.Today;
            var dayOfWeek = (int)today.DayOfWeek;
            var thisMonday = dayOfWeek == 0 ? today.AddDays(-6) : today.AddDays(1 - dayOfWeek);
            var lastMonday = thisMonday.AddDays(-7);

            SetDateRange(lastMonday, thisMonday);
        }

        private void SetThisMonth()
        {
            var today = DateTime.Today;
            var start = new DateTime(today.Year, today.Month, 1);

            SetDateRange(start, start.AddMonths(1));
        }

        private void SetThisQuarter()
        {
            var today = DateTime.Today;
            var quarterStartMonth = ((today.Month - 1) / 3) * 3 + 1;
            var start = new DateTime(today.Year, quarterStartMonth, 1);

            SetDateRange(start, start.AddMonths(3));
        }

        private void SetThisYear()
        {
            var today = DateTime.Today;
            var start = new DateTime(today.Year, 1, 1);

            SetDateRange(start, new DateTime(today.Year + 1, 1, 1));
        }

        private void SetAll()
        {
            var today = DateTime.Today;

            SetDateRange(
                DateTime.Parse("1900-01-01 00:00:00"),
                new DateTime(today.Year + 1, 1, 1));
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            _ = LoadTransactionsAsync();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            ClearLedgerState();
        }
    }
}

