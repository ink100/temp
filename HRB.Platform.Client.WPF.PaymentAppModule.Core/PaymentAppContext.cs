using HRB.Payment.Core.DtoModels;
using HRB.Platform.Client.Core.Enums;
using HRB.Platform.Client.Core.ExtensionFunctions;
using HRB.Platform.Client.Core.Interfaces;
using HRB.Platform.Client.WPF.Core.Instruments.Abstractions;
using HRB.Platform.Client.WPF.Core.Services.IServices;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.DtoModels;
using HRB.Platform.Client.WPF.PaymentAppModule.Core.Repository;
using Lanymy.Common.ExtensionFunctions;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core
{


    public class PaymentAppContext : BaseWpfAppContext<PaymentLogger>
        , IAppContextStartUpArgs<PaymentStartUpArgsDto>
        , IAppContextRepository<IPaymentRepository>
        , IAppContextSettings<SettingsDto>
    {

        private PaymentStartUpArgsDto _CurrentStartUpArgs;

        public PaymentStartUpArgsDto CurrentStartUpArgs
        {

            get
            {
                if (_CurrentStartUpArgs.IfIsNull())
                {
                    _CurrentStartUpArgs = this.GetStartUpArgs<PaymentStartUpArgsDto>();

                    if (_CurrentStartUpArgs.IfIsNull())
                    {
                        throw new ArgumentNullException(nameof(PaymentStartUpArgsDto));
                    }

                }

                return _CurrentStartUpArgs;

            }

        }
        private IPaymentRepository _CurrentRepository;

        public IPaymentRepository CurrentRepository
        {
            get
            {
                if (_CurrentRepository.IfIsNull())
                {
                    _CurrentRepository = CurrentContainerProvider.Resolve<IPaymentRepository>();
                }
                return _CurrentRepository;
            }
        }



        private SettingsDto _CurrentSettings;

        public SettingsDto CurrentSettings
        {
            get
            {
                if (_CurrentSettings.IfIsNull())
                {

                    _CurrentSettings = CurrentContainerProvider.Resolve<SettingsDto>();

                    if (_CurrentSettings.IfIsNull())
                    {

                        _CurrentSettings = new SettingsDto
                        {

                            CreateDateTime = DateTime.Now,
                            LastUpdateDateTime = DateTime.Now,


                        };


                        this.SaveCurrentSettings(_CurrentSettings);

                    }

                }

                _CurrentSettings.IsWeChatNicknameReminderEnabled = false;
                _CurrentSettings.IsAlipayNicknameReminderEnabled = false;

                return _CurrentSettings;

            }
        }




        public PaymentAppContext(string appModuleName, IContainerProvider containerProvider, ITranslator translator, IWpfResourcesGetter resourcesGetter, IWpfCommonRequestService commonRequestService)
            : base(appModuleName, containerProvider, translator, resourcesGetter, commonRequestService)
        {
        }

    }

}
