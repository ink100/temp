using System.IO;
using System.Windows.Input;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.ViewModels
{
    /// <summary>
    /// 用户协议对话框 ViewModel
    /// </summary>
    public class UserAgreementDialogViewModel : BindableBase, IDialogAware
    {
        private string _title = "免责条款与知情同意书";
        private string _agreementContent = string.Empty;

        /// <summary>
        /// 标题
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// 协议内容
        /// </summary>
        public string AgreementContent
        {
            get => _agreementContent;
            set => SetProperty(ref _agreementContent, value);
        }

        /// <summary>
        /// 同意命令
        /// </summary>
        public ICommand AgreeCommand { get; }

        /// <summary>
        /// 拒绝命令
        /// </summary>
        public ICommand DeclineCommand { get; }

        public DialogCloseListener RequestClose { get; }

        public UserAgreementDialogViewModel()
        {
            AgreeCommand = new DelegateCommand(OnAgree);
            DeclineCommand = new DelegateCommand(OnDecline);

            LoadAgreementContent();
        }

        public bool CanCloseDialog() => true;

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.ContainsKey("Title"))
                Title = parameters.GetValue<string>("Title");

            if (parameters.ContainsKey("Content"))
                AgreementContent = parameters.GetValue<string>("Content");
        }

        private void OnAgree()
        {
            RequestClose.Invoke(new DialogResult(ButtonResult.OK));
        }

        private void OnDecline()
        {
            RequestClose.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        /// <summary>
        /// 加载协议内容
        /// </summary>
        private void LoadAgreementContent()
        {
            try
            {
                // 获取应用程序根目录
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string agreementFilePath = Path.Combine(appDirectory, "免责条款与知情同意书.md");

                if (File.Exists(agreementFilePath))
                {
                    AgreementContent = File.ReadAllText(agreementFilePath);
                }
                else
                {
                    AgreementContent = "未找到免责条款文件。";
                }
            }
            catch (Exception ex)
            {
                AgreementContent = $"加载免责条款失败：{ex.Message}";
            }
        }
    }
}
