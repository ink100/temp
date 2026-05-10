using HRB.Payment.KeyTool.WebApi.Models;
using HRB.Payment.KeyTool.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace HRB.Payment.KeyTool.WebApi.Controllers;

[ApiController]
[Route("api/license")]
public class LicenseController : ControllerBase
{
    private readonly LicenseWebService _licenseWebService;

    public LicenseController(LicenseWebService licenseWebService)
    {
        _licenseWebService = licenseWebService;
    }

    [HttpPost("generate")]
    public IActionResult Generate([FromBody] GenerateLicenseRequest request)
    {
        try
        {
            var bytes = _licenseWebService.BuildLicenseFileBytes(request);
            var fileName = $"{DateTime.Now:yyyyMMddHHmmss}.key";
            return File(bytes, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
