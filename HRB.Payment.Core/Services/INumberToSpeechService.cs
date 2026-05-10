using HRB.Payment.Core.Models;

namespace HRB.Payment.Core.Services
{
    /// <summary>
    /// 数字转语音播放服务接口
    /// </summary>
    public interface INumberToSpeechService
    {
        /// <summary>
        /// 播放速度倍数（1.0为正常速度，大于1.0为加速，小于1.0为减速）
        /// </summary>
        double SpeedRatio { get; set; }

        /// <summary>
        /// 播放金额语音
        /// </summary>
        /// <param name="amount">金额（单位：元）</param>
        /// <param name="paymentChannel">支付渠道</param>
        Task PlayAmountAsync(decimal amount, PaymentChannel paymentChannel);

        /// <summary>
        /// 播放单个音频文件
        /// </summary>
        /// <param name="soundName">音频文件名（不含扩展名）</param>
        Task PlaySoundAsync(string soundName);
    }
}

