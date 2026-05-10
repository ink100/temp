using HRB.Payment.Core.DtoModels;
using HRB.Payment.Core.Helpers;
using Lanymy.Common.ExtensionFunctions;
using Lanymy.Common.Helpers;
using Lanymy.Common.Instruments.CryptoModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace HRB.Platform.Client.WPF.PaymentAppModule.Core
{

    internal class EnvironmentSettings
    {

        public const string SECRET_KEY = "AD9WYC8V9P3B7FYH";

        public const string LICENSE_FILE_FULL_NAME = "license.key";

        public const string DB_SECRET_KEY = "604QBB4FPKXW66KH";

        /// <summary>
        /// 用户协议版本号 - 更新此版本号将要求用户重新同意条款
        /// </summary>
        public const string USER_AGREEMENT_VERSION = "V1.0";


        //public const string VX_PROCESS_NAME = "WeChat.exe";
        //public const string VX_PROCESS_NAME = "WeChat";
        //public const string VX_VERSION = "3.9.12.55";



        public const string VX_PROCESS_NAME = "WeChat";
        public const string VX_EXE_FILE_FULL_NAME = VX_PROCESS_NAME + ".exe";
        public const string VX_VERSION = "3.9.12.54";
        public const string VX_HOME_ROOT_DIRECTORY_NAME = "WeChatHome";
        public const string VX_ROOT_DIRECTORY_NAME = VX_PROCESS_NAME;
        public const string VX_SHADOW_ROOT_DIRECTORY_NAME = VX_ROOT_DIRECTORY_NAME + "Shadow";
        public const string VX_START_FILE_FULL_NAME = "start.bat";
        public const string VX_ZIP_FILE_FULL_NAME = VX_PROCESS_NAME + ".zip";


        public static readonly string VX_HOME_ROOT_DIRECTORY_FULL_PATH;
        public static readonly string VX_ROOT_DIRECTORY_FULL_PATH;
        public static readonly string VX_ROOT_DIRECTORY_EXE_FILE_FULL_PATH;
        public static readonly string VX_SHADOW_ROOT_DIRECTORY_FULL_PATH;
        public static readonly string VX_SHADOW_ROOT_DIRECTORY_EXE_FILE_FULL_PATH;
        public static string VX_START_FILE_FULL_PATH { get; internal set; }
        public static readonly string VX_ZIP_FILE_FULL_PATH;





        //private const string LICENSE_FILE_FULL_NAME = "db.db";

        public static readonly string ROOT_DIRECTORY_FULL_PATH;
        public static readonly string LICENSE_FILE_FULL_PATH;


        static EnvironmentSettings()
        {

            ROOT_DIRECTORY_FULL_PATH = PathHelper.GetCallDomainPath();

            //LICENSE_FILE_FULL_PATH = Path.Combine(ROOT_DIRECTORY_FULL_PATH, LICENSE_FILE_FULL_NAME);
            LICENSE_FILE_FULL_PATH = Path.Combine(PathHelper.CombineRelativePath(ROOT_DIRECTORY_FULL_PATH, "../"), LICENSE_FILE_FULL_NAME);


            VX_HOME_ROOT_DIRECTORY_FULL_PATH = Path.Combine(PathHelper.CombineRelativePath(ROOT_DIRECTORY_FULL_PATH, "../"), VX_HOME_ROOT_DIRECTORY_NAME);

            VX_ZIP_FILE_FULL_PATH = Path.Combine(VX_HOME_ROOT_DIRECTORY_FULL_PATH, VX_ZIP_FILE_FULL_NAME);
            VX_ROOT_DIRECTORY_FULL_PATH = Path.Combine(VX_HOME_ROOT_DIRECTORY_FULL_PATH, VX_ROOT_DIRECTORY_NAME);
            VX_ROOT_DIRECTORY_EXE_FILE_FULL_PATH = Path.Combine(VX_ROOT_DIRECTORY_FULL_PATH, VX_EXE_FILE_FULL_NAME);

            VX_SHADOW_ROOT_DIRECTORY_FULL_PATH = Path.Combine(VX_HOME_ROOT_DIRECTORY_FULL_PATH, VX_SHADOW_ROOT_DIRECTORY_NAME);
            VX_SHADOW_ROOT_DIRECTORY_EXE_FILE_FULL_PATH = Path.Combine(VX_SHADOW_ROOT_DIRECTORY_FULL_PATH, VX_EXE_FILE_FULL_NAME);


            //VX_START_FILE_FULL_PATH = Path.Combine(VX_ROOT_DIRECTORY_FULL_PATH, VX_START_FILE_FULL_NAME);


        }



        public static LicenseDto GetLicenseDto()
        {
            LicenseDto licenseDto = null;

            if (File.Exists(LICENSE_FILE_FULL_PATH))
            {
                byte[] bytes = File.ReadAllBytes(LICENSE_FILE_FULL_PATH);
                EncryptModelDigestInfoModel<LicenseDto>? encryptModelDigestInfoModel = SecurityHelper.DecryptModelFromBytes<LicenseDto>(bytes, SECRET_KEY);
                licenseDto = encryptModelDigestInfoModel.SourceModel;
            }


            return licenseDto;
        }


        public static string GetLocalKey()
        {
            LicenseDto licenseDto = new() { CreateDateTime = DateTime.Now, SN = PcHelper.GetPhysicalSerialNumber(), };

            EncryptModelDigestInfoModel<LicenseDto>? encryptModelDigestInfoModel = SecurityHelper.EncryptModelToBase64String(licenseDto, SECRET_KEY);

            return encryptModelDigestInfoModel.EncryptedBase64String;
        }
    }
}