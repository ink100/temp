using HRB.Platform.Client.WPF.Core.CustomAttributes;
using HRB.Platform.Client.WPF.Core.Instruments.Abstractions;
using HRB.Platform.Client.WPF.Platform.Core;
using HRB.Platform.Client.WPF.Platform.Core.ViewModels;
using Lanymy.Common.ExtensionFunctions;
using System.Windows;

namespace HRB.Platform.Client.WPF.Platform.Views
{
    /// <summary>
    /// WindowShell.xaml 的交互逻辑
    /// </summary>
    [IocBindViewModelForShell(typeof(WindowShellViewModel), GlobalSettings.APP_MODULE_NAME)]
    public partial class WindowShell : Window, IPlatformWindowShell
    {

        public WindowShell()
        {

            InitializeComponent();

            if (GlobalSettings.CurrentAppContext.CurrentStartUpArgs.IsFullScreen)
            {
                //this.FullScreen();

                ResizeMode = ResizeMode.NoResize;
                WindowState = WindowState.Maximized;
                WindowStyle = WindowStyle.None;
#if DEBUG
                WindowStyle = WindowStyle.SingleBorderWindow;
#endif
            }
            else
            {

                WindowState = WindowState.Maximized;
                ResizeMode = ResizeMode.NoResize;


                WindowStyle = WindowStyle.None;

#if DEBUG
                WindowStyle = WindowStyle.SingleBorderWindow;
#endif

            }

            Loaded += WindowShell_Loaded;
            Unloaded += WindowShell_Unloaded;
            Closed += WindowShell_Closed;

        }




        private void WindowShell_Loaded(object sender, RoutedEventArgs e)
        {
            var lifecycleAware = DataContext as IUserControlLifecycleAware;
            if (!lifecycleAware.IfIsNull())
            {
                lifecycleAware.OnLoaded(sender);
            }
        }

        private void WindowShell_Unloaded(object sender, RoutedEventArgs e)
        {
            var lifecycleAware = DataContext as IUserControlLifecycleAware;
            if (!lifecycleAware.IfIsNull())
            {
                lifecycleAware.OnUnloaded(sender);
            }
        }

        private void WindowShell_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
            //Application.Current.Shutdown();
        }


    }
}
