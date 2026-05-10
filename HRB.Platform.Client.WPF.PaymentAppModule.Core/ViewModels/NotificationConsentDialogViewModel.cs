using Prism.Commands;
using Prism.Mvvm;
using Prism.Dialogs;
using System;
using System.IO;
using System.Windows.Input;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.ViewModels
{
    /// <summary>
    /// 消息通知知情同意书对话框 ViewModel
    /// </summary>
    public class NotificationConsentDialogViewModel : BindableBase, IDialogAware
    {
        private string _title = "支付消息通知服务知情同意书";
        private string _consentContent = string.Empty;
        private bool _isReadOnlyMode = false;

        /// <summary>
        /// 标题
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// 知情同意书内容
        /// </summary>
        public string ConsentContent
        {
            get => _consentContent;
            set => SetProperty(ref _consentContent, value);
        }

        /// <summary>
        /// 是否为只读模式（只显示确认按钮）
        /// </summary>
        public bool IsReadOnlyMode
        {
            get => _isReadOnlyMode;
            set => SetProperty(ref _isReadOnlyMode, value);
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

        public NotificationConsentDialogViewModel()
        {
            AgreeCommand = new DelegateCommand(OnAgree);
            DeclineCommand = new DelegateCommand(OnDecline);

            LoadConsentContent();
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
                ConsentContent = parameters.GetValue<string>("Content");

            if (parameters.ContainsKey("IsReadOnlyMode"))
                IsReadOnlyMode = parameters.GetValue<bool>("IsReadOnlyMode");

            // 如果指定了文件路径，则加载该文件
            if (parameters.ContainsKey("FilePath"))
            {
                string filePath = parameters.GetValue<string>("FilePath");
                LoadConsentContentFromFile(filePath);
            }
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
        /// 加载知情同意书内容
        /// </summary>
        private void LoadConsentContent()
        {
            try
            {
                // 获取应用程序根目录
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string consentFilePath = Path.Combine(appDirectory, "支付消息通知服务知情同意书.md");

                if (File.Exists(consentFilePath))
                {
                    ConsentContent = File.ReadAllText(consentFilePath);
                }
                else
                {
                    ConsentContent = "未找到知情同意书文件。";
                }
            }
            catch (Exception ex)
            {
                ConsentContent = $"加载知情同意书失败：{ex.Message}";
            }
        }

        /// <summary>
        /// 从指定文件加载知情同意书内容
        /// </summary>
        private void LoadConsentContentFromFile(string fileName)
        {
            try
            {
                // 获取应用程序根目录
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string consentFilePath = Path.Combine(appDirectory, fileName);

                if (File.Exists(consentFilePath))
                {
                    ConsentContent = File.ReadAllText(consentFilePath);
                }
                else
                {
                    ConsentContent = "未找到知情同意书文件。";
                }
            }
            catch (Exception ex)
            {
                ConsentContent = $"加载知情同意书失败：{ex.Message}";
            }
        }
    }
}
