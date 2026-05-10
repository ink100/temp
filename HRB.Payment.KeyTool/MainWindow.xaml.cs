using HRB.Payment.Core.DtoModels;
using Lanymy.Common.ExtensionFunctions;
using Lanymy.Common.Helpers;
using Microsoft.Win32;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace HRB.Payment.KeyTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ShowMessage(string message)
        {

            Dispatcher.InvokeAsync(() =>
            {
                tbxLog.AppendText(string.Format("[ {0:yyyy-MM-dd HH:mm:ss.fff} ] - {1}", DateTime.Now, message) + Environment.NewLine);
                tbxLog.ScrollToEnd();
            });

        }

        private void BtnCreateKeyFile_OnClick(object sender, RoutedEventArgs e)
        {

            var clientKeyStr = tbxClientKey.Text.Trim();
            var validDaysStr = tbxValidDays.Text.Trim();

            if (clientKeyStr.IfIsNullOrEmpty() || validDaysStr.IfIsNullOrEmpty())
            {
                ShowMessage("请输入有效值!");
                return;
            }

            var licenseDto = EnvironmentSettings.GetLicenseDto(clientKeyStr);

            if (licenseDto.IfIsNull())
            {
                ShowMessage("请输入有效 密钥 值!");
                return;
            }

            var validDays = 0;
            if (!int.TryParse(validDaysStr, out validDays) || validDays <= 0)
            {
                ShowMessage("请输入有效 天数!");
                return;
            }

            var dialog = new OpenFolderDialog
            {
                Title = "选择保存激活文件目标文件夹",
                Multiselect = false,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
            };

            bool? result = dialog.ShowDialog();

            if (result == true)
            {

                var selectFolderFullPath = dialog.FolderName;
                var keyFileFullPath = Path.Combine(selectFolderFullPath, DateTime.Now.ToString("yyyyMMddHHmmss") + ".key");

                licenseDto.KeyCreateDateTime = DateTime.Now;
                licenseDto.MaxDateTime = licenseDto.KeyCreateDateTime.AddDays(validDays);


                EnvironmentSettings.SaveLicenseToFile(licenseDto, keyFileFullPath);

                ShowMessage(string.Format("密钥文件已生成 ,路径 [ {0} ]", keyFileFullPath));

            }
            else
            {
                ShowMessage("取消了文件夹选择!");
            }


        }



    }
}