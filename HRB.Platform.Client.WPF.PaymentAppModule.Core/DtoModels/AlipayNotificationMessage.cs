namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.DtoModels
{
    public class AlipayNotificationMessage
    {
        public int APMessageType { get; set; }
        public string Message { get; set; }
        public string CreateDateTime { get; set; }
    }

    public class AlipayPayMessage
    {
        public int PayMessageType { get; set; }
        public string PayMessage { get; set; }
    }

    public class AlipayAppConfig
    {
        public string AppID { get; set; }
        public string Key { get; set; }

        public string UserID { get; set; }

        public string UserName { get; set; }

        public string EMail { get; set; }

        public string Phone { get; set; }

    }
}
