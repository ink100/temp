namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Services
{
    /// <summary>
    /// 对上游在 CDATA 中途截断的微信 paymsg XML 做保守恢复。
    /// 仅删除最后一个未完成字段，并补齐 paymsg/sysmsg 结束标签；
    /// 后续仍必须通过标准 XDocument 解析和支付字段校验。
    /// </summary>
    internal static class WeChatXmlRecovery
    {
        private const string CDataStart = "<![CDATA[";
        private const string CDataEnd = "]]>";

        public static bool TryRepairTruncatedCData(
            string xmlContent,
            out string repairedXml,
            out string omittedElementName)
        {
            repairedXml = string.Empty;
            omittedElementName = string.Empty;

            if (string.IsNullOrWhiteSpace(xmlContent))
                return false;

            var cdataStartIndex = xmlContent.LastIndexOf(CDataStart, StringComparison.Ordinal);
            if (cdataStartIndex < 0)
                return false;

            // 最后一个 CDATA 已正常结束时，不属于本方法可恢复的截断场景。
            if (xmlContent.IndexOf(CDataEnd, cdataStartIndex + CDataStart.Length, StringComparison.Ordinal) >= 0)
                return false;

            var paymsgStartIndex = xmlContent.IndexOf("<paymsg", StringComparison.Ordinal);
            if (paymsgStartIndex < 0 || paymsgStartIndex >= cdataStartIndex)
                return false;

            // 未结束 CDATA 前最后一个标签必须是 paymsg 的普通字段开始标签。
            var elementStartIndex = xmlContent.LastIndexOf('<', cdataStartIndex - 1);
            var elementEndIndex = elementStartIndex < 0
                ? -1
                : xmlContent.IndexOf('>', elementStartIndex + 1);

            if (elementStartIndex <= paymsgStartIndex ||
                elementEndIndex < 0 ||
                elementEndIndex >= cdataStartIndex)
            {
                return false;
            }

            var openingTag = xmlContent.Substring(
                elementStartIndex + 1,
                elementEndIndex - elementStartIndex - 1).Trim();

            if (openingTag.Length == 0 ||
                openingTag[0] is '/' or '!' or '?' ||
                openingTag.EndsWith("/", StringComparison.Ordinal))
            {
                return false;
            }

            var nameEndIndex = openingTag.IndexOfAny([' ', '\t', '\r', '\n']);
            omittedElementName = nameEndIndex < 0
                ? openingTag
                : openingTag[..nameEndIndex];

            if (string.IsNullOrWhiteSpace(omittedElementName) ||
                omittedElementName.Contains(':') ||
                omittedElementName.Equals("paymsg", StringComparison.Ordinal) ||
                omittedElementName.Equals("sysmsg", StringComparison.Ordinal))
            {
                omittedElementName = string.Empty;
                return false;
            }

            if (!IsRecoverableOptionalElement(omittedElementName))
            {
                omittedElementName = string.Empty;
                return false;
            }

            repairedXml = xmlContent[..elementStartIndex] + "</paymsg></sysmsg>";
            return true;
        }

        private static bool IsRecoverableOptionalElement(string elementName)
        {
            // 只允许忽略不参与订单状态、订单号、金额、用户和时间判定的末尾展示字段。
            // 关键字段一旦截断必须让标准解析失败，不能猜测恢复。
            return elementName.Equals("headimgurl", StringComparison.Ordinal) ||
                   elementName.Equals("scene", StringComparison.Ordinal);
        }
    }
}
