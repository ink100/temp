using HRB.Payment.Message.Client;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Helpers;
using System.Threading;
using Timer = System.Threading.Timer;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 微信连接管理服务实现
    /// 负责管理微信客户端的连接生命周期和状态
    /// </summary>
    public class WeChatConnectionService : IWeChatConnectionService
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IPluginProcessService _pluginProcessService;
        private MessageClientShell? _client;
        private bool _isLoadVX = false;
        private bool _isConnected = false;
        private string _connectionStatus = "未连接";
        private Timer _diagnosticHeartbeatTimer;
        private DateTime _lastHeartbeatOutputTime = DateTime.MinValue;
        private DateTime _lastStartTime = DateTime.MinValue;
        private DateTime _lastConnectedTime = DateTime.MinValue;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="eventAggregator">事件聚合器，用于消息通信</param>
        /// <param name="pluginProcessService"></param>
        public WeChatConnectionService(IEventAggregator eventAggregator, IPluginProcessService pluginProcessService)
        {
            _eventAggregator = eventAggregator;
            _pluginProcessService = pluginProcessService;
        }

        /// <summary>
        /// 获取当前连接状态
        /// </summary>
        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnConnectionStatusChanged();
                }
            }
        }

        /// <summary>
        /// 获取连接状态描述
        /// </summary>
        public string ConnectionStatus
        {
            get => _connectionStatus;
            private set
            {
                if (_connectionStatus != value)
                {
                    _connectionStatus = value;
                    OnConnectionStatusChanged();
                }
            }
        }

        /// <summary>
        /// 连接状态变化事件
        /// </summary>
        public event EventHandler<ConnectionStatusChangedEventArgs>? ConnectionStatusChanged;

        /// <summary>
        /// 启动微信连接服务
        /// </summary>
        /// <returns>启动任务</returns>
        public async Task StartAsync()
        {
            // 初始化状态
            _lastStartTime = DateTime.Now;
            WeChatListenerConsoleDebug.Write("CONNECTION", "开始启动微信消息连接服务");
            WeChatListenerConsoleDebug.Write("EVENT-BUS", $"WeChatConnectionService 使用的 EventAggregator Hash={_eventAggregator.GetHashCode()}");
            IsConnected = false;
            ConnectionStatus = "未连接";


            try
            {

                _client ??= new MessageClientShell(_eventAggregator);
                WeChatListenerConsoleDebug.Write("MSG-CLIENT", $"MessageClientShell 已创建，ClientHash={_client.GetHashCode()}，ClientType={_client.GetType().FullName}");
                WeChatListenerConsoleDebug.Write("CONNECTION", "MessageClientShell 准备启动");
                var isSuccess = await _client.StartAsync();
                WeChatListenerConsoleDebug.Write("CONNECTION", $"MessageClientShell 启动返回：{isSuccess}");


                if (isSuccess)
                {
                    _isLoadVX = true;
                    IsConnected = true;
                    ConnectionStatus = "已连接";
                    _lastConnectedTime = DateTime.Now;
                    WeChatListenerConsoleDebug.Write("CONNECTION", "微信消息连接服务已连接");
                    StartDiagnosticHeartbeat();
                }
                else
                {
                    IsConnected = false;
                    ConnectionStatus = "连接失败";
                    WeChatListenerConsoleDebug.Write("CONNECTION", "微信消息连接服务连接失败");
                    StartDiagnosticHeartbeat();
                }
            }
            catch (Exception e)
            {
                WeChatListenerConsoleDebug.WriteException("CONNECTION-ERROR", e);
                Console.WriteLine(e);
                throw;
            }



            return;

        }

        /// <summary>
        /// 停止微信连接服务
        /// </summary>
        /// <returns>停止任务</returns>
        public Task StopAsync()
        {
            _isLoadVX = true; // 停止监听循环
            StopDiagnosticHeartbeat();
            _client = null;
            IsConnected = false;
            ConnectionStatus = "已断开";
            WeChatListenerConsoleDebug.Write("CONNECTION", "微信消息连接服务已断开");
            return Task.CompletedTask;
        }

        
        /// <summary>
        /// 启动微信监听诊断心跳。
        /// 作用：扫码后没有业务日志时，确认 MessageClientShell/连接服务是否仍然处于连接状态。
        /// 只在“微信监听控制台输出”开启时输出；关闭后 Timer 仍低频运行但不输出，避免影响业务。
        /// </summary>
        private void StartDiagnosticHeartbeat()
        {
            try
            {
                if (_diagnosticHeartbeatTimer != null)
                    return;

                _diagnosticHeartbeatTimer = new Timer(_ => OutputDiagnosticHeartbeat(), null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5));
                WeChatListenerConsoleDebug.Write("HEARTBEAT", "微信监听诊断心跳已启动，间隔5秒");
            }
            catch (Exception ex)
            {
                WeChatListenerConsoleDebug.WriteException("HEARTBEAT-ERROR", ex);
            }
        }

        /// <summary>
        /// 停止微信监听诊断心跳。
        /// </summary>
        private void StopDiagnosticHeartbeat()
        {
            try
            {
                var timer = _diagnosticHeartbeatTimer;
                _diagnosticHeartbeatTimer = null;
                timer?.Dispose();
                WeChatListenerConsoleDebug.Write("HEARTBEAT", "微信监听诊断心跳已停止");
            }
            catch (Exception ex)
            {
                WeChatListenerConsoleDebug.WriteException("HEARTBEAT-ERROR", ex);
            }
        }

        /// <summary>
        /// 输出连接层状态。
        /// 注意：这里不能直接证明微信支付数据进来了，只用于判断“扫码没日志时，连接层是否还活着”。
        /// </summary>
        private void OutputDiagnosticHeartbeat()
        {
            if (!WeChatListenerConsoleDebug.IsEnabled)
                return;

            try
            {
                var now = DateTime.Now;
                if ((now - _lastHeartbeatOutputTime).TotalSeconds < 4)
                    return;

                _lastHeartbeatOutputTime = now;

                var clientExists = _client != null;
                var clientHash = clientExists ? _client.GetHashCode().ToString() : "null";
                var clientType = clientExists ? _client.GetType().FullName : "null";
                var connectedSeconds = _lastConnectedTime == DateTime.MinValue ? -1 : (int)(now - _lastConnectedTime).TotalSeconds;

                WeChatListenerConsoleDebug.Write("HEARTBEAT",
                    $"IsConnected={IsConnected}, ConnectionStatus={ConnectionStatus}, ClientExists={clientExists}, ClientHash={clientHash}, ClientType={clientType}, IsLoadVX={_isLoadVX}, LastStart={_lastStartTime:yyyy-MM-dd HH:mm:ss}, LastConnected={_lastConnectedTime:yyyy-MM-dd HH:mm:ss}, ConnectedSeconds={connectedSeconds}, EventAggregatorHash={_eventAggregator.GetHashCode()}");
            }
            catch (Exception ex)
            {
                WeChatListenerConsoleDebug.WriteException("HEARTBEAT-ERROR", ex);
            }
        }

/// <summary>
        /// 触发连接状态变化事件
        /// </summary>
        private void OnConnectionStatusChanged()
        {
            ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs
            {
                IsConnected = IsConnected,
                Status = ConnectionStatus
            });
        }
    }
}
