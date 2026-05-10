using HRB.Payment.Core.Models;
using HRB.Platform.Client.Core.Interfaces;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HRB.Payment.Core.Services
{
    /// <summary>
    /// 数字转语音播放服务
    /// </summary>
    public class NumberToSpeechService : INumberToSpeechService
    {
        /// <summary>
        /// 播放速度倍数（1.0为正常速度，大于1.0为加速，小于1.0为减速）
        /// </summary>
        public double SpeedRatio { get; set; } = 1.0;

        /// <summary>
        /// 音频文件缓存（soundName -> byte[]）
        /// </summary>
        private readonly Dictionary<string, byte[]> _audioCache = new();

        /// <summary>
        /// 缓存锁
        /// </summary>
        private readonly object _cacheLock = new();

        private readonly Dictionary<int, string> _digitSounds = new()
        {
            { 0, "zero" },
            { 1, "one" },
            { 2, "two" },
            { 3, "three" },
            { 4, "four" },
            { 5, "five" },
            { 6, "six" },
            { 7, "seven" },
            { 8, "eight" },
            { 9, "nine" }
        };

        private readonly Dictionary<int, string> _unitSounds = new()
        {
            { 10, "ten" },
            { 100, "hundred" },
            { 1000, "thousand" },
            { 10000, "ten_thousand" },
            { 100000000, "one_bilion" }
        };

        private const string YuanSound = "yuan";
        private const string DotSound = "dot";
        private const string ZfbPaySound = "zfb_pay";
        private const string WechatPaySound = "wechat_pay";

        /// <summary>
        /// 播放金额语音
        /// </summary>
        /// <param name="amount">金额（单位：元）</param>
        /// <param name="paymentChannel">支付渠道</param>
        public async Task PlayAmountAsync(decimal amount, PaymentChannel paymentChannel)
        {
            if (amount < 0)
            {
                throw new ArgumentException("金额不能为负数", nameof(amount));
            }

            if (amount == 0)
            {
                //await PlaySoundAsync("zero");
                //await PlaySoundAsync(YuanSound);
                return;
            }

            var soundFiles = new List<string>();

            // 分离整数部分和小数部分
            var integerPart = (long)Math.Truncate(amount);
            var decimalPart = amount - integerPart;

            // 处理整数部分
            if (integerPart > 0)
            {
                soundFiles.AddRange(ConvertAmountToSounds(integerPart));
            }
            else
            {
                // 如果整数部分为0，需要读"零"
                soundFiles.Add("zero");
            }

            // 处理小数部分
            if (decimalPart > 0)
            {
                // 添加小数点
                soundFiles.Add(DotSound);
                
                // 将小数部分转换为字符串，逐位读取
                var amountStr = amount.ToString("G");
                if (amountStr.Contains('.') || amountStr.Contains('。'))
                {
                    var parts = amountStr.Split(new[] { '.', '。' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        var decimalDigits = parts[1].TrimEnd('0'); // 移除末尾的0
                        foreach (var digitChar in decimalDigits)
                        {
                            if (int.TryParse(digitChar.ToString(), out var digit))
                            {
                                soundFiles.Add(_digitSounds[digit]);
                            }
                        }
                    }
                }
            }

            // 添加"元"
            soundFiles.Add(YuanSound);

            // 在金额前添加支付方式音频
            var finalSoundFiles = new List<string>();
            
            // 先播放支付方式
            if (paymentChannel == PaymentChannel.Alipay)
            {
                finalSoundFiles.Add(ZfbPaySound);
            }
            else if (paymentChannel == PaymentChannel.WeChat)
            {
                finalSoundFiles.Add(WechatPaySound);
            }
            
            // 再播放金额
            finalSoundFiles.AddRange(soundFiles);

            // 播放所有音频
            foreach (var soundFile in finalSoundFiles)
            {
                await PlaySoundAsync(soundFile);
            }
        }

        /// <summary>
        /// 将金额（元）转换为音频文件列表
        /// </summary>
        private List<string> ConvertAmountToSounds(long amount)
        {
            var sounds = new List<string>();

            // 处理亿位
            if (amount >= 100000000)
            {
                var yi = amount / 100000000;
                sounds.AddRange(ConvertNumberToSounds(yi));
                sounds.Add(_unitSounds[100000000]);
                amount %= 100000000;
            }

            // 处理万位
            if (amount >= 10000)
            {
                var wan = amount / 10000;
                sounds.AddRange(ConvertNumberToSounds(wan));
                sounds.Add(_unitSounds[10000]);
                amount %= 10000;
            }

            // 处理千位及以下
            if (amount > 0)
            {
                sounds.AddRange(ConvertNumberToSounds(amount));
            }

            return sounds;
        }

        /// <summary>
        /// 将0-9999的数字转换为音频文件列表
        /// </summary>
        private List<string> ConvertNumberToSounds(long number)
        {
            var sounds = new List<string>();

            if (number == 0)
            {
                sounds.Add(_digitSounds[0]);
                return sounds;
            }

            // 处理千位
            if (number >= 1000)
            {
                var qian = number / 1000;
                if (qian > 0)
                {
                    sounds.Add(_digitSounds[(int)qian]);
                    sounds.Add(_unitSounds[1000]);
                }
                number %= 1000;
            }

            // 处理百位
            if (number >= 100)
            {
                var bai = number / 100;
                if (bai > 0)
                {
                    sounds.Add(_digitSounds[(int)bai]);
                    sounds.Add(_unitSounds[100]);
                }
                number %= 100;
            }

            // 处理十位和个位
            if (number >= 10)
            {
                var shi = number / 10;
                if (shi > 0)
                {
                    // 如果十位是1，且前面没有其他数字，读"十"而不是"一十"
                    if (shi == 1 && sounds.Count == 0)
                    {
                        sounds.Add(_unitSounds[10]);
                    }
                    else
                    {
                        sounds.Add(_digitSounds[(int)shi]);
                        sounds.Add(_unitSounds[10]);
                    }
                }
                number %= 10;
            }

            // 处理个位
            if (number > 0)
            {
                sounds.Add(_digitSounds[(int)number]);
            }

            return sounds;
        }

        /// <summary>
        /// 播放单个音频文件
        /// </summary>
        /// <param name="soundName">音频文件名（不含扩展名）</param>
        public async Task PlaySoundAsync(string soundName)
        {
            try
            {
                // 从缓存获取或加载音频数据
                byte[] audioData;
                lock (_cacheLock)
                {
                    if (!_audioCache.TryGetValue(soundName, out audioData!))
                    {
                        // 缓存未命中，加载音频文件
                        var uri = new Uri($"pack://application:,,,/HRB.Payment.Core;component/Assets/Sound/{soundName}.mp3", UriKind.Absolute);
                        var resourceInfo = Application.GetResourceStream(uri);

                        if (resourceInfo == null || resourceInfo.Stream == null)
                        {
                            // 如果找不到资源，静默失败
                            return;
                        }

                        // 读取到字节数组并缓存
                        using var ms = new MemoryStream();
                        resourceInfo.Stream.CopyTo(ms);
                        audioData = ms.ToArray();
                        _audioCache[soundName] = audioData;
                    }
                }

                // 从缓存的字节数组创建内存流
                using var memoryStream = new MemoryStream(audioData);

                // 使用 NAudio 播放
                WaveStream? reader = null;
                try
                {
                    // 尝试作为 MP3 读取
                    reader = new Mp3FileReader(memoryStream);
                }
                catch (FormatException)
                {
                    // 如果 MP3 读取失败，尝试作为 WAV 读取
                    memoryStream.Position = 0;
                    try
                    {
                        reader = new WaveFileReader(memoryStream);
                    }
                    catch
                    {
                        // 如果都失败了，静默返回
                        return;
                    }
                }

                if (reader == null)
                {
                    return;
                }

                using (reader)
                {
                    // 如果需要变速，使用重采样（会改变音调，但实现简单可靠）
                    IWaveProvider waveProvider = reader;
                    MediaFoundationResampler? resampler = null;

                    if (Math.Abs(SpeedRatio - 1.0) > 0.01)
                    {
                        // 通过改变采样率实现变速
                        var outFormat = new WaveFormat(
                            (int)(reader.WaveFormat.SampleRate * SpeedRatio),
                            reader.WaveFormat.BitsPerSample,
                            reader.WaveFormat.Channels);

                        resampler = new MediaFoundationResampler(reader, outFormat);
                        waveProvider = resampler;
                    }

                    // 创建播放设备
                    using var outputDevice = new WaveOutEvent();
                    outputDevice.Init(waveProvider);

                    // 创建完成信号
                    var tcs = new TaskCompletionSource<bool>();
                    outputDevice.PlaybackStopped += (s, e) =>
                    {
                        if (e.Exception != null)
                        {
                            tcs.TrySetException(e.Exception);
                        }
                        else
                        {
                            tcs.TrySetResult(true);
                        }
                    };

                    // 开始播放
                    outputDevice.Play();

                    // 等待播放完成
                    await tcs.Task;

                    // 清理重采样器
                    resampler?.Dispose();
                }
            }
            catch
            {
                // 静默处理错误，避免影响主流程
            }
        }
    }



}

