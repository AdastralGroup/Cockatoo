using System.Data;
using System.Net;
using System.Text.Json;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.Common.Helpers;
using Adastral.Cockatoo.DataAccess;
using Adastral.Cockatoo.DataAccess.Helpers;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.Services.WebApi.Helpers;
using Adastral.Cockatoo.Services.WebApi.Models.Request;
using Adastral.Cockatoo.Services.WebApi.Models.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using CheckScopedPermissionResult = Adastral.Cockatoo.Services.WebApi.ScopedPermissionWebService.CheckScopedPermissionResult;

namespace Adastral.Cockatoo.Services.WebApi.Controllers;

[ApiController]
[Route("~/api/v1/Manage/Bullseye/")]
[TrackRequest]
public class ManageBullseyeApiV1Controller : Controller
{
    private readonly ApplicationDetailRepository _appDetailRepo;
    private readonly BullseyeAppRevisionRepository _bullseyeRevisionRepo;
    private readonly BullseyePatchRepository _bullseyePatchRepo;
    private readonly StorageFileRepository _storageFileRepo;
    private readonly BullseyeCacheService _bullCacheService;
    private readonly BullseyeAppRepository _bullseyeAppRepo;
    private readonly BullseyeService _bullService;
    private readonly ManageBullseyeWebService _manageBullseyeWebService;
    private readonly AuthWebService _authWebService;
    private readonly ScopedPermissionWebService _scopedPermissionWebService;

    public ManageBullseyeApiV1Controller(IServiceProvider services)
        : base()
    {
        _appDetailRepo = CoreContext.Instance!.GetRequiredService<ApplicationDetailRepository>();
        _bullseyeRevisionRepo = CoreContext.Instance!.GetRequiredService<BullseyeAppRevisionRepository>();
        _bullseyePatchRepo = CoreContext.Instance!.GetRequiredService<BullseyePatchRepository>();
        _storageFileRepo = CoreContext.Instance!.GetRequiredService<StorageFileRepository>();
        _bullCacheService = CoreContext.Instance!.GetRequiredService<BullseyeCacheService>();
        _bullseyeAppRepo = CoreContext.Instance!.GetRequiredService<BullseyeAppRepository>();
        _bullService = CoreContext.Instance!.GetRequiredService<BullseyeService>();
        _manageBullseyeWebService = services.GetRequiredService<ManageBullseyeWebService>();
        _authWebService = services.GetRequiredService<AuthWebService>();
        _scopedPermissionWebService = services.GetRequiredService<ScopedPermissionWebService>();
    }

    [ProducesResponseType(typeof(IEnumerable<ApplicationDetailModel>), 200, "application/json")]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 401, "application/json")]
    [ProducesResponseType(typeof(NotFoundResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [AuthRequired]
    [PermissionRequired(PermissionKind.BullseyeViewPrivateModels)]
    [HttpGet("Apps")]
    public async Task<ActionResult> ListApps()
    {
        try
        {
            var data = await _bullService.GetAllApps();
            Response.StatusCode = 200;
            return Json(data, BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    [ProducesResponseType(typeof(IEnumerable<BullseyeAppRevisionModel>), 200, "application/json")]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 401, "application/json")]
    [ProducesResponseType(typeof(NotFoundResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [AuthRequired]
    [ScopedPermissionRequired("appId", ScopedPermissionKeyKind.ApplicationId, PermissionKind.BullseyeViewPrivateModels)]
    [HttpGet("App/{appId}/Revisions")]
    public async Task<ActionResult> ListAppRevisions(string appId)
    {
        try
        {
            var app = await _bullseyeAppRepo.GetById(appId);
            if (app == null)
            {
                Response.StatusCode = 404;
                return Json(new ExceptionWebResponse(
                    new Exception(
                        $"Could not find Application with Id {appId} in {nameof(ApplicationDetailRepository)}")),
                    BaseService.SerializerOptions);
            }

            var result = await _bullseyeRevisionRepo.GetAllForApp(appId);
            Response.StatusCode = 200;
            return Json(result, BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// Trigger the <see cref="BullseyeCacheService.GenerateCache"/> method.
    /// </summary>
    /// <param name="appId"><inheritdoc cref="BullseyeCacheService.GenerateCache" path="/param[@name='appId']"/></param>
    /// <param name="publishedOnly"><inheritdoc cref="BullseyeCacheService.GenerateCache" path="/param[@name='publishedOnly']"/></param>
    /// <param name="setLiveState"><inheritdoc cref="BullseyeCacheService.GenerateCache" path="/param[@name='setLiveState']"/></param>
    /// <remarks>
    /// Requires the <see cref="PermissionKind.BullseyeGenerateCache"/> permission.
    /// </remarks>
    [ProducesResponseType(typeof(ManageBullseyeV1GenerateCacheResponse), 200, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [AuthRequired]
    [ScopedPermissionRequired("appId", ScopedPermissionKeyKind.ApplicationId, PermissionKind.BullseyeGenerateCache)]
    [HttpPost("App/{appId}/Cache")]
    public async Task<ActionResult> GenerateCache(
        string appId,
        [ModelBinder(typeof(JsonModelBinder))] [FromBody] ManageBullseyeV1GenerateCacheRequest data)
    {
        try
        {
            var app = await _appDetailRepo.GetById(appId);
            if (app == null)
            {
                Response.StatusCode = 404;
                return Json(new ExceptionWebResponse(
                    new Exception(
                        $"Could not find Application with Id {appId} in {nameof(ApplicationDetailRepository)}")),
                    BaseService.SerializerOptions);
            }

            var response = await _bullCacheService.GenerateCache(appId, data.PublishedOnly, data.SetLiveState);
            Response.StatusCode = 200;
            return Json(new ManageBullseyeV1GenerateCacheResponse()
            {
                CacheV1Id = response.V1.Id,
                CacheV2Id = response.V2.Id,
                IsNewBullseyeApp = response.IsNewBullseyeApp
            }, BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    [ProducesResponseType(typeof(List<ManageBullseyeV1GenerateCacheResponse>), 200, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [AuthRequired]
    [PermissionRequired(PermissionKind.BullseyeGenerateCache)]
    [ScopedPermissionRequired("appId", ScopedPermissionKeyKind.ApplicationId, PermissionKind.BullseyeGenerateCache)]
    [HttpPost("App/{appId}/Cache/Auto")]
    public async Task<ActionResult> GenerateCacheAuto(string appId)
    {
        try
        {
            var app = await _appDetailRepo.GetById(appId);
            if (app == null)
            {
                Response.StatusCode = 404;
                return Json(new ExceptionWebResponse(
                    new Exception(
                        $"Could not find Application with Id {appId} in {nameof(ApplicationDetailRepository)}")),
                    BaseService.SerializerOptions);
            }
            var prms = new List<(bool, bool)>()
            {
                (false, false),
                (true, true)
            };
            var result = new List<ManageBullseyeV1GenerateCacheResponse>();
            foreach (var (a, b) in prms)
            {
                var response = await _bullCacheService.GenerateCache(appId, a, b);
                var x = new ManageBullseyeV1GenerateCacheResponse()
                {
                    CacheV1Id = response.V1.Id,
                    CacheV2Id = response.V2.Id,
                    IsNewBullseyeApp = response.IsNewBullseyeApp
                };
                result.Add(x);
            }
            Response.StatusCode = 200;
            return Json(result, BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// Register a patch. <see cref="BullseyeAppRevisionModel.BullseyeAppId"/> must match for the
    /// <see cref="ManageBullseyeV1RegisterPatchRequest.FromRevisionId"/> and <see cref="ManageBullseyeV1RegisterPatchRequest.ToRevisionId"/> revisions.
    /// </summary>
    /// <remarks>
    /// Requires the <see cref="PermissionKind.BullseyeRegisterPatch"/> permission.
    /// </remarks>
    [ProducesResponseType(typeof(BullseyePatchModel), 200, "application/json")]
    [ProducesResponseType(typeof(NotFoundResponse), 404, "application/json")]
    [ProducesResponseType(typeof(RevisionParentAppMismatchResponse), 409, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [AuthRequired]
    [HttpPost("RegisterPatch")]
    public async Task<ActionResult> RegisterPatch(
        [ModelBinder(typeof(JsonModelBinder))] [FromBody] ManageBullseyeV1RegisterPatchRequest data)
    {
        try
        {
            var fromRevision = await _bullseyeRevisionRepo.GetById(data.FromRevisionId);
            if (fromRevision == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundResponse()
                {
                    Message = $"Could not find {nameof(BullseyeAppRevisionModel)} with Id {data.FromRevisionId}",
                    PropertyName = nameof(data.FromRevisionId),
                    PropertyParentType = CockatooHelper.FormatTypeName(data.GetType())
                }, BaseService.SerializerOptions);
            }

            var toRevision = await _bullseyeRevisionRepo.GetById(data.ToRevisionId);
            if (toRevision == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundResponse()
                {
                    Message = $"Could not find {nameof(BullseyeAppRevisionModel)} with Id {data.ToRevisionId}",
                    PropertyName = nameof(data.ToRevisionId),
                    PropertyParentType = CockatooHelper.FormatTypeName(data.GetType())
                }, BaseService.SerializerOptions);
            }

            if (_scopedPermissionWebService.TryHandleManualCheck(
                    HttpContext, toRevision.BullseyeAppId, PermissionKind.BullseyeRegisterPatch, out var xp))
            {
                Response.StatusCode = xp!.StatusCode;
                return xp.ActionResult;
            }

            if (fromRevision.BullseyeAppId != toRevision.BullseyeAppId)
            {
                Response.StatusCode = (int)HttpStatusCode.Conflict;
                var x = new RevisionParentAppMismatchResponse()
                {
                    Items = [
                        new RevisionParentAppMismatchResponse.RevisionParentAppMismtchItem()
                        {
                            RevisionId = fromRevision.Id,
                            AppId = fromRevision.BullseyeAppId,
                            PropertyName = nameof(data.FromRevisionId),
                            PropertyParent = CockatooHelper.FormatTypeName(data.GetType())
                        },
                        new RevisionParentAppMismatchResponse.RevisionParentAppMismtchItem()
                        {
                            RevisionId = toRevision.Id,
                            AppId = toRevision.BullseyeAppId,
                            PropertyName = nameof(data.ToRevisionId),
                            PropertyParent = CockatooHelper.FormatTypeName(data.GetType())
                        },
                    ]
                };
                x.Message = x.BuildMessage();
                return Json(x, BaseService.SerializerOptions);
            }

            var archiveFile = await _storageFileRepo.GetById(data.PatchFileId);
            if (archiveFile == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundResponse()
                {
                    Message = $"Could not find {nameof(StorageFileModel)} with Id {data.PatchFileId}",
                    PropertyName = nameof(data.PatchFileId),
                    PropertyParentType = CockatooHelper.FormatTypeName(data.GetType())
                }, BaseService.SerializerOptions);
            }

            StorageFileModel? peerToPeerFile = null;
            if (string.IsNullOrEmpty(data.PeerToPeerFileId) == false)
            {
                peerToPeerFile = await _storageFileRepo.GetById(data.PeerToPeerFileId);
                if (peerToPeerFile == null)
                {
                    Response.StatusCode = 404;
                    return Json(new NotFoundResponse()
                    {
                        Message = $"Could not find {nameof(StorageFileModel)} with Id {data.PeerToPeerFileId}",
                        PropertyName = nameof(data.PeerToPeerFileId),
                        PropertyParentType = CockatooHelper.FormatTypeName(data.GetType())
                    }, BaseService.SerializerOptions);
                }
            }

            var model = new BullseyePatchModel()
            {
                FromRevisionId = fromRevision.Id,
                ToRevisionId = toRevision.Id,
                StorageFileId = archiveFile.Id,
                PeerToPeerStorageFileId = peerToPeerFile?.Id
            };
            model = await _bullseyePatchRepo.InsertOrUpdate(model);
            Response.StatusCode = 200;
            return Json(model, BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// <para>Create a new revision for an App.</para>
    /// </summary>
    /// <remarks>
    /// Requires the <see cref="PermissionKind.BullseyeRegisterRevision"/> permission.
    /// </remarks>
    [ProducesResponseType(typeof(BullseyeAppRevisionModel), 200, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 400, "application/json")]
    [ProducesResponseType(typeof(NotFoundResponse), 404, "application/json")]
    [ProducesResponseType(typeof(RevisionParentAppMismatchResponse), 409, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [AuthRequired]
    [ScopedPermissionRequired("appId", ScopedPermissionKeyKind.ApplicationId, PermissionKind.BullseyeRegisterRevision)]
    [HttpPost("RegisterRevision")]
    public async Task<ActionResult> RegisterRevision(
        [FromQuery] string appId,
        [ModelBinder(typeof(JsonModelBinder))] [FromBody] ManageBullseyeV1RegisterRevisionRequest data)
    {
        try
        {
            var checkState = await _bullService.CanRegisterRevision(
                appId,
                data.Version,
                data.Tag,
                data.ArchiveFileId,
                data.PeerToPeerFileId,
                data.SignatureFileId,
                data.PreviousRevisionId);
            switch (checkState)
            {
                case BullseyeService.CanRegisterVersionResult.AppDetailNotFound:
                    Response.StatusCode = 404;
                    return Json(new NotFoundWebResponse(
                        typeof(ApplicationDetailModel),
                        nameof(ApplicationDetailModel.Id),
                        appId,
                        $"Could not find model in {nameof(ApplicationDetailRepository)}"), BaseService.SerializerOptions);
                case BullseyeService.CanRegisterVersionResult.AppDetailInvalidType:
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Json(
                        new ExceptionWebResponse(new ArgumentException(
                            $"Application {appId} has invalid {nameof(ApplicationDetailModel.Type)}. " +
                            $"Only {nameof(ApplicationDetailType)}.{nameof(ApplicationDetailType.Kachemak)} is valid.")),
                        BaseService.SerializerOptions);
                case BullseyeService.CanRegisterVersionResult.VersionAlreadyExists:
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Json(
                        new ExceptionWebResponse(
                            new Exception(
                                $"Revision already exists with the version provided (version: {data.Version}, appId: {appId})")),
                        BaseService.SerializerOptions);
                case BullseyeService.CanRegisterVersionResult.TagAlreadyExists:
                    Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    return Json(
                        new ExceptionWebResponse(
                            new Exception(
                                $"Revision already exists with the tag provided (tag: {data.Tag}, appId: {appId})")),
                        BaseService.SerializerOptions);
                case BullseyeService.CanRegisterVersionResult.ArchiveFileNotFound:
                    Response.StatusCode = 404;
                    return Json(new NotFoundResponse()
                    {
                        Message = $"Couldn't find {nameof(StorageFileModel)} with Id {data.ArchiveFileId}",
                        PropertyName = nameof(data.ArchiveFileId),
                        PropertyParentType = CockatooHelper.FormatTypeName(data.GetType())
                    }, BaseService.SerializerOptions);
                case BullseyeService.CanRegisterVersionResult.PeerToPeerFileNotFound:
                    Response.StatusCode = 404;
                    return Json(new NotFoundResponse()
                    {
                        Message = $"Couldn't find {nameof(StorageFileModel)} with Id {data.PeerToPeerFileId}",
                        PropertyName = nameof(data.PeerToPeerFileId),
                        PropertyParentType = CockatooHelper.FormatTypeName(data.GetType())
                    }, BaseService.SerializerOptions);
                case BullseyeService.CanRegisterVersionResult.SignatureFileNotFound:
                    Response.StatusCode = 404;
                    return Json(new NotFoundResponse()
                    {
                        Message = $"Couldn't find {nameof(StorageFileModel)} with Id {data.SignatureFileId}",
                        PropertyName = nameof(data.SignatureFileId),
                        PropertyParentType = CockatooHelper.FormatTypeName(data.GetType())
                    }, BaseService.SerializerOptions);
                case BullseyeService.CanRegisterVersionResult.PreviousRevisionNotFound:
                    Response.StatusCode = 404;
                    return Json(new NotFoundResponse()
                    {
                        Message = $"Couldn't find {nameof(BullseyeAppRevisionModel)} with Id {data.PreviousRevisionId}",
                        PropertyName = nameof(data.PreviousRevisionId),
                        PropertyParentType = CockatooHelper.FormatTypeName(data.GetType())
                    }, BaseService.SerializerOptions);
                case BullseyeService.CanRegisterVersionResult.PreviousRevisionAppMismatch:
                    // known to be not-null since this result is only returned
                    // when the model previous revion exists
                    BullseyeAppRevisionModel previousRevisionModel = (await _bullseyeRevisionRepo.GetById(data.PreviousRevisionId!))!;
                    Response.StatusCode = (int)HttpStatusCode.Conflict;
                    var x = new RevisionParentAppMismatchResponse()
                    {
                        Items = [
                            new RevisionParentAppMismatchResponse.RevisionParentAppMismtchItem()
                            {
                                RevisionId = null,
                                AppId = appId,
                                PropertyName = "target"
                            },
                            new RevisionParentAppMismatchResponse.RevisionParentAppMismtchItem()
                            {
                                RevisionId = previousRevisionModel.Id,
                                AppId = previousRevisionModel.BullseyeAppId,
                                PropertyName = nameof(data.PreviousRevisionId),
                                PropertyParent = CockatooHelper.FormatTypeName(data.GetType())
                            }
                        ]
                    };
                    x.Message = x.BuildMessage();
                    return Json(x, BaseService.SerializerOptions);
            }

            var model = new BullseyeAppRevisionModel()
            {
                BullseyeAppId = appId,
                Version = data.Version.ToString(),
                ArchiveStorageFileId = data.ArchiveFileId,
                PeerToPeerStorageFileId = data.PeerToPeerFileId,
                SignatureStorageFileId = data.SignatureFileId,
                PreviousRevisionId = data.PreviousRevisionId,
                ExtractedArchiveSize = data.ExtractedArchiveSize?.ToString(),
                IsLive = data.IsLive
            };
            model = await _bullseyeRevisionRepo.InsertOrUpdate(model);
            Response.StatusCode = 200;
            return Json(model, BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }


    /// <summary>
    /// Update a Revision with the request <paramref name="data"/> provided.
    /// </summary>
    [ProducesResponseType(typeof(ComparisonResponse<BullseyeAppRevisionModel>), 200, "application/json")]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 401, "application/json")]
    [ProducesResponseType(typeof(NotFoundResponse), 404, "application/json")]
    [ProducesResponseType(typeof(RevisionParentAppMismatchResponse), 409, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [AuthRequired]
    [HttpPatch("Revision")]
    public async Task<ActionResult> UpdateRevisionProperties(
        [ModelBinder(typeof(JsonModelBinder))] [FromBody] ManageBullseyeV1UpdateRevisionRequest data)
    {
        try
        {
            var currentUser = await _authWebService.GetCurrentUser(HttpContext);
            if (currentUser == null)
                throw new NoNullAllowedException($"{nameof(AuthWebService)}.{nameof(AuthWebService.GetCurrentUser)} returned null");

            var revisionModel = await _bullseyeRevisionRepo.GetById(data.RevisionId);
            if (revisionModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BullseyeAppRevisionModel),
                    nameof(BullseyeAppRevisionModel.Id),
                    data.RevisionId,
                    $"Could not find model in {nameof(BullseyeAppRevisionRepository)} {nameof(data.RevisionId)}"), BaseService.SerializerOptions);
            }

            if (_scopedPermissionWebService.TryHandleManualCheck(
                    HttpContext, revisionModel.BullseyeAppId, PermissionKind.BullseyeUpdateRevisionLiveState, out var xp))
            {
                Response.StatusCode = xp!.StatusCode;
                return xp.ActionResult;
            }

            var result = await _manageBullseyeWebService.UpdateRevision(data);
            result.TryGet(out int code, out var body);
            Response.StatusCode = code;
            return Json(body, BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// <para>Update the Previous Revision Id for a Bullseye Revision.</para>
    ///
    /// <para>Will return 409 when the Bullseye App for both revision provided doesn't match (when <paramref name="previousRevisionId"/> <i>is</i> provided)</para>
    /// </summary>
    /// <param name="revisionId">Revision that is going to have the value <see cref="BullseyeAppRevisionModel.PreviousRevisionId"/> changed.</param>
    /// <param name="previousRevisionId">Value to set for <see cref="BullseyeAppRevisionModel.PreviousRevisionId"/>. When not <see langword="null"/>, it will be verified if it exists, and when it doesn't, Error 404 will be returned.</param>
    /// <remarks>
    /// Requires the <see cref="PermissionKind.BullseyeUpdatePreviousRevision"/> permission.
    /// </remarks>
    [ProducesResponseType(typeof(ComparisonResponse<BullseyeAppRevisionModel>), 200, "application/json")]
    [ProducesResponseType(typeof(NotFoundWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), (int)HttpStatusCode.Conflict, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [AuthRequired]
    [PermissionRequired(PermissionKind.BullseyeUpdatePreviousRevision)]
    [HttpPatch("Revision/{revisionId}/Previous")]
    public async Task<ActionResult> RevisionSetPreviousId(string revisionId, string? previousRevisionId)
    {
        try
        {
            var revisionModel = await _bullseyeRevisionRepo.GetById(revisionId);
            if (revisionModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BullseyeAppRevisionModel),
                    nameof(BullseyeAppRevisionModel.Id),
                    revisionId,
                    $"Could not find model in {nameof(BullseyeAppRevisionRepository)} {nameof(revisionId)}"), BaseService.SerializerOptions);
            }

            if (_scopedPermissionWebService.TryHandleManualCheck(
                    HttpContext, revisionModel.BullseyeAppId, PermissionKind.BullseyeUpdatePreviousRevision, out var xp))
            {
                Response.StatusCode = xp!.StatusCode;
                return xp.ActionResult;
            }

            BullseyeAppRevisionModel? previousRevisionModel = null;
            if (!string.IsNullOrEmpty(previousRevisionId))
            {
                // return 404 when previous revision not found
                previousRevisionModel = await _bullseyeRevisionRepo.GetById(previousRevisionId);
                if (previousRevisionModel == null)
                {
                    Response.StatusCode = 404;
                    return Json(new NotFoundWebResponse(
                        typeof(BullseyeAppRevisionModel),
                        nameof(BullseyeAppRevisionModel.Id),
                        previousRevisionId,
                        $"Could not find model in {nameof(BullseyeAppRevisionRepository)} from parameter {nameof(previousRevisionId)}"), BaseService.SerializerOptions);
                }

                // return 409 when bullseye app mismatch
                if (revisionModel.BullseyeAppId != previousRevisionModel.BullseyeAppId)
                {
                    Response.StatusCode = (int)HttpStatusCode.Conflict;
                    return Json(
                        new ExceptionWebResponse(
                            new Exception(
                                $"Revisions provided are for different apps. (target: {revisionModel.BullseyeAppId}, previous: {previousRevisionModel.Id})")),
                        BaseService.SerializerOptions);
                }
            }

            // save before state, update model, then return comparison
            var before = revisionModel.JsonClone()!;
            revisionModel.PreviousRevisionId = previousRevisionModel?.Id;
            revisionModel = await _bullseyeRevisionRepo.InsertOrUpdate(revisionModel);
            Response.StatusCode = 200;
            return Json(new ComparisonResponse<BullseyeAppRevisionModel>(before, revisionModel), BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// Update the <see cref="BullseyeAppRevisionModel.IsLive"/> property.
    /// </summary>
    /// <param name="revisionId">Revision to update. Will return 404 when not found.</param>
    /// <param name="value">New value for <see cref="BullseyeAppRevisionModel.IsLive"/></param>
    /// <remarks>
    /// Requires the <see cref="PermissionKind.BullseyeUpdateRevisionLiveState"/> permission.
    /// </remarks>
    [ProducesResponseType(typeof(ComparisonResponse<BullseyeAppRevisionModel>), 200, "application/json")]
    [ProducesResponseType(typeof(NotFoundWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [AuthRequired]
    [HttpPatch("Revision/{revisionId}/Live")]
    public async Task<ActionResult> RevisionSetLiveState(string revisionId, bool value)
    {
        try
        {
            var revisionModel = await _bullseyeRevisionRepo.GetById(revisionId);
            if (revisionModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BullseyeAppRevisionModel),
                    nameof(BullseyeAppRevisionModel.Id),
                    revisionId,
                    $"Could not find model in {nameof(BullseyeAppRevisionRepository)} {nameof(revisionId)}"), BaseService.SerializerOptions);
            }

            if (_scopedPermissionWebService.TryHandleManualCheck(
                    HttpContext, revisionModel.BullseyeAppId, PermissionKind.BullseyeUpdateRevisionLiveState, out var xp))
            {
                Response.StatusCode = xp!.StatusCode;
                return xp.ActionResult;
            }

            // save before state, update model, then return comparison
            var before = revisionModel.JsonClone()!;
            revisionModel.IsLive = value;
            revisionModel = await _bullseyeRevisionRepo.InsertOrUpdate(revisionModel);
            Response.StatusCode = 200;
            return Json(new ComparisonResponse<BullseyeAppRevisionModel>(before, revisionModel), BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// Mark what revision is the latest for a specific app.
    /// </summary>
    /// <param name="appId"><see cref="BullseyeAppModel.ApplicationDetailModelId"/></param>
    /// <param name="revisionId"><see cref="BullseyeAppRevisionModel.Id"/></param>
    /// <remarks>
    /// Requires the <see cref="PermissionKind.BullseyeAppMarkLatestRevision"/> permission.
    /// </remarks>
    [ProducesResponseType(typeof(ComparisonResponse<BullseyeAppModel>), 200, "application/json")]
    [ProducesResponseType(typeof(NotFoundWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 409, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [AuthRequired]
    [ScopedPermissionRequired("appId", ScopedPermissionKeyKind.ApplicationId, PermissionKind.BullseyeAppMarkLatestRevision)]
    [HttpPatch("App/{appId}/LatestRevision/{revisionId}")]
    public async Task<ActionResult> SetLatestRevision(
        string appId,
        string revisionId)
    {
        try
        {
            var appModel = await _bullseyeAppRepo.GetById(appId);
            if (appModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BullseyeAppModel),
                    nameof(BullseyeAppModel.Id),
                    appId,
                    $"Could not find model in {nameof(BullseyeAppRepository)}"), BaseService.SerializerOptions);
            }

            var revisionModel = await _bullseyeRevisionRepo.GetById(revisionId);
            if (revisionModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BullseyeAppRevisionModel),
                    nameof(BullseyeAppRevisionModel.Id),
                    revisionId,
                    $"Could not find model in {nameof(BullseyeAppRevisionRepository)}"), BaseService.SerializerOptions);
            }
            if (revisionModel.BullseyeAppId != appModel.ApplicationDetailModelId)
            {
                Response.StatusCode = (int)HttpStatusCode.Conflict;
                return Json(
                    new ExceptionWebResponse(
                        new Exception(
                            $"Revision provided is from a different app. (revision: {revisionModel.BullseyeAppId}, app: {appModel.ApplicationDetailModelId})")),
                    BaseService.SerializerOptions);
            }

            var before = JsonSerializer.Deserialize<BullseyeAppModel>(JsonSerializer.Serialize(appModel, BaseService.SerializerOptions), BaseService.SerializerOptions);

            appModel.LatestRevisionId = revisionModel.Id;
            appModel = await _bullseyeAppRepo.InsertOrUpdate(appModel);

            Response.StatusCode = 200;
            return Json(new ComparisonResponse<BullseyeAppModel>(before, appModel), BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// Set the latest revision to <see langword="null"/>.
    /// </summary>
    /// <param name="appId"><see cref="BullseyeAppModel.ApplicationDetailModelId"/></param>
    /// <remarks>
    /// Requires the <see cref="PermissionKind.BullseyeAppMarkLatestRevision"/> permission.
    /// </remarks>
    [ProducesResponseType(typeof(ComparisonResponse<BullseyeAppModel>), 200, "application/json")]
    [ProducesResponseType(typeof(NotFoundWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [AuthRequired]
    [ScopedPermissionRequired("appId", ScopedPermissionKeyKind.ApplicationId, PermissionKind.BullseyeAppMarkLatestRevision)]
    [HttpDelete("App/{appId}/LatestRevision")]
    public async Task<ActionResult> UnmarkLatestRevision(
        string appId)
    {
        try
        {
            var appModel = await _bullseyeAppRepo.GetById(appId);
            if (appModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(BullseyeAppModel),
                    nameof(BullseyeAppModel.Id),
                    appId,
                    $"Could not find model in {nameof(BullseyeAppRepository)}"), BaseService.SerializerOptions);
            }

            var before = JsonSerializer.Deserialize<BullseyeAppModel>(JsonSerializer.Serialize(appModel, BaseService.SerializerOptions), BaseService.SerializerOptions);

            appModel.LatestRevisionId = null;
            appModel = await _bullseyeAppRepo.InsertOrUpdate(appModel);

            Response.StatusCode = 200;
            return Json(new ComparisonResponse<BullseyeAppModel>(before, appModel), BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <inheritdoc cref="ManageBullseyeV1DeleteRequest"/>
    [ProducesResponseType(typeof(ManageBullseyeV1DeleteResponse), 200, "application/json")]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 401, "application/json")]
    [ProducesResponseType(typeof(NotFoundResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [AuthRequired]
    [PermissionRequired(PermissionKind.BullseyeAdmin)]
    [HttpDelete("App")]
    public async Task<ActionResult> DeleteApp(
        [ModelBinder(typeof(JsonModelBinder))] [FromBody] ManageBullseyeV1DeleteRequest data)
    {
        try
        {
            var appModel = await _bullseyeAppRepo.GetById(data.AppId);
            if (appModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundResponse()
                {
                    Message = $"Could not find {nameof(BullseyeAppModel)} with Id {data.AppId}",
                    PropertyName = nameof(data.AppId),
                    PropertyParentType = CockatooHelper.FormatTypeName(data.GetType())
                });
            }

            Response.StatusCode = 200;
            var result = await _bullService.DeleteBullseyeApp(data);
            return Json(result, BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    [ProducesResponseType(typeof(ManageBullseyeV1DeleteRevisionResponse), 200, "application/json")]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 401, "application/json")]
    [ProducesResponseType(typeof(NotFoundResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [AuthRequired]
    [PermissionRequired(PermissionKind.BullseyeDeleteRevision)]
    [HttpDelete("Revision/{revisionId}")]
    public async Task<ActionResult> DeleteRevision(string revisionId)
    {
        try
        {
            var revisionModel = await _bullseyeRevisionRepo.GetById(revisionId);
            if (revisionModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundResponse()
                {
                    Message = $"Could not find {nameof(BullseyeAppRevisionModel)} with Id {revisionId}",
                    PropertyName = nameof(revisionId)
                });
            }

            if (_scopedPermissionWebService.TryHandleManualCheck(
                    HttpContext, revisionModel.BullseyeAppId, PermissionKind.BullseyeManageRevision, out var xp))
            {
                Response.StatusCode = xp!.StatusCode;
                return xp.ActionResult;
            }

            Response.StatusCode = 200;
            var result = await _bullService.DeleteBullseyeRevision(revisionId);
            return Json(result, BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    [ProducesResponseType(typeof(BullseyeAppRevisionModel), 200, "application/json")]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 401, "application/json")]
    [ProducesResponseType(typeof(NotFoundResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [AuthRequired]
    [HttpGet("Revision/{revisionId}")]
    public async Task<ActionResult> GetRevision(string revisionId)
    {
        try
        {
            var model = await _bullseyeRevisionRepo.GetById(revisionId);
            if (model == null || string.IsNullOrEmpty(revisionId))
            {
                Response.StatusCode = 404;
                return Json(new NotFoundResponse()
                {
                    Message = $"Could not find {nameof(BullseyeAppRevisionModel)} with Id {revisionId}",
                    PropertyName = nameof(revisionId)
                });
            }

            if (_scopedPermissionWebService.TryHandleManualCheck(
                    HttpContext, model.BullseyeAppId, PermissionKind.BullseyeViewPrivateModels, out var xp))
            {
                Response.StatusCode = xp!.StatusCode;
                return xp.ActionResult;
            }

            Response.StatusCode = 200;
            return Json(model, BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// Get a list of <see cref="BullseyePatchModel"/> where <see cref="BullseyePatchModel.ToRevisionId"/> or <see cref="BullseyePatchModel.FromRevisionId"/> equals the <paramref name="revisionId"/> provided.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="PermissionKind.BullseyeViewPrivateModels"/>
    /// </remarks>
    [ProducesResponseType(typeof(List<BullseyePatchModel>), 200, "application/json")]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 401, "application/json")]
    [ProducesResponseType(typeof(NotFoundResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [AuthRequired]
    [HttpGet("Revision/{revisionId}/Patches")]
    public async Task<ActionResult> GetRevisionPatches(string revisionId)
    {
        try
        {
            var revision = await _bullseyeRevisionRepo.GetById(revisionId);
            if (revision == null || string.IsNullOrEmpty(revisionId))
            {
                Response.StatusCode = 404;
                return Json(new NotFoundResponse()
                {
                    Message = $"Could not find {nameof(BullseyeAppRevisionModel)} with Id {revisionId}",
                    PropertyName = nameof(revisionId)
                });
            }

            if (_scopedPermissionWebService.TryHandleManualCheck(
                    HttpContext, revision.BullseyeAppId, PermissionKind.BullseyeViewPrivateModels, out var xp))
            {
                Response.StatusCode = xp!.StatusCode;
                return xp.ActionResult;
            }

            Response.StatusCode = 200;
            var result = await _bullseyePatchRepo.GetAllWithRevision(revision.Id);
            return Json(result, BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// Get a list of <see cref="BullseyePatchModel"/> where <see cref="BullseyePatchModel.ToRevisionId"/> equals the <paramref name="revisionId"/> provided.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="PermissionKind.BullseyeViewPrivateModels"/>
    /// </remarks>
    [ProducesResponseType(typeof(List<BullseyePatchModel>), 200, "application/json")]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 401, "application/json")]
    [ProducesResponseType(typeof(NotFoundResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [AuthRequired]
    [HttpGet("Revision/{revisionId}/WhereCanPatchTo")]
    public async Task<ActionResult> GetRevisionPatchesTo(string revisionId)
    {
        try
        {
            var revision = await _bullseyeRevisionRepo.GetById(revisionId);
            if (revision == null || string.IsNullOrEmpty(revisionId))
            {
                Response.StatusCode = 404;
                return Json(new NotFoundResponse()
                {
                    Message = $"Could not find {nameof(BullseyeAppRevisionModel)} with Id {revisionId}",
                    PropertyName = nameof(revisionId)
                });
            }
            
            if (_scopedPermissionWebService.TryHandleManualCheck(
                    HttpContext, revision.BullseyeAppId, PermissionKind.BullseyeViewPrivateModels, out var xp))
            {
                Response.StatusCode = xp!.StatusCode;
                return xp.ActionResult;
            }

            Response.StatusCode = 200;
            var result = await _bullseyePatchRepo.GetAllRevisionTo(revision.Id);
            return Json(result, BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// Get a list of <see cref="BullseyePatchModel"/> where <see cref="BullseyePatchModel.FromRevisionId"/> equals the <paramref name="revisionId"/> provided.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="PermissionKind.BullseyeViewPrivateModels"/>
    /// </remarks>
    [ProducesResponseType(typeof(List<BullseyePatchModel>), 200, "application/json")]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 401, "application/json")]
    [ProducesResponseType(typeof(NotFoundResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [AuthRequired]
    [HttpGet("Revision/{revisionId}/WhereCanPatchFrom")]
    public async Task<ActionResult> GetRevisionPatchesFrom(string revisionId)
    {
        try
        {
            var revision = await _bullseyeRevisionRepo.GetById(revisionId);
            if (revision == null || string.IsNullOrEmpty(revisionId))
            {
                Response.StatusCode = 404;
                return Json(new NotFoundResponse()
                {
                    Message = $"Could not find {nameof(BullseyeAppRevisionModel)} with Id {revisionId}",
                    PropertyName = nameof(revisionId)
                });
            }
            
            if (_scopedPermissionWebService.TryHandleManualCheck(
                    HttpContext, revision.BullseyeAppId, PermissionKind.BullseyeViewPrivateModels, out var xp))
            {
                Response.StatusCode = xp!.StatusCode;
                return xp.ActionResult;
            }

            Response.StatusCode = 200;
            var result = await _bullseyePatchRepo.GetAllRevisionFrom(revision.Id);
            return Json(result, BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    [ProducesResponseType(typeof(ManageBullseyeV1DeletePatchResponse), 200, "application/json")]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 401, "application/json")]
    [ProducesResponseType(typeof(NotFoundResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [AuthRequired]
    [HttpDelete("Patch/{patchId}")]
    public async Task<ActionResult> DeletePatch(string patchId)
    {
        try
        {
            var patch = await _bullseyePatchRepo.GetById(patchId);
            if (patch == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundResponse()
                {
                    Message = $"Could not find {nameof(BullseyePatchModel)} with Id {patchId}",
                    PropertyName = nameof(patchId)
                });
            }

            var user = await _authWebService.GetCurrentUser(HttpContext);
            if (user == null)
            {
                throw new InvalidOperationException(
                    $"This was checked already, since this action has {nameof(AuthRequiredAttribute)}");
            }

            var revision = await _bullseyeRevisionRepo.GetById(patch.ToRevisionId);
            revision ??= await _bullseyeRevisionRepo.GetById(patch.FromRevisionId);
            if (revision == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundResponse()
                {
                    Message = $"Could not find {nameof(BullseyePatchModel)} with Id {patch.FromRevisionId} or {patch.ToRevisionId}",
                    PropertyName = nameof(patch.FromRevisionId),
                    PropertyParentType = CockatooHelper.FormatTypeName(patch.GetType())
                });
            }
            
            if (_scopedPermissionWebService.TryHandleManualCheck(
                    HttpContext, revision.BullseyeAppId, PermissionKind.BullseyeDeletePatch, out var xp))
            {
                Response.StatusCode = xp!.StatusCode;
                return xp.ActionResult;
            }

            Response.StatusCode = 200;
            var result = await _bullService.DeletePatch(patchId);
            return Json(result, BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }
}