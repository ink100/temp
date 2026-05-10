using HRB.Platform.Client.WPF.Core.CustomAttributes;
using HRB.Platform.Client.WPF.Core.Models;
using HRB.Platform.Client.WPF.PaymentAppModule.Core;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Services;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Views
{
    /// <summary>
    /// MainPage.xaml 的交互逻辑
    /// </summary>
    [IocBindViewModelForNavigation(typeof(MainPageViewModel), GlobalSettings.APP_MODULE_NAME)]
    public partial class MainPage : BaseRegionUserControl// , IEntryView
    {
        private readonly IPluginProcessService _pluginProcessService;

        public MainPage(IPluginProcessService pluginProcessService)
        {
            _pluginProcessService = pluginProcessService;
            InitializeComponent();

            // 监听键盘事件
            this.KeyDown += MainPage_KeyDown;
            // 确保控件可以接收键盘焦点
            this.Focusable = true;

            // 控件加载完成后自动获取焦点
            this.Loaded += (s, e) => this.Focus();

        }

        private async void MainPage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;

                // 弹出确认对话框
                var result = MessageBox.Show(
                    "确定要退出应用吗？",
                    "退出确认",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await _pluginProcessService.CleanupExistingProcessesAsync();

                    // 直接退出应用
                    Application.Current.Shutdown();
                }
            }
        }


    }
}
