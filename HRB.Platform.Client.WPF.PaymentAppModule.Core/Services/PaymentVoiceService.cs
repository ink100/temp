using HRB.Payment.Core.Models;
using HRB.Payment.Core.Services;
using HRB.Platform.Client.Core.Interfaces;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 支付语音播报服务实现。
    /// 语音播放设备通常只能稳定串行播放；这里用队列锁避免多笔订单连续触发时出现重叠、抢声或漏播。
    /// 同时增加订单级去重和过期扫码提示抑制，避免用户已经付款后又听到“扫码未支付”。
    /// </summary>
    public class PaymentVoiceService : IPaymentVoiceService
    {
        private const int RepeatIntervalMilliseconds = 1000;
        private const int BeforeNotPayRepeatCount = 2;
        private const int CancelRepeatCount = 2;

        private static readonly TimeSpan DuplicateWindow = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan CompletedOrderRetention = TimeSpan.FromMinutes(10);

        private readonly INumberToSpeechService _speechService;
        private readonly ITtsService _ttsService;
        private readonly IHrbLogger _log;

        private readonly SemaphoreSlim _playLock = new(1, 1);
        private readonly object _stateLock = new();
        private readonly Dictionary<string, DateTime> _lastVoiceRequestTimes = new();
        private readonly Dictionary<string, DateTime> _completedOrderTimes = new();

        public PaymentVoiceService(
            INumberToSpeechService speechService,
            ITtsService ttsService)
        {
            _speechService = speechService;
            _ttsService = ttsService;
            _log = GlobalSettings.CurrentAppContext.CurrentLogger;
        }

        /// <summary>
        /// 播放支付开始提示音
        /// </summary>
        public Task PlayPaymentStartedAsync(PaymentChannel channel, string? nickname = null, string? orderNumber = null)
        {
            if (ShouldSkipDuplicate(BuildVoiceKey("start", orderNumber, channel.ToString())))
                return Task.CompletedTask;

            return EnqueuePlaybackAsync(async () =>
            {
                await SpeakNicknameAsync(nickname);
                await _speechService.PlaySoundAsync(GetChannelVoice(channel));
            }, "播放支付开始语音失败");
        }

        /// <summary>
        /// 播放支付开始提示音（带“上次未付款”提示）
        /// </summary>
        public Task PlayPaymentStartedWithBeforeNotPayAsync(PaymentChannel channel, string? nickname = null, string? orderNumber = null)
        {
            if (ShouldSkipDuplicate(BuildVoiceKey("before_not_pay", orderNumber, channel.ToString())))
                return Task.CompletedTask;

            return EnqueuePlaybackAsync(async () =>
            {
                var channelVoice = GetChannelVoice(channel);

                await PlayRepeatedAsync(async () =>
                {
                    await SpeakNicknameAsync(nickname);
                    await _speechService.PlaySoundAsync(channelVoice);
                    await _speechService.PlaySoundAsync("before_not_pay");
                }, BeforeNotPayRepeatCount);
            }, "播放支付开始语音（带上次未付款）失败");
        }

        /// <summary>
        /// 播放支付取消提示音
        /// </summary>
        public Task PlayPaymentCancelledAsync(string? nickname = null, string? orderNumber = null)
        {
            MarkOrderCompleted(orderNumber);

            if (ShouldSkipDuplicate(BuildVoiceKey("cancel", orderNumber, nickname ?? string.Empty)))
                return Task.CompletedTask;

            return EnqueuePlaybackAsync(async () =>
            {
                await PlayRepeatedAsync(async () =>
                {
                    await SpeakNicknameAsync(nickname);
                    await _speechService.PlaySoundAsync("cancel_pay");
                }, CancelRepeatCount);
            }, "播放支付取消语音失败");
        }

        /// <summary>
        /// 播放支付成功语音（金额播报）
        /// </summary>
        public Task PlayPaymentSuccessAsync(decimal amount, PaymentChannel channel, string? orderNumber = null)
        {
            MarkOrderCompleted(orderNumber);

            if (ShouldSkipDuplicate(BuildVoiceKey("success", orderNumber, channel + ":" + amount.ToString("0.##"))))
                return Task.CompletedTask;

            return EnqueuePlaybackAsync(
                () => _speechService.PlayAmountAsync(amount, channel),
                "播放支付成功语音失败");
        }

        /// <summary>
        /// 播放“扫码未支付”提示音。
        /// 如果此提示在队列里等待期间订单已经成功/取消，则会自动跳过，避免过期语音误播。
        /// </summary>
        public Task PlayScanNotPayAsync(string? orderNumber = null)
        {
            if (IsOrderCompleted(orderNumber))
                return Task.CompletedTask;

            if (ShouldSkipDuplicate(BuildVoiceKey("scan_not_pay", orderNumber, string.Empty)))
                return Task.CompletedTask;

            return EnqueuePlaybackAsync(async () =>
            {
                if (IsOrderCompleted(orderNumber))
                {
                    _log.Info($"跳过过期扫码未支付语音，订单已完成: {orderNumber}");
                    return;
                }

                await _speechService.PlaySoundAsync("scan_not_pay");
            }, "播放扫码未支付语音失败");
        }

        private async Task EnqueuePlaybackAsync(Func<Task> playAction, string errorMessage)
        {
            await _playLock.WaitAsync();
            try
            {
                await playAction();
            }
            catch (Exception ex)
            {
                _log.Info($"{errorMessage}: {ex.Message}");
            }
            finally
            {
                _playLock.Release();
            }
        }

        private static async Task PlayRepeatedAsync(Func<Task> playOneRound, int repeatCount)
        {
            var safeRepeatCount = Math.Max(1, repeatCount);
            for (var i = 0; i < safeRepeatCount; i++)
            {
                await playOneRound();

                if (i < safeRepeatCount - 1)
                    await Task.Delay(RepeatIntervalMilliseconds);
            }
        }

        private Task SpeakNicknameAsync(string? nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname))
            {
                return Task.CompletedTask;
            }

            return _ttsService.SpeakAsync(nickname.Trim());
        }

        private void MarkOrderCompleted(string? orderNumber)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
                return;

            lock (_stateLock)
            {
                CleanupState_NoLock(DateTime.Now);
                _completedOrderTimes[orderNumber.Trim()] = DateTime.Now;
            }
        }

        private bool IsOrderCompleted(string? orderNumber)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
                return false;

            lock (_stateLock)
            {
                CleanupState_NoLock(DateTime.Now);
                return _completedOrderTimes.ContainsKey(orderNumber.Trim());
            }
        }

        private bool ShouldSkipDuplicate(string key)
        {
            var now = DateTime.Now;
            lock (_stateLock)
            {
                CleanupState_NoLock(now);

                if (_lastVoiceRequestTimes.TryGetValue(key, out var lastTime)
                    && now - lastTime < DuplicateWindow)
                {
                    _log.Info($"跳过重复语音请求: {key}");
                    return true;
                }

                _lastVoiceRequestTimes[key] = now;
                return false;
            }
        }

        private void CleanupState_NoLock(DateTime now)
        {
            foreach (var key in _lastVoiceRequestTimes
                         .Where(kv => now - kv.Value > DuplicateWindow)
                         .Select(kv => kv.Key)
                         .ToList())
            {
                _lastVoiceRequestTimes.Remove(key);
            }

            foreach (var key in _completedOrderTimes
                         .Where(kv => now - kv.Value > CompletedOrderRetention)
                         .Select(kv => kv.Key)
                         .ToList())
            {
                _completedOrderTimes.Remove(key);
            }
        }

        private static string BuildVoiceKey(string voiceType, string? orderNumber, string fallback)
        {
            var id = string.IsNullOrWhiteSpace(orderNumber) ? fallback : orderNumber.Trim();
            return $"{voiceType}:{id}";
        }

        /// <summary>
        /// 获取支付渠道对应的语音文件名
        /// </summary>
        private static string GetChannelVoice(PaymentChannel channel)
        {
            return channel == PaymentChannel.Alipay ? "alipay_pay" : "vx_pay";
        }
    }
}
