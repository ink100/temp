using HRB.Payment.Core.Events;
using HRB.Payment.Core.Helpers;
using HRB.Payment.Core.Models;
using HRB.Payment.Message.Core.BusEvents;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.ViewModels
{
#if DEBUG
    /// <summary>
    /// 测试用支付数据
    /// </summary>
    internal class TestPaymentData
    {
        public string AlipayOrderNo { get; set; } = string.Empty;
        public string OtherAccount { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string TradeStatus { get; set; } = string.Empty;
    }

    /// <summary>
    /// MainPageViewModel 测试部分
    /// 包含所有测试相关的字段、属性和方法
    /// </summary>
    public partial class MainPageViewModel
    {
        #region 测试字段

        // 测试用定时器和状态
        private DispatcherTimer? _testPaymentTimer;
        private int _testEventIndex = 0;
        private int _testDataIndex = 0;
        private readonly List<TestPaymentData> _testPaymentDataList = new();

        #endregion

        #region 测试命令属性

        // 测试命令
        public ICommand TestPaymentStartedCommand { get; private set; }
        public ICommand TestPaymentSuccessCommand { get; private set; }
        public ICommand TestPaymentCancelledCommand { get; private set; }

        public ICommand GetAppStatusCommand { get; private set; }

        #endregion

        #region 测试命令初始化

        /// <summary>
        /// 初始化测试命令（在主构造函数中调用）
        /// </summary>
        partial void InitializeTestCommands()
        {
            TestPaymentStartedCommand = new DelegateCommand(TestPaymentStarted);
            TestPaymentSuccessCommand = new DelegateCommand(TestPaymentSuccess);
            TestPaymentCancelledCommand = new DelegateCommand(TestPaymentCancelled);

            GetAppStatusCommand = new DelegateCommand(GetAppStatus);

          //  _eventAggregator.GetEvent<GetVXStatusAnswerEvent>().Subscribe(OnVxStatusAnswer);

        }

        //private void OnVxStatusAnswer(VXStatusEto obj)
        //{
        //    Debug.Write(JsonConvert.SerializeObject(obj));
        //}

        #endregion

        #region 测试支付事件

        /// <summary>
        /// 启动测试支付定时器
        /// </summary>
        private void StartTestPaymentTimer()
        {
            if (_testPaymentTimer != null)
            {
                _testPaymentTimer.Stop();
                _testPaymentTimer = null;
            }

            _testEventIndex = 0;
            _testDataIndex = 0;

            _testPaymentTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _testPaymentTimer.Tick += OnTestPaymentTimerTick;
            _testPaymentTimer.Start();

            Debug.WriteLine("测试支付定时器已启动，每2秒触发一次事件");
        }

        /// <summary>
        /// 测试支付定时器触发
        /// 事件顺序：订单1(start -> cancel) -> 订单2(start -> success) -> 停止
        /// </summary>
        private void OnTestPaymentTimerTick(object? sender, EventArgs e)
        {
            if (_testPaymentDataList.Count < 2)
                return;

            // 根据事件索引执行不同的操作
            // 0: 订单1 start
            // 1: 订单1 cancel
            // 2: 订单2 start
            // 3: 订单2 success
            // 4: 停止定时器
            switch (_testEventIndex)
            {
                case 0:
                    {
                        // 订单1 - start
                        var data1 = _testPaymentDataList[0];
                        var eventArgs = new PaymentEventArgs
                        {
                            Amount = data1.TotalAmount,
                            DisplayName = data1.OtherAccount,
                            PaymentChannel = PaymentChannel.Alipay,
                            OrderNumber = data1.AlipayOrderNo,
                            UserId = data1.OtherAccount,
                            Remarks = data1.OtherAccount
                        };
                        Debug.WriteLine($"[测试] 订单1 - 发送 PaymentStarted 事件 - 订单号: {eventArgs.OrderNumber}, 金额: {eventArgs.Amount}");
                        _eventPublisher.PublishPaymentStarted(eventArgs);
                        break;
                    }

                case 1:
                    {
                        // 订单1 - cancel
                        var data1 = _testPaymentDataList[0];
                        var eventArgs = new PaymentEventArgs
                        {
                            Amount = data1.TotalAmount,
                            DisplayName = data1.OtherAccount,
                            PaymentChannel = PaymentChannel.Alipay,
                            OrderNumber = data1.AlipayOrderNo,
                            UserId = data1.OtherAccount,
                            Remarks = data1.OtherAccount
                        };
                        Debug.WriteLine($"[测试] 订单1 - 发送 PaymentCancelled 事件 - 订单号: {eventArgs.OrderNumber}");
                        _eventPublisher.PublishPaymentCancelled(eventArgs);
                        break;
                    }

                case 2:
                    {
                        // 订单2 - start
                        var data2 = _testPaymentDataList[1];
                        var eventArgs = new PaymentEventArgs
                        {
                            Amount = data2.TotalAmount,
                            DisplayName = data2.OtherAccount,
                            PaymentChannel = PaymentChannel.Alipay,
                            OrderNumber = data2.AlipayOrderNo,
                            UserId = data2.OtherAccount,
                            Remarks = data2.OtherAccount
                        };
                        Debug.WriteLine($"[测试] 订单2 - 发送 PaymentStarted 事件 - 订单号: {eventArgs.OrderNumber}, 金额: {eventArgs.Amount}");
                        _eventPublisher.PublishPaymentStarted(eventArgs);
                        break;
                    }

                case 3:
                    {
                        // 订单2 - success
                        var data2 = _testPaymentDataList[1];
                        var eventArgs = new PaymentEventArgs
                        {
                            Amount = data2.TotalAmount,
                            DisplayName = data2.OtherAccount,
                            PaymentChannel = PaymentChannel.Alipay,
                            OrderNumber = data2.AlipayOrderNo,
                            UserId = data2.OtherAccount,
                            Remarks = data2.OtherAccount
                        };
                        Debug.WriteLine($"[测试] 订单2 - 发送 PaymentSuccess 事件 - 订单号: {eventArgs.OrderNumber}, 金额: {eventArgs.Amount}");
                        _eventPublisher.PublishPaymentSuccess(eventArgs);
                        break;
                    }

                case 4:
                    {
                        // 停止定时器
                        Debug.WriteLine("[测试] 所有测试订单已完成，停止定时器");
                        StopTestPaymentTimer();
                        return;
                    }
            }

            // 递增事件索引
            _testEventIndex++;
        }

        /// <summary>
        /// 停止测试支付定时器
        /// </summary>
        private void StopTestPaymentTimer()
        {
            if (_testPaymentTimer != null)
            {
                _testPaymentTimer.Stop();
                _testPaymentTimer = null;
                Debug.WriteLine("测试支付定时器已停止");
            }
        }

        /// <summary>
        /// 测试支付开始事件
        /// </summary>
        private void TestPaymentStarted()
        {

            var eventArgs = new PaymentEventArgs
            {
                Amount = 0.01m,
                DisplayName = "测试用户",
                PaymentChannel = PaymentChannel.Alipay,
                OrderNumber = Guid.NewGuid().ToString(),
                UserId = "test_user_001",
                Remarks = "测试用户",
                PayTime = PcHelper.GetNetNowTime(),
                Status = PaymentStatus.Scan
            };
            Debug.WriteLine($"[手动测试] 发送 PaymentStarted 事件 - 订单号: {eventArgs.OrderNumber}, 金额: {eventArgs.Amount}");
            _eventPublisher.PublishPaymentStarted(eventArgs);
        }

        /// <summary>
        /// 测试支付成功事件
        /// </summary>
        private void TestPaymentSuccess()
        {
            // 获取最后一个扫码中的订单
            var lastScanOrder = Transactions.FirstOrDefault(t => t.Status == PaymentStatus.Scan);
            if (lastScanOrder == null)
            {
                _notificationService.ShowWarning("没有找到扫码中的订单");
                return;
            }

            var eventArgs = new PaymentEventArgs
            {
                Amount = lastScanOrder.Amount,
                DisplayName = lastScanOrder.Remarks,
                PaymentChannel = lastScanOrder.PaymentChannel,
                OrderNumber = lastScanOrder.OrderNumber,
                UserId = lastScanOrder.UserId,
                Remarks = lastScanOrder.Remarks,
                PayTime = PcHelper.GetNetNowTime(),
                Status = PaymentStatus.Success
            };
            Debug.WriteLine($"[手动测试] 发送 PaymentSuccess 事件 - 订单号: {eventArgs.OrderNumber}");
            _eventPublisher.PublishPaymentSuccess(eventArgs);
        }

        /// <summary>
        /// 测试支付取消事件
        /// </summary>
        private void TestPaymentCancelled()
        {
            // 获取最后一个扫码中的订单
            var lastScanOrder = Transactions.FirstOrDefault(t => t.Status == PaymentStatus.Scan);
            if (lastScanOrder == null)
            {
                _notificationService.ShowWarning("没有找到扫码中的订单");
                return;
            }

            var eventArgs = new PaymentEventArgs
            {
                Amount = lastScanOrder.Amount,
                DisplayName = lastScanOrder.Remarks,
                PaymentChannel = lastScanOrder.PaymentChannel,
                OrderNumber = lastScanOrder.OrderNumber,
                UserId = lastScanOrder.UserId,
                Remarks = lastScanOrder.Remarks,
                PayTime = PcHelper.GetNetNowTime(),
                Status = PaymentStatus.Cancel
            };
            Debug.WriteLine($"[手动测试] 发送 PaymentCancelled 事件 - 订单号: {eventArgs.OrderNumber}");
            _eventPublisher.PublishPaymentCancelled(eventArgs);
        }

        #endregion



        #region 测试微信插件事件

        private void GetAppStatus()
        {
           // _eventAggregator.GetEvent<GetVXStatusRequestEvent>().Publish();
        }

        #endregion
    }
#endif
}
