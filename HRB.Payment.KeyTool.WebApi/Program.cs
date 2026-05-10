using HRB.Payment.KeyTool.WebApi.Models;
using HRB.Payment.KeyTool.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.Configure<LicenseGeneratorOptions>(builder.Configuration.GetSection("LicenseGenerator"));
builder.Services.AddSingleton<LicenseWebService>();
builder.Services.AddControllers();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.Run();
