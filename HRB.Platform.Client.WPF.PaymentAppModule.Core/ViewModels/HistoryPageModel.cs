using HRB.Payment.Core.Models;
using HRB.Payment.Core.Services;
using HRB.Platform.Client.WPF.Core.Services.IServices;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Repository;
using Prism.Commands;
using Prism.Mvvm;
using System;
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
                if (SetProperty(ref _startDate, value))
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
                if (SetProperty(ref _endDate, value))
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
                var transactions = await _transactionService.GetTransactionsByDateRangeAsync(StartDate, EndDate);

                // 支付渠道筛选
                if (!string.IsNullOrWhiteSpace(SelectedPaymentChannel))
                {
                    transactions = transactions.Where(t => t.PaymentChannel.ToString() == SelectedPaymentChannel).ToList();
                }

                // 支付状态筛选
                if (!string.IsNullOrWhiteSpace(SelectedPaymentStatus))
                {
                    transactions = transactions.Where(t => t.Status.ToString() == SelectedPaymentStatus).ToList();
                }

                // 文本搜索
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    transactions = transactions.Where(t =>
                        t.ToModel().OrderNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        t.ToModel().UserId.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        t.ToModel().DisplayName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        t.ToModel().PaymentChannelDisplay.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        t.ToModel().Remarks.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                // 计算统计数据
                TotalRevenue = transactions.Where(t => t.Status == PaymentStatus.Success).Sum(t => t.Amount);
                AlipayRevenue = transactions.Where(t => t.PaymentChannel == PaymentChannel.Alipay && t.Status == PaymentStatus.Success).Sum(t => t.Amount);
                WeChatRevenue = transactions.Where(t => t.PaymentChannel == PaymentChannel.WeChat && t.Status == PaymentStatus.Success).Sum(t => t.Amount);
                AlipayCount = transactions.Count(t => t.PaymentChannel == PaymentChannel.Alipay);
                WeChatCount = transactions.Count(t => t.PaymentChannel == PaymentChannel.WeChat);
                SuccessCount = transactions.Count(t => t.Status == PaymentStatus.Success);
                CancelCount = transactions.Count(t => t.Status == PaymentStatus.Cancel);

                Transactions.Clear();
                foreach (var transaction in transactions)
                {
                    Transactions.Add(transaction.ToModel());
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void NavigateToMain()
        {
            PaymentGoBack();
        }

        private void SetToday()
        {
            StartDate = DateTime.Today;
            EndDate = DateTime.Today.AddDays(1);
        }

        private void SetThisWeek()
        {
            var today = DateTime.Today;
            var dayOfWeek = (int)today.DayOfWeek;
            var monday = dayOfWeek == 0 ? today.AddDays(-6) : today.AddDays(1 - dayOfWeek);
            StartDate = monday;
            EndDate = monday.AddDays(7);
        }

        private void SetLastWeek()
        {
            var today = DateTime.Today;
            var dayOfWeek = (int)today.DayOfWeek;
            var thisMonday = dayOfWeek == 0 ? today.AddDays(-6) : today.AddDays(1 - dayOfWeek);
            var lastMonday = thisMonday.AddDays(-7);
            StartDate = lastMonday;
            EndDate = thisMonday;
        }

        private void SetThisMonth()
        {
            var today = DateTime.Today;
            StartDate = new DateTime(today.Year, today.Month, 1);
            EndDate = StartDate.AddMonths(1);
        }

        private void SetThisQuarter()
        {
            var today = DateTime.Today;
            var quarterStartMonth = ((today.Month - 1) / 3) * 3 + 1;
            StartDate = new DateTime(today.Year, quarterStartMonth, 1);
            EndDate = StartDate.AddMonths(3);
        }

        private void SetThisYear()
        {
            var today = DateTime.Today;
            StartDate = new DateTime(today.Year, 1, 1);
            EndDate = new DateTime(today.Year + 1, 1, 1);
        }
        private void SetAll()
        {
            var today = DateTime.Today;
            StartDate = DateTime.Parse("1900-01-01 00:00:00");
            EndDate = new DateTime(today.Year + 1, 1, 1);
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
        }
    }
}

