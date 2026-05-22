using HRB.Platform.Client.WPF.Core.CustomAttributes;
using HRB.Platform.Client.WPF.PaymentAppModule.Core;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Views
{
    /// <summary>
    /// Interaction logic for HistoryView.xaml
    /// </summary>
    [IocBindViewModelForNavigation(typeof(HistoryPageModel), GlobalSettings.APP_MODULE_NAME)]
    public partial class HistoryPage
    {
        private ScrollViewer? _scrollViewer;
        private bool _isLoadingMore;

        public HistoryPage()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_scrollViewer != null)
                _scrollViewer.ScrollChanged -= OnScrollChanged;

            // 优先绑定台账 DataGrid 内部的 ScrollViewer。
            var dataGrid = FindVisualChild<DataGrid>(this);
            _scrollViewer = dataGrid == null
                ? FindVisualChild<ScrollViewer>(this)
                : FindVisualChild<ScrollViewer>(dataGrid);

            if (_scrollViewer != null)
            {
                _scrollViewer.ScrollChanged += OnScrollChanged;

                GlobalSettings.CurrentAppContext.CurrentLogger.Info(
                    "台账滚动监听已绑定");
            }
            else
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Info(
                    "台账滚动监听绑定失败：未找到 ScrollViewer");
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_scrollViewer != null)
                _scrollViewer.ScrollChanged -= OnScrollChanged;

            _isLoadingMore = false;
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_isLoadingMore)
                return;

            if (e.VerticalChange <= 0.01)
                return;

            if (e.ExtentHeight <= 0 || e.ViewportHeight <= 0)
                return;

            var remaining = e.ExtentHeight - e.VerticalOffset - e.ViewportHeight;

            GlobalSettings.CurrentAppContext.CurrentLogger.Info(
                $"台账滚动检查，Offset:{e.VerticalOffset:F2}，Viewport:{e.ViewportHeight:F2}，Extent:{e.ExtentHeight:F2}，Remaining:{remaining:F2}");

            if (remaining > 3)
                return;

            GlobalSettings.CurrentAppContext.CurrentLogger.Info(
                "台账滚动到底部，准备加载更多");

            _ = LoadMoreAsync();
        }

        private async Task LoadMoreAsync()
        {
            if (_isLoadingMore)
                return;

            if (DataContext is not HistoryPageModel vm)
                return;

            _isLoadingMore = true;

            try
            {
                var loaded = await vm.LoadMoreTransactionsAsync();

                GlobalSettings.CurrentAppContext.CurrentLogger.Info(
                    $"台账滚动加载完成，是否加载到数据:{loaded}");
            }
            catch (Exception ex)
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Error(
                    $"台账滚动加载更多异常:{ex}");
            }
            finally
            {
                _isLoadingMore = false;
            }
        }

        private static T? FindVisualChild<T>(DependencyObject parent)
            where T : DependencyObject
        {
            if (parent == null)
                return null;

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
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
    }
}