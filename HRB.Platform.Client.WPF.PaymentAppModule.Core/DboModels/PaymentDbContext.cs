using HRB.Platform.Client.WPF.Core.Instruments.Abstractions;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.DboModels
{


    public class PaymentDbContext : BaseWpfLiteDbContext
    {
        public PaymentDbContext(string connectionString, bool isSeed) : base(connectionString, isSeed)
        {

        }


        protected override void Seed()
        {



        }

    }

}
