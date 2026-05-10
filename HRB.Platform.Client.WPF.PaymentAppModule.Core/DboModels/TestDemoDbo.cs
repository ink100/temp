using Lanymy.Common.Abstractions.Interfaces;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.DboModels
{

    public class TestDemoDbo : ICreateDateTime
    {


        [BsonId]
        public Guid ID { get; set; }

        public string Message { get; set; }

        public DateTime CreateDateTime { get; set; }



    }

}
