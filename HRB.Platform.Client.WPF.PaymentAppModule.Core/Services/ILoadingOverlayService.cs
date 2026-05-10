using HRB.Platform.Client.WPF.PaymentAppModule.Core.Controls;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 全局加载遮罩层服务接口
    /// </summary>
    public interface ILoadingOverlayService
    {
        /// <summary>
        /// 设置遮罩层容器
        /// </summary>
        void SetContainer(LoadingOverlayContainer container);

        /// <summary>
        /// 显示遮罩层
        /// </summary>
        /// <param name="message">提示消息</param>
        void Show(string message = "正在处理，请稍候...");

        /// <summary>
        /// 隐藏遮罩层
        /// </summary>
        void Hide();

        /// <summary>
        /// 更新遮罩层消息
        /// </summary>
        /// <param name="message">新的提示消息</param>
        void UpdateMessage(string message);
    }
}
