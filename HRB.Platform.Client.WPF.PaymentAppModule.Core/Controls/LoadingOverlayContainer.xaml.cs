using System.Windows;
using System.Windows.Controls;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Controls
{
    /// <summary>
    /// 全局加载遮罩层容器控件
    /// </summary>
    public partial class LoadingOverlayContainer : UserControl
    {
        public LoadingOverlayContainer()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 显示遮罩层
        /// </summary>
        public void Show(string message)
        {
            MessageText.Text = message;
            Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 隐藏遮罩层
        /// </summary>
        public void Hide()
        {
            Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 更新消息
        /// </summary>
        public void UpdateMessage(string message)
        {
            MessageText.Text = message;
        }
    }
}
