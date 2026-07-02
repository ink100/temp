using Edge_tts_sharp;
using Edge_tts_sharp.Model;
using System.Text.RegularExpressions;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// TTS服务
    /// </summary>
    public class TtsService : ITtsService
    {
        private eVoice? _voice;
        private string _cachedVoiceName = string.Empty;

        private readonly PlayOption _option = new()
        {
            Rate = 0,
            Volume = 1
        };

        public void Speak(string hint)
        {
            Edge_tts.Await = false;
            ApplySettings(hint);
            var player = Edge_tts.GetPlayer(_option, GetVoice());
            player.Play();
        }

        public async Task SpeakAsync(string hint)
        {
            // Edge TTS PlayAsync 并非真异步，会阻塞调用线程。
            // 用 Task.Run 丢到后台线程执行，避免卡死 WPF UI 消息泵。
            await Task.Run(() =>
            {
                Edge_tts.Await = false;
                ApplySettings(hint);
                var play = Edge_tts.GetPlayer(_option, GetVoice());
                play.Play();
            });
        }

        /// <summary>
        /// 异步播报完整文本（保留数字和标点），用于全量在线 TTS 模式。
        /// 与 SpeakAsync 的区别：不做汉字过滤，允许数字/小数点等。
        /// </summary>
        public async Task SpeakFullTextAsync(string text)
        {
            await Task.Run(() =>
            {
                Edge_tts.Await = false;
                ApplyFullSettings(text);
                var play = Edge_tts.GetPlayer(_option, GetVoice());
                play.Play();
            });
        }

        /// <inheritdoc />
        

      

        // TtsService.cs
        public async Task<bool> SaveToFileAsync(string text, string filePath)
        {
            Edge_tts.Await = true;      // 阻塞等待 WebSocket 返回
            try
            {
                await Task.Run(() =>     // 丢线程池，不阻塞调用方线程
                {
                    var option = new PlayOption
                    {
                        Text = CleanTextChineseOnly(text),
                        Rate = 0,
                        Volume = 1,
                        SavePath = filePath   // ← 库的 OnClose 回调写入这里
                    };
                    Edge_tts.SaveAudio(option, GetVoice());
                });
                return true;             // 文件已写入
            }
            catch { return false; }     // 网络/WebSocket 异常 → false
        }


        private void ApplySettings(string hint)
        {
            var settings = GlobalSettings.CurrentAppContext.CurrentSettings;
            _option.Text = CleanTextChineseOnly(hint);
            var uiRate = Math.Clamp(settings.TtsRate, 0, 100);

            // 界面语速是 0% - 100%，其中 50% 表示正常音速。
            // Edge TTS 的 Rate 使用 -50 到 100：
            // 0%   -> -50（慢）
            // 50%  -> 0（正常）
            // 100% -> 100（快）
            _option.Rate = uiRate <= 50
                ? uiRate - 50
                : (int)Math.Round((uiRate - 50) * 2.0);
            _option.Volume = (float)(Math.Clamp(settings.TtsVolume, 0, 100) / 100.0);
        }

        private void ApplyFullSettings(string text)
        {
            var settings = GlobalSettings.CurrentAppContext.CurrentSettings;
            _option.Text = text; // 不过滤，保留数字和标点
            var uiRate = Math.Clamp(settings.TtsRate, 0, 100);

            _option.Rate = uiRate <= 50
                ? uiRate - 50
                : (int)Math.Round((uiRate - 50) * 2.0);
            _option.Volume = (float)(Math.Clamp(settings.TtsVolume, 0, 100) / 100.0);
        }

        private string CleanTextChineseOnly(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            text = Regex.Replace(text, @"[^一-龥]", "");
            return text;
        }

        private static string GetVoiceDisplayName(eVoice voice)
        {
            var type = voice.GetType();
            return type.GetProperty("Name")?.GetValue(voice)?.ToString()
                ?? type.GetProperty("ShortName")?.GetValue(voice)?.ToString()
                ?? type.GetProperty("FriendlyName")?.GetValue(voice)?.ToString()
                ?? voice.ToString()
                ?? string.Empty;
        }

        private eVoice GetVoice()
        {
            var settings = GlobalSettings.CurrentAppContext.CurrentSettings;
            var configuredVoiceName = settings.TtsVoiceName?.Trim() ?? string.Empty;

            if (_voice != null && string.Equals(_cachedVoiceName, configuredVoiceName, StringComparison.OrdinalIgnoreCase))
                return _voice;

            var voices = Edge_tts.GetVoice();

            if (!string.IsNullOrWhiteSpace(configuredVoiceName))
            {
                _voice = voices.FirstOrDefault(c => string.Equals(GetVoiceDisplayName(c), configuredVoiceName, StringComparison.OrdinalIgnoreCase));
            }

            _voice ??= voices.FirstOrDefault(c => c.Locale == "zh-CN") ?? voices.First();
            _cachedVoiceName = configuredVoiceName;
            return _voice;
        }
    }
}
