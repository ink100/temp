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

        private static readonly TimeSpan DuplicateWindow = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan CompletedOrderRetention = TimeSpan.FromMinutes(10);

        private readonly INumberToSpeechService _speechService;
        private readonly ITtsService _ttsService;
        private readonly IHrbLogger _log;

        private readonly SemaphoreSlim _playLock = new(1, 1);
        private readonly object _stateLock = new();
        private readonly Dictionary<string, DateTime> _lastVoiceRequestTimes = new();
        private readonly Dictionary<string, DateTime> _completedOrderTimes = new();

        /// <summary>
        /// 本地提示音文件名 → 中文文本映射，用于在线全量 TTS 模式。
        /// </summary>
        private static readonly Dictionary<string, string> SoundTextMap = new()
        {
            { "alipay_pay", "支付宝收款" },
            { "vx_pay", "微信收款" },
            { "scan_not_pay", "扫码未支付" },
            { "cancel_pay", "支付已取消" },
            { "before_not_pay", "上次未付款" },
            { "zfb_pay", "支付宝收款" },
            { "wechat_pay", "微信收款" },
        };

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
                if (IsOnlineSpeechEnabled())
                    await SpeakOnlineSoundAsync(GetChannelVoice(channel));
                else
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
                    if (IsOnlineSpeechEnabled())
                    {
                        await SpeakOnlineSoundAsync(channelVoice);
                        await SpeakOnlineSoundAsync("before_not_pay");
                    }
                    else
                    {
                        await _speechService.PlaySoundAsync(channelVoice);
                        await _speechService.PlaySoundAsync("before_not_pay");
                    }
                }, GetPriorUnpaidRepeatCount());
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
                    if (IsOnlineSpeechEnabled())
                        await SpeakOnlineSoundAsync("cancel_pay");
                    else
                        await _speechService.PlaySoundAsync("cancel_pay");
                }, GetPaymentCancelledRepeatCount());
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
                () => IsOnlineSpeechEnabled()
                    ? SpeakOnlineAmountAsync(amount, channel)
                    : _speechService.PlayAmountAsync(amount, channel),
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

                if (IsOnlineSpeechEnabled())
                    await SpeakOnlineSoundAsync("scan_not_pay");
                else
                    await _speechService.PlaySoundAsync("scan_not_pay");
            }, "播放扫码未支付语音失败");
        }

        private async Task EnqueuePlaybackAsync(Func<Task> playAction, string errorMessage)
        {
            await _playLock.WaitAsync();
            try
            {
                ApplySpeechSpeed();
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
        private void ApplySpeechSpeed()
        {
            var settings = GlobalSettings.CurrentAppContext.CurrentSettings;
            var rate = Math.Clamp(settings.TtsRate <= 0 ? 50 : settings.TtsRate, 0, 100);

         
            // 0 = 慢速，大约 0.6 倍速
            // 50 = 正常语速，1.0 倍速
            // 100 = 最快，大约 2.0 倍速
            var speedRatio = rate <= 50
                ? 0.6 + rate * 0.008
                : 1.0 + (rate - 50) / 50.0;

            _speechService.SpeedRatio = speedRatio;
        }

        private async Task PlayRepeatedAsync(Func<Task> playOneRound, int repeatCount)
        {
            var safeRepeatCount = Math.Clamp(repeatCount, 1, 20);
            var intervalMilliseconds = GetRepeatIntervalMilliseconds();

            for (var i = 0; i < safeRepeatCount; i++)
            {
                await playOneRound();

                if (i < safeRepeatCount - 1)
                    await Task.Delay(intervalMilliseconds);
            }
        }

        private int GetRepeatIntervalMilliseconds()
        {
            var settings = GlobalSettings.CurrentAppContext.CurrentSettings;
            var seconds = settings.VoiceRepeatIntervalSeconds <= 0 ? 1 : settings.VoiceRepeatIntervalSeconds;
            return Math.Clamp(seconds, 1, 30) * 1000;
        }

        private int GetPriorUnpaidRepeatCount()
        {
            var settings = GlobalSettings.CurrentAppContext.CurrentSettings;
            var count = settings.PriorUnpaidVoiceRepeatCount <= 0 ? 1 : settings.PriorUnpaidVoiceRepeatCount;
            return Math.Clamp(count, 1, 20);
        }

        private int GetPaymentCancelledRepeatCount()
        {
            var settings = GlobalSettings.CurrentAppContext.CurrentSettings;
            var count = settings.PaymentCancelledVoiceRepeatCount <= 0 ? 1 : settings.PaymentCancelledVoiceRepeatCount;
            return Math.Clamp(count, 1, 20);
        }

        private async Task SpeakNicknameAsync(string? nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname))
                return;

            try
            {
                // Edge TTS 是在线 TTS，网络慢时不能阻塞核心收款提示音。
                // 2 秒内没有播出来，就跳过昵称，继续播放本地提示音。
                await _ttsService
                    .SpeakAsync(nickname.Trim())
                    .WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch (TimeoutException)
            {
                _log.Info($"昵称 TTS 播报超时，已跳过昵称: {nickname}");
            }
            catch (Exception ex)
            {
                _log.Info($"昵称 TTS 播报失败，已跳过昵称: {ex.Message}");
            }
        }

        /// <summary>
        /// 在线全量 TTS 模式下，将提示音文件名映射为中文文本并通过 Edge TTS 播报。
        /// </summary>
        private async Task SpeakOnlineSoundAsync(string soundName)
        {
            if (!SoundTextMap.TryGetValue(soundName, out var text))
            {
                _log.Info($"未找到在线语音映射: {soundName}");
                return;
            }

            await _ttsService.SpeakFullTextAsync(text);
        }

        /// <summary>
        /// 在线全量 TTS 模式下，播报收款金额。
        /// 格式："支付宝收款12.34元" / "微信收款12.34元"
        /// </summary>
        private async Task SpeakOnlineAmountAsync(decimal amount, PaymentChannel channel)
        {
            var channelText = channel == PaymentChannel.Alipay ? "支付宝收款" : "微信收款";
            var text = $"{channelText}{amount:0.##}元";
            await _ttsService.SpeakFullTextAsync(text);
        }

        /// <summary>
        /// 是否启用了在线全量 TTS 语音播报模式。
        /// </summary>
        private bool IsOnlineSpeechEnabled()
        {
            try { return GlobalSettings.CurrentAppContext.CurrentSettings.IsUseOnlineTtsSpeech; }
            catch { return false; }
        }

        public void MarkOrderCompleted(string? orderNumber)
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
