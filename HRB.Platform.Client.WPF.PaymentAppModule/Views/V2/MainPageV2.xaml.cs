using HRB.Platform.Client.WPF.Core.CustomAttributes;
using HRB.Platform.Client.WPF.Core.Models;
using HRB.Platform.Client.WPF.PaymentAppModule.Core;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.V2;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Views.V2
{
    [IocBindViewModelForNavigation(typeof(MainPageV2ViewModel), GlobalSettings.APP_MODULE_NAME)]
    public partial class MainPageV2 : BaseRegionUserControl
    {
        private static readonly TimeSpan AutoScrollSuspendDuration = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan AutoScrollDebounceDuration = TimeSpan.FromMilliseconds(180);
        private static readonly TimeSpan MinAutoScrollInterval = TimeSpan.FromMilliseconds(500);

        private ScrollViewer? _transactionScrollViewer;
        private DispatcherTimer? _autoScrollResumeTimer;
        private DispatcherTimer? _autoScrollDebounceTimer;

        private DateTime _lastAutoScrollAt = DateTime.MinValue;

        private bool _isProgrammaticScroll;
        private bool _isAutoScrollSuspendedByUser;
        private bool _isAutoScrollScheduled;
        public MainPageV2()
        {
            InitializeComponent();

            KeyDown += OnKeyDown;
            Focusable = true;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Focus();

            _transactionScrollViewer ??= FindVisualChild<ScrollViewer>(TransactionsDataGrid);
            if (_transactionScrollViewer != null)
            {
                _transactionScrollViewer.ScrollChanged -= OnTransactionScrollChanged;
                _transactionScrollViewer.ScrollChanged += OnTransactionScrollChanged;
            }

            TransactionsDataGrid.PreviewMouseWheel -= OnUserScrollInput;
            TransactionsDataGrid.PreviewMouseWheel += OnUserScrollInput;
            TransactionsDataGrid.PreviewTouchMove -= OnUserTouchMove;
            TransactionsDataGrid.PreviewTouchMove += OnUserTouchMove;
            TransactionsDataGrid.PreviewKeyDown -= OnTransactionGridPreviewKeyDown;
            TransactionsDataGrid.PreviewKeyDown += OnTransactionGridPreviewKeyDown;

            AttachTransactionsCollectionChanged();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_transactionScrollViewer != null)
                _transactionScrollViewer.ScrollChanged -= OnTransactionScrollChanged;

            TransactionsDataGrid.PreviewMouseWheel -= OnUserScrollInput;
            TransactionsDataGrid.PreviewTouchMove -= OnUserTouchMove;
            TransactionsDataGrid.PreviewKeyDown -= OnTransactionGridPreviewKeyDown;

            DetachTransactionsCollectionChanged();
            _autoScrollResumeTimer?.Stop();
            _autoScrollDebounceTimer?.Stop();

            _isProgrammaticScroll = false;
            _isAutoScrollScheduled = false;
            _isAutoScrollSuspendedByUser = false;
        }

        private void AttachTransactionsCollectionChanged()
        {
            if (DataContext is MainPageV2ViewModel vm)
            {
                vm.Transactions.CollectionChanged -= OnTransactionsCollectionChanged;
                vm.Transactions.CollectionChanged += OnTransactionsCollectionChanged;
            }
        }

        private void DetachTransactionsCollectionChanged()
        {
            if (DataContext is MainPageV2ViewModel vm)
                vm.Transactions.CollectionChanged -= OnTransactionsCollectionChanged;
        }

        private void OnTransactionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add &&
                e.Action != NotifyCollectionChangedAction.Reset &&
                e.Action != NotifyCollectionChangedAction.Replace)
                return;

            if (!IsAutoScrollEnabled() || _isAutoScrollSuspendedByUser)
                return;

            ScheduleAutoScroll();
        }
        /// <summary>
        /// 合并连续新增交易产生的自动滚动请求。
        /// 采用“短延迟 + 最小滚动间隔”的方式，避免突发记录把 UI 线程塞满。
        /// </summary>
        private void ScheduleAutoScroll()
        {
            if (!IsAutoScrollEnabled() || _isAutoScrollSuspendedByUser)
                return;

            if (_isAutoScrollScheduled)
                return;

            _isAutoScrollScheduled = true;

            var now = DateTime.UtcNow;
            var elapsed = now - _lastAutoScrollAt;

            var delay = elapsed >= MinAutoScrollInterval
                ? AutoScrollDebounceDuration
                : MinAutoScrollInterval - elapsed;

            if (delay < TimeSpan.FromMilliseconds(60))
                delay = TimeSpan.FromMilliseconds(60);

            _autoScrollDebounceTimer ??= new DispatcherTimer();
            _autoScrollDebounceTimer.Tick -= OnAutoScrollDebounceTimerTick;
            _autoScrollDebounceTimer.Tick += OnAutoScrollDebounceTimerTick;
            _autoScrollDebounceTimer.Interval = delay;
            _autoScrollDebounceTimer.Stop();
            _autoScrollDebounceTimer.Start();
        }

        private void OnAutoScrollDebounceTimerTick(object? sender, EventArgs e)
        {
            _autoScrollDebounceTimer?.Stop();

            Dispatcher.BeginInvoke(
                ScrollTransactionsToTop,
                DispatcherPriority.Background);
        }
        private bool IsAutoScrollEnabled()
        {
            return DataContext is MainPageV2ViewModel vm && vm.IsAutoScrollToLatestEnabled;
        }

        private void ScrollTransactionsToTop()
        {
            var shouldResetProgrammaticFlagLater = false;

            try
            {
                if (!IsAutoScrollEnabled() || _isAutoScrollSuspendedByUser)
                    return;

                _transactionScrollViewer ??= FindVisualChild<ScrollViewer>(TransactionsDataGrid);
                if (_transactionScrollViewer == null)
                    return;

                // 已经在顶部，就不要重复触发布局和滚动事件
                if (_transactionScrollViewer.VerticalOffset <= 0.5)
                {
                    _lastAutoScrollAt = DateTime.UtcNow;
                    return;
                }

                _isProgrammaticScroll = true;
                shouldResetProgrammaticFlagLater = true;

                _transactionScrollViewer.ScrollToTop();
                _lastAutoScrollAt = DateTime.UtcNow;
            }
            finally
            {
                if (shouldResetProgrammaticFlagLater)
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        _isProgrammaticScroll = false;
                        _isAutoScrollScheduled = false;
                    }, DispatcherPriority.Background);
                }
                else
                {
                    _isProgrammaticScroll = false;
                    _isAutoScrollScheduled = false;
                }
            }
        }


        private void OnUserScrollInput(object sender, MouseWheelEventArgs e)
        {
            SuspendAutoScrollTemporarily();
        }

        private void OnUserTouchMove(object sender, TouchEventArgs e)
        {
            SuspendAutoScrollTemporarily();
        }

        private void OnTransactionGridPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Up or Key.Down or Key.PageUp or Key.PageDown or Key.Home or Key.End or Key.Space)
                SuspendAutoScrollTemporarily();
        }

        private void OnTransactionScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // 程序主动滚动时，不识别为用户手动滚动
            if (_isProgrammaticScroll)
                return;

            // 鼠标拖动滚动条时不会触发 PreviewMouseWheel，
            // 这里继续作为“用户手动滚动”的兜底判断。
            //
            // ExtentHeightChange 接近 0，说明不是因为列表内容高度变化导致的滚动，
            // 更可能是用户主动滚动。
            if (Math.Abs(e.VerticalChange) > 0.01 &&
                Math.Abs(e.ExtentHeightChange) < 0.01)
            {
                SuspendAutoScrollTemporarily();
            }
        }

        private void SuspendAutoScrollTemporarily()
        {
            if (!IsAutoScrollEnabled())
                return;

            _isAutoScrollSuspendedByUser = true;

            // 用户开始手动查看历史记录时，取消尚未执行的自动滚动
            _autoScrollDebounceTimer?.Stop();
            _isAutoScrollScheduled = false;

            _autoScrollResumeTimer ??= new DispatcherTimer
            {
                Interval = AutoScrollSuspendDuration
            };
            _autoScrollResumeTimer.Tick -= OnAutoScrollResumeTimerTick;
            _autoScrollResumeTimer.Tick += OnAutoScrollResumeTimerTick;
            _autoScrollResumeTimer.Stop();
            _autoScrollResumeTimer.Start();
        }

        private void OnAutoScrollResumeTimerTick(object? sender, EventArgs e)
        {
            _autoScrollResumeTimer?.Stop();
            _isAutoScrollSuspendedByUser = false;
            // 用户 5 秒不再手动滚动后，补一次滚动到最新记录
            ScheduleAutoScroll();
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }

            return null;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;

                var result = MessageBox.Show("确定要退出应用吗？", "退出确认",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Application.Current.Shutdown();
                }
            }
        }
    }
}
