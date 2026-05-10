using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Configuration
{
    public static class CustomAlipayConfig
    {

        ///// <summary>
        ///// 应用ID
        ///// </summary>
        //public const string AppId = "2021006116698125";

        /// <summary>
        /// 支付宝网关
        /// </summary>
        public static string? GatewayUrl = "https://openapi.alipay.com/gateway.do";

        ///// <summary>
        ///// APP私钥
        ///// </summary>
        //public const string? AppPrivateKey =
        //    "MIIEpAIBAAKCAQEA0nSyWCKs2bIn3huGWPCrBRFko/TGrcr1QMMWcxA0ljTW7URTi0LhKLzuO6EDnTk+iMlLlQVc6PnA5dLYiHk9Vcp9QsIvTO4bJJowZqSioHyJjXh4Ww6l+fBQWT68qSFheuxe+VBRx8NtpHuFtmO0wUiGZVeMz9PaqnWVPZAlazuxN4muRVQrqvgdYBWFsAOujNnNLMCf4RyEZ+Q1uFbBCh3zyMJOGG0slR4kL0yeRkDFcoe6LE9rpRgJiEvpCxac/vAddEiK4+smVGKQTKeuBYlNXoXtRWVqJiXbADK4InUcS1r3u+s3akDQ08JEPushs4oHlpKA9RblRyp4zn9CWwIDAQABAoIBAQCmlJv9yySKA8wusBa9G3IixsukQ0FnmrhZlJGWbgNRyW09zNFb74oNFs5zAFW+AuuOldZvlBkgh1+6ChQ+och8uZRAXTfXaritViZteG8JHMo74llRqdySYzcWDOSD8toX1DSwnnS86+FDSkpiPbV7MA7A9HWOox/3wK5qVkhoAR4jjIdioBbs7d49uo9wntwPRmyl/znfcMnXftWrbi+ptQXHaXpES5o+vPDdha+9f00KJP2u1R8h7HBgEZ4VDhE7JFW8hTv5V4smrhmf0Vybr1BOwiUTGPwoJnPHrFU9ZjRPnTUNbejAl2CksdOuzOYW/i0iMR2BZl8y+nHjGWHxAoGBAOorp2rDDHUnhY7rFFYwu0tctjlT8ANhZxbL3F0tsji7ZVm4Fling1hntyMN6qzKTnsVK+vlgLrknQq/RLixIoiUQhEBBVnu/Z/vGJUkq5MS5+PPUn6oyfaAhDLvHNzAG7f76JDSRPvyszUK8Bzhmw1LnKXrgb05EhS4gLimzvQFAoGBAOYTGv4Ell6nqq6JaXeIc1k39qtJSPKmwuT8kQOsJv1Y6dsi58hnsgVRmZSi5Rm1bzkiX/tOAjFQcoj+TACTsU6sfFu9/myMpJ9JcHkMoUTXT4Ar3cVDy8oGNQRZ7QorbMw+OdpjrZq18PReM7y+sqrdp/l84dgBSZ2wrVbLVorfAoGAOlo0dtMx6IO3sVx9DdlI4sewlOqItZ7w/GpCeGmprp9r6waHcwITJdV6el6+at5i3iLxdfATuv6673GoI0norBYdvHT/q2B1jQQcoRWpN5YPeOIx0WQoJ6fwyWxyScQJZDh+xI/RJuNcqswV92x0ocSEvYfJJajyC33Kfbj7ey0CgYEAufelcL9qf/YMm2mMq0bK3cnDg24IHcK3c/nGiW5kUZHF7RAIw881cOElP6RvhFlIHqlvZaUHVq8M9Th/XFQFFG+NUWaAOWNxSSNGW0HfNNLIevR6HJIRLinYPr0lQXwaQ/jkMGczkwMPUaa3MQ//QOdLd/j+X+eihmiho69WM4cCgYAlrFoPp/YqfPJda718nDRAswtQA/AwrMFZDbERY9OsZLyqepps1X9Vnz2fNEPwffq1+5wRJLGT2PoWkIVlczfLKpvK5CO4OnQqpxy/N0I9u6VZZK76wyBr1JLyOSVuxZUsWfaabVfufKdL2u3cxFIHubk35fq4285ngi7yh1YEiA==";


        ///// <summary>
        ///// 支付宝公钥
        ///// </summary>
        //public const string? AliPayPublicKey =
        //    "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAua58MIFfCzrtKpsEwUTlz/4EGsVvgUCfzzd9KA82Jo/s25X0xE1Me8luPaQ5KaJoDeq2qZyzpH/qU+sOwpOkRuqgSTaF9+WSYPeI0UPVLbwvF6IdsNJSLCOE/Gob5EdGr+UV54jDIMBjqGqnWJjGcEH5RGwbk5nqw/tAy/hD0GXvNE1G7GHPoGScQhEvqvNRnB4Lr8aSsaoTPS+V0Re6IAtjV+DsUb7TWB7JW21zKEOMB/rI6XiIrwg8RiDyxSdIxRUr5xgzZz7GYl0gR5DbNfxHSy5qf+AUSmKxlsrKb2nxWPKWahGVvebVAvQjoIYgOAjTmgi2bBxve8IAHlWpCwIDAQAB";
        public const string? SignType = "RSA2";
        public const string? Charset = "UTF-8";
        public const string? Format = "JSON";
        public const string? Version = "1.0";

    }
}
