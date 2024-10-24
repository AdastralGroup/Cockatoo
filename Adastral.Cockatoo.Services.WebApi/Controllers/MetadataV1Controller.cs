using System.Reflection;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.Services.WebApi.Models.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Adastral.Cockatoo.Services.WebApi.Controllers;

[ApiController]
[Route("~/api/")]
public class MetadataV1Controller : Controller
{
    private readonly CockatooConfig _config;

    public MetadataV1Controller(IServiceProvider services)
        : base()
    {
        _config = services.GetRequiredService<CockatooConfig>();
    }
    [HttpGet("Metadata")]
    public ActionResult GetMetadata()
    {
        var data = Json(
            new MetadataV1Response()
            {
                Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown",
                InstanceId = CoreContext.Instance?.Id,
                PublicUrl = _config.PublicUrl,
                PartnerUrl = _config.PartnerUrl,
            }, BaseService.SerializerOptions);
        Response.StatusCode = 200;
        return data;
    }
}