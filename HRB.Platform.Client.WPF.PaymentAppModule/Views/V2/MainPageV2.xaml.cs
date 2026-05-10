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

            Dispatcher.BeginInvoke(ScrollTransactionsToTop, DispatcherPriority.Background);
        }

        private bool IsAutoScrollEnabled()
        {
            return DataContext is MainPageV2ViewModel vm && vm.IsAutoScrollToLatestEnabled;
        }

        private void ScrollTransactionsToTop()
        {
            if (!IsAutoScrollEnabled() || _isAutoScrollSuspendedByUser)
                return;

            _transactionScrollViewer ??= FindVisualChild<ScrollViewer>(TransactionsDataGrid);
            if (_transactionScrollViewer == null)
                return;

            try
            {
                _isProgrammaticScroll = true;
                _transactionScrollViewer.ScrollToTop();

                if (TransactionsDataGrid.Items.Count > 0)
                    TransactionsDataGrid.ScrollIntoView(TransactionsDataGrid.Items[0]);
            }
            finally
            {
                Dispatcher.BeginInvoke(() => _isProgrammaticScroll = false, DispatcherPriority.Background);
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
            if (_isProgrammaticScroll)
                return;

            // 鼠标拖动滚动条时不会触发 PreviewMouseWheel，这里用 ScrollChanged 兜底识别用户滚动。
            if (Math.Abs(e.VerticalChange) > 0.01 && Math.Abs(e.ExtentHeightChange) < 0.01)
                SuspendAutoScrollTemporarily();
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
