using HRB.Payment.Core.DtoModels;
using HRB.Payment.Core.Helpers;
using Lanymy.Common.Helpers;
using System.IO;

namespace HRB.Payment.KeyTool
{

    internal class EnvironmentSettings
    {


        public const string SECRET_KEY = "AD9WYC8V9P3B7FYH";


        public static readonly string ROOT_DIRECTORY_FULL_PATH;


        static EnvironmentSettings()
        {

            ROOT_DIRECTORY_FULL_PATH = PathHelper.GetCallDomainPath();

        }



        public static LicenseDto GetLicenseDto(string key)
        {
            try
            {

                var encryptModelDigestInfoModel = SecurityHelper.DecryptModelFromBase64String<LicenseDto>(key, SECRET_KEY);
                return encryptModelDigestInfoModel.SourceModel;

            }
            catch (Exception e)
            {
                return null;
            }

        }


        public static void SaveLicenseToFile(LicenseDto licenseDto, string keyFileFullPath)
        {

            var encryptModelDigestInfoModel = SecurityHelper.EncryptModelToBytes(licenseDto, SECRET_KEY);
            var bytes = encryptModelDigestInfoModel.EncryptedBytes;
            File.WriteAllBytes(keyFileFullPath, bytes);

        }


        public static string GetLocalKey()
        {

            var licenseDto = new LicenseDto
            {
                CreateDateTime = DateTime.Now,
                SN = PcHelper.GetPhysicalSerialNumber(),
            };

            var encryptModelDigestInfoModel = SecurityHelper.EncryptModelToBase64String(licenseDto, SECRET_KEY);

            return encryptModelDigestInfoModel.EncryptedBase64String;

        }


    }

}
