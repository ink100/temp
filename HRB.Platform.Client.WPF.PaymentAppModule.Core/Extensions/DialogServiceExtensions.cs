using Prism.Dialogs;
using System;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Extensions
{
    /// <summary>
    /// DialogService 扩展方法
    /// </summary>
    public static class DialogServiceExtensions
    {
        /// <summary>
        /// 显示信息对话框
        /// </summary>
        public static void ShowInfo(this IDialogService dialogService, string message, string title = "提示", Action<IDialogResult>? callback = null)
        {
            var parameters = new DialogParameters
            {
                { "Title", title },
                { "Message", message },
                { "Icon", "Info" },
                { "ShowCancel", false }
            };

            dialogService.ShowDialog("MessageDialog", parameters, callback ?? (_ => { }));
        }

        /// <summary>
        /// 显示警告对话框
        /// </summary>
        public static void ShowWarning(this IDialogService dialogService, string message, string title = "警告", Action<IDialogResult>? callback = null)
        {
            var parameters = new DialogParameters
            {
                { "Title", title },
                { "Message", message },
                { "Icon", "Warning" },
                { "ShowCancel", false }
            };

            dialogService.ShowDialog("MessageDialog", parameters, callback ?? (_ => { }));
        }

        /// <summary>
        /// 显示错误对话框
        /// </summary>
        public static void ShowError(this IDialogService dialogService, string message, string title = "错误", Action<IDialogResult>? callback = null)
        {
            var parameters = new DialogParameters
            {
                { "Title", title },
                { "Message", message },
                { "Icon", "Error" },
                { "ShowCancel", false }
            };

            dialogService.ShowDialog("MessageDialog", parameters, callback ?? (_ => { }));
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        public static void ShowConfirm(this IDialogService dialogService, string message, string title = "确认", Action<IDialogResult> callback = null)
        {
            var parameters = new DialogParameters
            {
                { "Title", title },
                { "Message", message },
                { "Icon", "Question" },
                { "ShowCancel", true }
            };

            dialogService.ShowDialog("MessageDialog", parameters, callback);
        }

        /// <summary>
        /// 显示成功对话框
        /// </summary>
        public static void ShowSuccess(this IDialogService dialogService, string message, string title = "成功", Action<IDialogResult>? callback = null)
        {
            var parameters = new DialogParameters
            {
                { "Title", title },
                { "Message", message },
                { "Icon", "Info" },
                { "ShowCancel", false }
            };

            dialogService.ShowDialog("MessageDialog", parameters, callback ?? (_ => { }));
        }
    }
}
