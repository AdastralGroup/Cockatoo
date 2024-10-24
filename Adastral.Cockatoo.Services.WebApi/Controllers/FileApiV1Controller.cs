using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Adastral.Cockatoo.Services.WebApi.Controllers;

[ApiController]
[Route("~/api/v1/File")]
public class FileApiV1Controller : Controller
{
    private readonly StorageFileRepository _storageFileRepo;
    private readonly StorageService _storageService;
    private readonly CockatooConfig _config;
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public FileApiV1Controller(IServiceProvider services)
        : base()
    {
        _storageFileRepo = services.GetRequiredService<StorageFileRepository>();
        _storageService = services.GetRequiredService<StorageService>();
        _config = services.GetRequiredService<CockatooConfig>();
    }

    [HttpGet("{id}/Content")]
    [ProducesResponseType(200, Type = typeof(FileContentResult))]
    [ProducesResponseType(301)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetFile(string id)
    {
        _log.Trace($"{nameof(GetFile)}, id={id}");
        var model = await _storageFileRepo.GetById(id);
        if (model == null)
        {
            return new NotFoundResult();
        }

        if (_config.Storage.S3.Enable)
        {
            if (_config.Storage.Proxy == false)
            {
                string serviceUrl = _config.Storage.S3.ServiceUrl.TrimEnd('/');
                string location = $"{serviceUrl}/{_config.Storage.S3.BucketName}/{model.Location}";
                _log.Trace($"id={id} Redirecting to {location}");
                return new RedirectResult(location, true);
            }
        }

        try
        {
            var content = await _storageService.GetStream(model);
            return new FileStreamResult(content, model.ContentType)
            {
                FileDownloadName = Path.GetFileName(model.Location)
            };
        }
        catch (Exception ex)
        {
            if (ex.Message == "NotFound")
            {
                return new NotFoundResult();
            }

            throw;
        }
    }

    [HttpGet("{id}/Details")]
    [ProducesResponseType(200, Type = typeof(StorageFileModel))]
    [ProducesResponseType(404)]
    [TrackRequest]
    public async Task<IActionResult> GetFileDetails(string id)
    {
        _log.Trace($"{nameof(GetFileDetails)}, id={id}");
        var model = await _storageFileRepo.GetById(id);
        if (model == null)
        {
            return new NotFoundResult();
        }

        return Json(model, BaseService.SerializerOptions);
    }

    [HttpPost("Upload")]
    [ProducesResponseType(200, Type = typeof(StorageFileModel))]
    [ProducesResponseType(500, Type = typeof(ExceptionWebResponse))]
    [AuthRequired]
    [PermissionRequired(PermissionKind.FileUpload)]
    public async Task<ActionResult> UploadFile([FromQuery] string filename)
    {
        try
        {
            _log.Debug($"[filename={filename}]Upload started");
            HttpContext.Request.Body.Seek(0, SeekOrigin.Begin);
            var model = await _storageService.UploadFile(HttpContext.Request.Body, filename, HttpContext.Request.ContentLength);
            if (string.IsNullOrEmpty(model.ContentType))
            {
                model.ContentType = MimeTypes.GetMimeType(filename);
                model = await _storageFileRepo.InsertOrUpdate(model);
            }

            Response.StatusCode = 200;
            return Json(model, BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            Response.StatusCode = 500;
            _log.Error(ex);
            SentrySdk.CaptureException(ex);
            var errorResponse = new ExceptionWebResponse(new Exception("Failed to upload file", ex));
            return Json(errorResponse, BaseService.SerializerOptions);
        }
    }
}