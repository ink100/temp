using HRB.Payment.Core.DtoModels;
using HRB.Payment.Core.Helpers;
using Lanymy.Common.Helpers;
using System.Diagnostics;
using System.IO;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 软件激活服务实现
    /// </summary>
    public class LicenseService : ILicenseService
    {
        /// <summary>
        /// 加密密钥（与 KeyTool 保持一致）
        /// </summary>
        private const string SECRET_KEY = EnvironmentSettings.SECRET_KEY;

        /// <summary>
        /// 激活文件名
        /// </summary>
        private const string LICENSE_FILE_NAME = EnvironmentSettings.LICENSE_FILE_FULL_NAME;

        private LicenseDto? _cachedLicense;
        private readonly string _licenseFilePath;

        /// <summary>
        /// 构造函数
        /// </summary>
        public LicenseService()
        {
            // 获取应用程序根目录
            //var rootPath = PathHelper.GetCallDomainPath();
            //_licenseFilePath = Path.Combine(rootPath, LICENSE_FILE_NAME);

            _licenseFilePath = EnvironmentSettings.LICENSE_FILE_FULL_PATH;
        }

        /// <summary>
        /// 获取当前激活信息
        /// </summary>
        /// <returns>激活信息，如果不存在或无效返回null</returns>
        public async Task<LicenseDto?> GetLicenseInfoAsync()
        {
            // 如果有缓存且有效，直接返回
            if (_cachedLicense != null)
            {
                return _cachedLicense;
            }

            // 从文件加载
            return await Task.Run(() => LoadLicenseFromFile());
        }

        /// <summary>
        /// 通过.key文件激活软件
        /// </summary>
        /// <param name="keyFilePath">.key文件的完整路径</param>
        /// <returns>激活结果（成功返回true和消息，失败返回false和错误信息）</returns>
        public async Task<(bool Success, string Message)> ActivateFromKeyFileAsync(string keyFilePath)
        {
            if (string.IsNullOrWhiteSpace(keyFilePath))
            {
                return (false, "请选择激活文件");
            }

            if (!File.Exists(keyFilePath))
            {
                return (false, "激活文件不存在");
            }

            try
            {

                // 读取并解密激活文件
                var bytes = await File.ReadAllBytesAsync(keyFilePath);
                var encryptModelDigestInfoModel = SecurityHelper.DecryptModelFromBytes<LicenseDto>(bytes, SECRET_KEY);
                var licenseDto = encryptModelDigestInfoModel.SourceModel;

                if (licenseDto == null)
                {
                    return (false, "激活文件格式错误");
                }

                // 验证激活信息
                var validationResult = ValidateLicenseDto(licenseDto);
                if (!validationResult.IsValid)
                {
                    return (false, validationResult.Message);
                }

                // 复制激活文件到应用程序根目录
                File.Copy(keyFilePath, _licenseFilePath, true);

                // 缓存激活信息
                _cachedLicense = licenseDto;

                var remainingDays = (licenseDto.MaxDateTime - DateTime.Now).Days;
                return (true, $"激活成功！\n到期时间：{licenseDto.MaxDateTime:yyyy-MM-dd HH:mm:ss}\n剩余天数：{remainingDays}天");
            }
            catch (Exception ex)
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Error($"激活失败: {ex.Message}");
                return (false, $"激活失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 验证激活状态
        /// </summary>
        /// <returns>是否有效激活（已激活且未过期）</returns>
        public async Task<bool> ValidateLicenseAsync()
        {
            var license = await GetLicenseInfoAsync();
            if (license == null)
            {
                return false;
            }

            // 验证激活信息
            var validationResult = ValidateLicenseDto(license);
            return validationResult.IsValid;
        }

        /// <summary>
        /// 获取当前机器的序列号（SN）
        /// </summary>
        /// <returns>机器序列号</returns>
        public string GetMachineSN()
        {
            return PcHelper.GetPhysicalSerialNumber();
            //return EnvironmentSettings.GetLocalKey();
        }


        /// <summary>
        /// 获取本地密钥
        /// </summary>
        /// <returns></returns>
        public string GetLocalKey()
        {
            return EnvironmentSettings.GetLocalKey();
        }


        /// <summary>
        /// 获取激活文件路径
        /// </summary>
        /// <returns>激活文件的完整路径</returns>
        public string GetLicenseFilePath()
        {
            return _licenseFilePath;
        }

        /// <summary>
        /// 检查激活文件是否存在
        /// </summary>
        /// <returns>激活文件是否存在</returns>
        public bool LicenseFileExists()
        {
            return File.Exists(_licenseFilePath);
        }

        #region 私有方法

        /// <summary>
        /// 从文件加载激活信息
        /// </summary>
        /// <returns>激活信息，如果加载失败返回null</returns>
        private LicenseDto? LoadLicenseFromFile()
        {
            try
            {
                if (!File.Exists(_licenseFilePath))
                {
                    return null;
                }

                // 读取并解密激活文件
                var bytes = File.ReadAllBytes(_licenseFilePath);
                var encryptModelDigestInfoModel = SecurityHelper.DecryptModelFromBytes<LicenseDto>(bytes, SECRET_KEY);
                var licenseDto = encryptModelDigestInfoModel.SourceModel;

                if (licenseDto == null)
                {
                    return null;
                }

                // 验证激活信息
                var validationResult = ValidateLicenseDto(licenseDto);
                if (!validationResult.IsValid)
                {
                    return null;
                }

                // 缓存激活信息
                _cachedLicense = licenseDto;
                return licenseDto;
            }
            catch (Exception ex)
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Error($"加载激活文件失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 验证激活信息
        /// </summary>
        /// <param name="licenseDto">激活信息</param>
        /// <returns>验证结果</returns>
        private (bool IsValid, string Message) ValidateLicenseDto(LicenseDto licenseDto)
        {
            // 1. 验证机器序列号是否匹配
            var currentSN = GetMachineSN();
            if (licenseDto.SN != currentSN)
            {
                return (false, "激活文件与当前机器不匹配");
            }

            // 2. 强制获取网络时间进行验证（防止修改系统时间）
            var netTime = PcHelper.GetNetNowTime();

            // 如果无法获取网络时间，验证失败
            if (netTime == DateTime.MinValue)
            {
                GlobalSettings.CurrentAppContext.CurrentLogger.Error("无法获取网络时间，验证失败");
                return (false, "无法连接网络获取时间，请连接网络后再使用");
            }

            GlobalSettings.CurrentAppContext.CurrentLogger.Info($"使用网络时间验证: {netTime:yyyy-MM-dd HH:mm:ss}");

            // 3. 验证是否过期
            if (netTime > licenseDto.MaxDateTime)
            {
                var expiredDays = (netTime - licenseDto.MaxDateTime).Days;
                return (false, $"激活已过期（过期{expiredDays}天）");
            }

            // 4. 验证激活时间是否合理（不能早于创建时间）
            if (netTime < licenseDto.KeyCreateDateTime)
            {
                return (false, "网络时间异常，请检查网络连接");
            }

            return (true, "激活有效");
        }

        #endregion
    }
}
