using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// TTS服务
    /// </summary>
    public interface ITtsService
    {
        Task SpeakAsync(string hint);

        void Speak(string hint);

        /// <summary>
        /// 异步播报完整文本（保留数字和标点，不做汉字过滤），用于全量在线 TTS 模式。
        /// </summary>
        Task SpeakFullTextAsync(string text);

        ///// <summary>
        ///// 播报（只保留汉字）
        ///// </summary>
        //void SpeakChineseOnly(string hint);

        ///// <summary>
        ///// 异步播报（只保留汉字）
        ///// </summary>
        //Task SpeakChineseOnlyAsync(string hint);

    }
}
