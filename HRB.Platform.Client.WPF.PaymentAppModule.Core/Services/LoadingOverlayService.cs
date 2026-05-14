using HRB.Platform.Client.WPF.PaymentAppModule.Core.Controls;
using System.Windows;
using Application = System.Windows.Application;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 全局加载遮罩层服务实现
    /// </summary>
    public class LoadingOverlayService : ILoadingOverlayService
    {
        private LoadingOverlayContainer? _container;

        /// <summary>
        /// 设置遮罩层容器
        /// </summary>
        public void SetContainer(LoadingOverlayContainer container)
        {
            _container = container;
        }

        /// <summary>
        /// 显示遮罩层
        /// </summary>
        public void Show(string message = "正在处理，请稍候...")
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                _container?.Show(message);
            });
        }

        /// <summary>
        /// 隐藏遮罩层
        /// </summary>
        public void Hide()
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                _container?.Hide();
            });
        }

        /// <summary>
        /// 更新遮罩层消息
        /// </summary>
        public void UpdateMessage(string message)
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                _container?.UpdateMessage(message);
            });
        }
    }
}
