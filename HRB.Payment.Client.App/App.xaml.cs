using HRB.Payment.Core.ConstKeys;
using HRB.Payment.Core.DtoModels;
using HRB.Platform.Client.Core.DtoModels;
using HRB.Platform.Client.Core.Enums;
using HRB.Platform.Client.WPF.Core.DtoModels;
using HRB.Platform.Client.WPF.Core.Instruments.Abstractions;
using HRB.Platform.Client.WPF.PaymentAppModule;
using HRB.Platform.Client.WPF.Platform;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Navigation.Regions;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace HRB.Payment.Client.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : BaseWpfApplication
    {
        private static Mutex _mutex = null;
        private const string MutexName = "HRBPaymentClientSingleInstanceMutex";

        protected override void OnStartup(StartupEventArgs e)
        {

            // 单实例检测
            bool createdNew;
            _mutex = new Mutex(true, MutexName, out createdNew);

            if (!createdNew)
            {
                // 应用程序已经在运行
                MessageBox.Show(
                    "收银台程序已经在运行中，请勿重复启动！",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                // 退出当前实例
                Current.Shutdown();
                return;
            }

            // 检测系统的渲染能力，如果不支持硬件加速，则强制使用软件渲染
            var tier = RenderCapability.Tier >> 16;
            if (tier < 1)
            {
                RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            }


            // 继续正常启动流程
            base.OnStartup(e);



        }

        //protected override void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        //{
        //    throw new NotImplementedException();
        //}


        protected override void OnExit(ExitEventArgs e)
        {
            // 释放 Mutex
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            // UnregisterApplicationRestart();

            base.OnExit(e);
        }

        protected override SplashScreenInfoDto GetSplashScreenInfo()
        {
            return new SplashScreenInfoDto
            {
                IsShowSplashScreen = false,
                //IsShowSplashScreen = true,
                IsFullScreen = true,
                ImagePath = "/HRB.Payment.Client.App;component/SplashScreen.png",
                ImageWidth = 1024,
                ImageHeight = 768,
#if DEBUG
                TimerIntervalMilliseconds = 50,
                IsDebugMode = true,
#else
                TimerIntervalMilliseconds = 200,
#endif
                ////TimerIntervalMilliseconds = 300,
            };
        }

        protected override PlatformWpfStartUpArgsDto GetPlatformStartUpArgs()
        {
            return new PlatformWpfStartUpArgsDto
            {

#if DEBUG
                IsFullScreen = true,
                //IsFullScreen = false,
#else
                IsFullScreen = true,
#endif
                IsSingletonRun = true,
                IsTelerikShell = false,
                CurrentLanguageType = LanguageTypeEnum.CN,
                IsEnableOnlineUpdate = false,
                IsEnabledPlatform = false,
                StartAppModuleName = PlatformWpfAppModuleKeys.PAYMENT_APP_MODULE_NAME,

            };
        }

        protected override BaseStartUpArgsDto GetStartAppModuleStartUpArgs()
        {
            return new PaymentStartUpArgsDto();
        }

        #region 全局异常


        protected override void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
          //  throw new NotImplementedException();
        }


        protected override void OnAppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
           // throw new NotImplementedException();
        }

        protected override void OnTaskSchedulerUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
          //  throw new NotImplementedException();
        }


        #endregion




        protected override void OnRegisterTypes(IContainerRegistry containerRegistry)
        {
        }

        protected override void OnConfigureRegionAdapterMappings(RegionAdapterMappings regionAdapterMappings)
        {
        }

        protected override void OnConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<PlatformViewAppModule>();
            moduleCatalog.AddModule<PaymentViewAppModule>();
        }

        protected override void OnConfigureViewModelLocator()
        {
        }

        protected override void OnInitializedEvent()
        {
        }



    }

}
