namespace HRB.Payment.KeyTool.WebApi.Models;

public class GenerateLicenseRequest
{
    public string ClientKey { get; set; } = string.Empty;
    public int ValidDays { get; set; }
}
