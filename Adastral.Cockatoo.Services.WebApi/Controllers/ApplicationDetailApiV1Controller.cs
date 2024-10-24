using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.Common.Helpers;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Models.AutoUpdaterDotNet;
using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.DataAccess.Repositories.AutoUpdaterDotNet;
using Adastral.Cockatoo.Services.WebApi.Models.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Adastral.Cockatoo.Services.WebApi.Controllers;

[ApiController]
[Route("~/api/v1/ApplicationDetail")]
[TrackRequest]
public class ApplicationDetailApiV1Controller : Controller
{
    private readonly ApplicationDetailRepository _appDetailRepo;
    private readonly PermissionWebService _permissionWebService;
    private readonly ApplicationDetailService _appDetailService;
    private readonly AUDNRevisionRepository _audnRevisionRepo;
    private readonly StorageFileRepository _storageFileRepo;
    private readonly StorageService _storageService;
    private readonly AuthWebService _authWebService;
    private readonly ScopedPermissionWebService _scopedPermissionWebService;

    public ApplicationDetailApiV1Controller(IServiceProvider services)
        : base()
    {
        _appDetailRepo = services!.GetRequiredService<ApplicationDetailRepository>();
        _permissionWebService = services.GetRequiredService<PermissionWebService>();
        _appDetailService = services.GetRequiredService<ApplicationDetailService>();
        _audnRevisionRepo = services.GetRequiredService<AUDNRevisionRepository>();
        _storageFileRepo = services.GetRequiredService<StorageFileRepository>();
        _storageService = services.GetRequiredService<StorageService>();
        _authWebService = services.GetRequiredService<AuthWebService>();
        _scopedPermissionWebService = services.GetRequiredService<ScopedPermissionWebService>();
    }
    
    /// <summary>
    /// Fetch an array of all available application details.
    /// </summary>
    [HttpGet("Available")]
    [ProducesResponseType(typeof(List<ApplicationDetailModel>), 200, "application/json")]
    public async Task<ActionResult> GetAvailableJson()
    {
        bool includePrivate = await _permissionWebService.CurrentHasAny(HttpContext, PermissionKind.ApplicationDetailViewAll);
        var data = await _appDetailRepo.GetAll(includePrivate);
        return Json(data.ToList(), BaseService.SerializerOptions);
    }

    /// <summary>
    /// Fetch info about a specific application by its ID
    /// </summary>
    [HttpGet("Id/{id}")]
    [ProducesResponseType(typeof(ApplicationDetailModel), 200, "application/json")]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 403, "application/json")]
    [ProducesResponseType(typeof(NotFoundResponse), 404, "application/json")]
    public async Task<ActionResult> GetById(string id)
    {
        var data = await _appDetailRepo.GetById(id);
        if (data == null)
        {
            Response.StatusCode = 404;
            return Json(new NotFoundResponse()
            {
                Message = $"Could not find {nameof(ApplicationDetailModel)} with Id {id}",
                PropertyName = nameof(id)
            }, BaseService.SerializerOptions);
        }
        var user = await _authWebService.GetCurrentUser(HttpContext);
        if (data!.IsPrivate)
        {
            if (user != null)
            {
                var i = await _scopedPermissionWebService.CheckApplicationScopedPermission(
                    data.Id, user, [PermissionKind.ApplicationDetailViewAll]);
                if (i != ScopedPermissionWebService.CheckScopedPermissionResult.Continue)
                {
                    Response.StatusCode = 403;
                    return Json(new NotAuthorizedResponse()
                    {
                        MissingPermissions = [PermissionKind.ApplicationDetailViewAll]
                    }, BaseService.SerializerOptions);
                }
            }
            else
            {
                Response.StatusCode = 403;
                return Json(new NotAuthorizedResponse()
                {
                    MissingPermissions = [PermissionKind.ApplicationDetailViewAll]
                }, BaseService.SerializerOptions);
            }
        }
        
        Response.StatusCode = 200;
        return Json(data, BaseService.SerializerOptions);
    }

    [HttpGet("Id/{id}/AutoUpdateDotNet/File")]
    [ProducesResponseType(200, Type = typeof(FileStreamResult))]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 403, "application/json")]
    [ProducesResponseType(typeof(NotFoundResponse), 404, "application/json")]
    public async Task<ActionResult> GetAUDNFile(string id, [FromQuery] bool includeDisabled = false)
    {
        var app = await _appDetailRepo.GetById(id);
        if (app == null)
        {
            Response.StatusCode = 404;
            return Json(new NotFoundResponse()
            {
                Message = $"Could not find {nameof(ApplicationDetailModel)} with Id {id}",
                PropertyName = nameof(id)
            }, BaseService.SerializerOptions);
        }

        if (app.Type != ApplicationDetailType.AutoUpdaterDotNet)
        {
            Response.StatusCode = 404;
            return Json(new NotFoundResponse()
            {
                Message = $"Could not find {nameof(ApplicationDetailModel)} with Id {id}",
                PropertyName = nameof(id)
            }, BaseService.SerializerOptions);
        }
        
        bool includePrivate = true;
        var user = await _authWebService.GetCurrentUser(HttpContext);
        if (app!.IsPrivate)
        {
            includePrivate = false;
            if (user != null)
            {
                var i = await _scopedPermissionWebService.CheckApplicationScopedPermission(
                    app.Id, user, [PermissionKind.ApplicationDetailViewAll]);
                if (i != ScopedPermissionWebService.CheckScopedPermissionResult.Continue)
                {
                    Response.StatusCode = 403;
                    return Json(new NotAuthorizedResponse()
                    {
                        MissingPermissions = [PermissionKind.ApplicationDetailViewAll]
                    }, BaseService.SerializerOptions);
                }

                includePrivate = true;
            }
            else
            {
                Response.StatusCode = 403;
                return Json(new NotAuthorizedResponse()
                {
                    MissingPermissions = [PermissionKind.ApplicationDetailViewAll]
                }, BaseService.SerializerOptions);
            }
        }

        var revision = await _audnRevisionRepo.GetLatestForApp(app.Id, includeDisabled && includePrivate);
        if (revision == null)
        {
            Response.StatusCode = 404;
            return Json(new NotFoundResponse()
            {
                Message = $"Could not find latest AutoUpdaterDotNet revision for {nameof(ApplicationDetailModel)} with Id {id}",
                PropertyName = nameof(id)
            }, BaseService.SerializerOptions);
        }
        var fileModel = await _storageFileRepo.GetById(revision.StorageFileId);
        if (fileModel == null)
        {
            Response.StatusCode = 404;
            return Json(new NotFoundResponse()
            {
                Message = $"Could not find file with Id {revision.StorageFileId} for {nameof(AUDNRevisionModel)} with Id {revision.Id}",
                PropertyName = nameof(revision.StorageFileId),
                PropertyParentType = CockatooHelper.FormatTypeName(revision.GetType())
            }, BaseService.SerializerOptions);
        }
        Response.Headers.TryAdd("X-AUDNRevisionModel-Id", new Microsoft.Extensions.Primitives.StringValues(revision.Id));
        var content = await _storageService.GetStream(fileModel);
        Response.StatusCode = 200;
        return new FileStreamResult(content, fileModel.ContentType)
        {
            FileDownloadName = Path.GetFileName(fileModel.Location)
        }; 
    }

    [HttpGet("Id/{id}/AutoUpdateDotNet")]
    [ProducesResponseType(typeof(UpdateInfoEventArgs), 200, "application/xml")]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 403, "application/json")]
    [ProducesResponseType(typeof(NotFoundResponse), 404, "application/json")]
    public async Task<ActionResult> GetAutoUpdateDotNet(string id, [FromQuery] bool includeDisabled = false, [FromQuery] bool force = false)
    {
        var app = await _appDetailRepo.GetById(id);
        if (app == null)
        {
            Response.StatusCode = 404;
            return Json(new NotFoundResponse()
            {
                Message = $"Could not find {nameof(ApplicationDetailModel)} with Id {id}",
                PropertyName = nameof(id)
            }, BaseService.SerializerOptions);
        }

        if (app.Type != ApplicationDetailType.AutoUpdaterDotNet)
        {
            Response.StatusCode = 404;
            return Json(new NotFoundResponse()
            {
                Message = $"Could not find {nameof(ApplicationDetailModel)} with Id {id}",
                PropertyName = nameof(id)
            }, BaseService.SerializerOptions);
        }
        
        bool includePrivate = true;
        var user = await _authWebService.GetCurrentUser(HttpContext);
        if (app!.IsPrivate)
        {
            includePrivate = false;
            if (user != null)
            {
                var i = await _scopedPermissionWebService.CheckApplicationScopedPermission(
                    app.Id, user, [PermissionKind.ApplicationDetailViewAll]);
                if (i != ScopedPermissionWebService.CheckScopedPermissionResult.Continue)
                {
                    Response.StatusCode = 403;
                    return Json(new NotAuthorizedResponse()
                    {
                        MissingPermissions = [PermissionKind.ApplicationDetailViewAll]
                    }, BaseService.SerializerOptions);
                }

                includePrivate = true;
            }
            else
            {
                Response.StatusCode = 403;
                return Json(new NotAuthorizedResponse()
                {
                    MissingPermissions = [PermissionKind.ApplicationDetailViewAll]
                }, BaseService.SerializerOptions);
            }
        }

        var xmlContent = await _appDetailService.GetAUDNXML(app.Id, includeDisabled && includePrivate, force && includePrivate);
        if (xmlContent == null)
        {
            Response.StatusCode = 404;
            return Json(
                new NotFoundResponse()
                {
                    Message = $"Could not find any revisions for the Application Id provided ({id})",
                    PropertyName = nameof(id)
                }, BaseService.SerializerOptions);
        }
        return new ContentResult
        {
            ContentType = "application/xml",
            Content = xmlContent,
            StatusCode = 200
        };
    }
}
