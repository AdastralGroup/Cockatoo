using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess;
using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Sentry;

namespace Adastral.Cockatoo.Services.WebApi.Controllers;

[ApiController]
[TrackRequest]
public class SouthbankApiController : Controller
{
    private readonly SouthbankService _sbService;

    public SouthbankApiController()
        : base()
    {
        _sbService = CoreContext.Instance!.Services.GetRequiredService<SouthbankService>();
    }
    /// <summary>
    /// Fetch the generated/cached Southbank model.
    /// </summary>
    [HttpGet]
    [Route("~/api/v1/Southbank")]
    [ProducesResponseType(typeof(SouthbankV1), 200, "application/json")]
    public async Task<ActionResult> GetSouthbankV1()
    {
        Response.StatusCode = 200;
        var data = await _sbService.GetLatest();
        return Json(data.GetV1(), BaseService.SerializerOptions);
    }

    /// <summary>
    /// Fetch the generated/cached Southbank model.
    /// </summary>
    [HttpGet]
    [Route("~/api/v2/Southbank")]
    [ProducesResponseType(typeof(SouthbankV2), 200, "application/json")]
    public async Task<ActionResult> GetSouthbankV2()
    {
        Response.StatusCode = 200;
        var data = await _sbService.GetLatest();
        return Json(data.GetV2(), BaseService.SerializerOptions);
    }

    /// <summary>
    /// Fetch the generated/cached Southbank model.
    /// </summary>
    [HttpGet]
    [Route("~/api/v3/Southbank")]
    [ProducesResponseType(typeof(SouthbankV3), 200, "application/json")]
    public async Task<ActionResult> GetSouthbankV3()
    {
        Response.StatusCode = 200;
        var data = await _sbService.GetLatest();
        return Json(data.GetV3(), BaseService.SerializerOptions);
    }

    [HttpGet("~/api/v1/Southbank/RefreshCache")]
    [ProducesResponseType(typeof(EmptyResult), 200)]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500)]
    [AuthRequired]
    [PermissionRequired(PermissionKind.RefreshSouthbank)]
    public async Task<ActionResult> RefreshCache()
    {
        try
        {
            Response.StatusCode = 200;
            await _sbService.GenerateSouthbank();
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            Response.StatusCode = 500;
            SentrySdk.CaptureException(ex);
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }
}