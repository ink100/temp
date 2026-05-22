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
            Edge_tts.Await = true;
            ApplySettings(hint);
            var play = Edge_tts.GetPlayer(_option, GetVoice());
            await play.PlayAsync();
        }

        private void ApplySettings(string hint)
        {
            var settings = GlobalSettings.CurrentAppContext.CurrentSettings;
            _option.Text = CleanTextChineseOnly(hint);
            _option.Rate = Math.Clamp(settings.TtsRate, -50, 100);
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
