using HRB.Payment.Message.Client;

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
            IsConnected = false;
            ConnectionStatus = "未连接";


            try
            {

                _client ??= new MessageClientShell(_eventAggregator);
                var isSuccess = await _client.StartAsync();


                if (isSuccess)
                {
                    _isLoadVX = true;
                    IsConnected = true;
                    ConnectionStatus = "已连接";
                }
                else
                {
                    IsConnected = false;
                    ConnectionStatus = "连接失败";
                }
            }
            catch (Exception e)
            {
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
            _client = null;
            IsConnected = false;
            ConnectionStatus = "已断开";
            return Task.CompletedTask;
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
