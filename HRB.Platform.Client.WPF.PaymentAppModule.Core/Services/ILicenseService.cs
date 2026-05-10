using HRB.Payment.Core.DtoModels;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 软件激活服务接口
    /// </summary>
    public interface ILicenseService
    {
        /// <summary>
        /// 获取当前激活信息
        /// </summary>
        /// <returns>激活信息，如果不存在或无效返回null</returns>
        Task<LicenseDto?> GetLicenseInfoAsync();

        /// <summary>
        /// 通过.key文件激活软件
        /// </summary>
        /// <param name="keyFilePath">.key文件的完整路径</param>
        /// <returns>激活结果（成功返回true和消息，失败返回false和错误信息）</returns>
        Task<(bool Success, string Message)> ActivateFromKeyFileAsync(string keyFilePath);

        /// <summary>
        /// 验证激活状态
        /// </summary>
        /// <returns>是否有效激活（已激活且未过期）</returns>
        Task<bool> ValidateLicenseAsync();

        /// <summary>
        /// 获取当前机器的序列号（SN）
        /// </summary>
        /// <returns>机器序列号</returns>
        string GetMachineSN();

        /// <summary>
        /// 获取激活文件路径
        /// </summary>
        /// <returns>激活文件的完整路径</returns>
        string GetLicenseFilePath();

        /// <summary>
        /// 检查激活文件是否存在
        /// </summary>
        /// <returns>激活文件是否存在</returns>
        bool LicenseFileExists();

        /// <summary>
        /// 获取本地密钥
        /// </summary>
        /// <returns></returns>
        string GetLocalKey();

    }
}
