namespace HRB.Payment.Core.DtoModels;

public class LicenseDto
{
    public string SN { get; set; } = string.Empty;
    public DateTime MaxDateTime { get; set; }
    public DateTime CreateDateTime { get; set; }
    public DateTime KeyCreateDateTime { get; set; }
}
