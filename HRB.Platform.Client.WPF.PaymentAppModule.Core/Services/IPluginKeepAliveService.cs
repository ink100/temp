using System;
using System.Threading.Tasks;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 插件保活服务接口
    /// </summary>
    public interface IPluginKeepAliveService
    {
        /// <summary>
        /// 启动保活监控（包含进程检测，可选心跳检测）
        /// </summary>
        /// <param name="pluginName">插件名称</param>
        /// <param name="onPluginDied">插件进程消失或心跳超时时的回调</param>
        /// <param name="eventAggregator">事件聚合器，用于发送心跳（可选，为 null 时不启用心跳检测）</param>
        void StartMonitoring(string pluginName, Func<Task> onPluginDied, IEventAggregator eventAggregator = null);

        /// <summary>
        /// 停止保活监控
        /// </summary>
        /// <param name="pluginName">插件名称</param>
        void StopMonitoring(string pluginName);

        /// <summary>
        /// 停止所有监控
        /// </summary>
        void StopAll();

        /// <summary>
        /// 通知收到心跳响应
        /// </summary>
        /// <param name="pluginName">插件名称</param>
        void NotifyHeartbeatResponse(string pluginName);
    }
}
