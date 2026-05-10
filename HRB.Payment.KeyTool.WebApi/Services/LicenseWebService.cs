using HRB.Payment.Core.DtoModels;
using HRB.Payment.KeyTool.WebApi.Models;
using Lanymy.Common.Helpers;
using Microsoft.Extensions.Options;

namespace HRB.Payment.KeyTool.WebApi.Services;

public class LicenseWebService
{
    private readonly LicenseGeneratorOptions _options;

    public LicenseWebService(IOptions<LicenseGeneratorOptions> options)
    {
        _options = options.Value;
    }

    public void ValidateRequest(GenerateLicenseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ClientKey))
            throw new InvalidOperationException("激活码不能为空");

        if (request.ValidDays <= 0)
            throw new InvalidOperationException("有效天数必须大于 0");

        if (string.IsNullOrWhiteSpace(_options.SecretKey))
            throw new InvalidOperationException("服务端 SecretKey 未配置");
    }

    private static string NormalizeClientKey(string clientKey)
    {
        return new string(clientKey.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }

    public LicenseDto? ParseClientKey(string clientKey)
    {
        try
        {
            var encrypted = SecurityHelper.DecryptModelFromBase64String<LicenseDto>(NormalizeClientKey(clientKey), _options.SecretKey);
            return encrypted.SourceModel;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[license] Client Key parse failed: {ex.GetType().FullName}: {ex.Message}");
            return null;
        }
    }

    public byte[] BuildLicenseFileBytes(GenerateLicenseRequest request)
    {
        ValidateRequest(request);

        var licenseDto = ParseClientKey(request.ClientKey.Trim());
        if (licenseDto == null)
            throw new InvalidOperationException("无效的 Client Key");

        licenseDto.KeyCreateDateTime = DateTime.Now;
        licenseDto.MaxDateTime = licenseDto.KeyCreateDateTime.AddDays(request.ValidDays);

        var encrypted = SecurityHelper.EncryptModelToBytes(licenseDto, _options.SecretKey);
        return encrypted.EncryptedBytes;
    }
}
