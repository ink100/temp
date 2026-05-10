namespace HRB.Payment.KeyTool.WebApi.Models;

public class LicenseGeneratorOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string ActivationCode { get; set; } = string.Empty;
    public string OtpSecret { get; set; } = string.Empty;
}
