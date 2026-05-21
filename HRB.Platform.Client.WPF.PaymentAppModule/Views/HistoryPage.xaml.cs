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
            _scrollViewer ??= FindVisualChild<ScrollViewer>(this);

            if (_scrollViewer != null)
                _scrollViewer.ScrollChanged += OnScrollChanged;
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

            if (remaining > 3)
                return;

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
                await vm.LoadMoreTransactionsAsync();
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