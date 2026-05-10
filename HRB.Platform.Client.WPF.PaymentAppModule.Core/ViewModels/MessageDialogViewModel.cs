using Prism.Commands;
using Prism.Mvvm;
using Prism.Dialogs;
using System;
using System.Windows.Input;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.ViewModels
{
    /// <summary>
    /// 消息对话框 ViewModel
    /// </summary>
    public class MessageDialogViewModel : BindableBase, IDialogAware
    {
        private string _title = "提示";
        private string _message = string.Empty;
        private string _icon = "Info";
        private bool _showCancel = false;

        /// <summary>
        /// 标题
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        /// <summary>
        /// 图标类型：Info, Warning, Error, Question
        /// </summary>
        public string Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        /// <summary>
        /// 是否显示取消按钮
        /// </summary>
        public bool ShowCancel
        {
            get => _showCancel;
            set => SetProperty(ref _showCancel, value);
        }

        /// <summary>
        /// 确定命令
        /// </summary>
        public ICommand OkCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public ICommand CancelCommand { get; }

        public DialogCloseListener RequestClose { get; }

        // DialogCloseListener IDialogAware.RequestClose => RequestClose;

       // public event Action<IDialogResult>? RequestClose;

        public MessageDialogViewModel()
        {
            OkCommand = new DelegateCommand(OnOk);
            CancelCommand = new DelegateCommand(OnCancel);
        }

        public bool CanCloseDialog() => true;

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.ContainsKey("Title"))
                Title = parameters.GetValue<string>("Title");

            if (parameters.ContainsKey("Message"))
                Message = parameters.GetValue<string>("Message");

            if (parameters.ContainsKey("Icon"))
                Icon = parameters.GetValue<string>("Icon");

            if (parameters.ContainsKey("ShowCancel"))
                ShowCancel = parameters.GetValue<bool>("ShowCancel");
        }

        private void OnOk()
        {
            RequestClose.Invoke(new DialogResult(ButtonResult.OK));
        }

        private void OnCancel()
        {
            RequestClose.Invoke(new DialogResult(ButtonResult.Cancel));
        }
    }
}
