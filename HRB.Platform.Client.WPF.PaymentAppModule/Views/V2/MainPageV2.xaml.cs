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

        private ScrollViewer? _transactionScrollViewer;
        private DispatcherTimer? _autoScrollResumeTimer;
        private bool _isProgrammaticScroll;
        private bool _isAutoScrollSuspendedByUser;
        /// <summary>
        /// 是否已经安排了一次自动滚动任务。
        /// 用于合并连续新增记录产生的重复滚动请求，避免 UI 线程堆积。
        /// </summary>
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
            _isProgrammaticScroll = false;
            _isAutoScrollPending = false;
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

            // 如果已经排队了一次自动滚动，就不再重复排队。
            // 连续多条交易进入时，最终只需要滚到一次最新记录即可。
            if (_isAutoScrollScheduled)
                return;

            _isAutoScrollScheduled = true;

            Dispatcher.BeginInvoke(
                ScrollTransactionsToTop,
                DispatcherPriority.ContextIdle);
        }

        private bool IsAutoScrollEnabled()
        {
            return DataContext is MainPageV2ViewModel vm && vm.IsAutoScrollToLatestEnabled;
        }

        private void ScrollTransactionsToTop()
        {
            try
            {
                if (!IsAutoScrollEnabled() || _isAutoScrollSuspendedByUser)
                    return;

                _transactionScrollViewer ??= FindVisualChild<ScrollViewer>(TransactionsDataGrid);
                if (_transactionScrollViewer == null)
                    return;

                _isProgrammaticScroll = true;

                if (TransactionsDataGrid.Items.Count > 0)
                {
                    // 先确保最新记录进入可视区域
                    var latestItem = TransactionsDataGrid.Items[0];
                    TransactionsDataGrid.ScrollIntoView(latestItem);
                }

                // 再将滚动条精确置顶
                _transactionScrollViewer.ScrollToTop();
            }
            finally
            {
                // 等这一轮 UI 滚动事件处理完后，再解除程序滚动标记
                Dispatcher.BeginInvoke(() =>
                {
                    _isProgrammaticScroll = false;
                    _isAutoScrollScheduled = false;
                }, DispatcherPriority.Background);
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
