using Lanymy.Common.Abstractions.Interfaces;

namespace HRB.Payment.Core.DtoModels
{

    public class LicenseDto
    {


        public string SN { get; set; }
        public DateTime MaxDateTime { get; set; }

        public DateTime CreateDateTime { get; set; }
        public DateTime KeyCreateDateTime { get; set; }


    }

}
