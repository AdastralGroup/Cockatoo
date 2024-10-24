using System.Data;
using System.Net;
using System.Text.Json;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.Common.Helpers;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.Services.WebApi.Models.Request;
using Adastral.Cockatoo.Services.WebApi.Models.Response;
using Microsoft.Extensions.DependencyInjection;

namespace Adastral.Cockatoo.Services.WebApi;

[CockatooDependency]
public class ManageBullseyeWebService : BaseService
{
    private readonly PermissionService _permissionService;
    private readonly BullseyeAppRevisionRepository _bullseyeAppRevisionRepo;
    private readonly BullseyePatchRepository _bullseyePatchRepo;
    private readonly StorageFileRepository _storageFileRepo;
    private readonly StorageService _storageService;
    public ManageBullseyeWebService(IServiceProvider services)
        : base(services)
    {
        _permissionService = services.GetRequiredService<PermissionService>();
        _bullseyeAppRevisionRepo = services.GetRequiredService<BullseyeAppRevisionRepository>();
        _bullseyePatchRepo = services.GetRequiredService<BullseyePatchRepository>();
        _storageFileRepo = services.GetRequiredService<StorageFileRepository>();
        _storageService = services.GetRequiredService<StorageService>();
    }
    public class CanUserPerformRequestResult
    {
        public bool Success { get; set; } = false;
        public List<PermissionKind> MissingPermissions { get; set; } = [];
        public CanUserPerformRequestResult()
            : this(true, [])
        { }
        public CanUserPerformRequestResult(IList<PermissionKind> missingPermissions)
            : this(missingPermissions.Count < 1, missingPermissions)
        { }
        public CanUserPerformRequestResult(bool success, IList<PermissionKind> missingPermissions)
        {
            Success = success;
            MissingPermissions = new(missingPermissions);
        }
    }
    /// <summary>
    /// Check if the <paramref name="user"/> can execute the provided <paramref name="request"/>.
    /// </summary>
    public async Task<CanUserPerformRequestResult> CanUserPerformRequest(UserModel user, ManageBullseyeV1UpdateRevisionRequest request)
    {
        var missing = new List<PermissionKind>();
        if (request.IsLive != null)
        {
            if (await _permissionService.HasAnyPermissionsAsync(user, PermissionKind.BullseyeUpdateRevisionLiveState) == false)
            {
                missing.Add(PermissionKind.BullseyeUpdateRevisionLiveState);
            }
        }
        if (string.IsNullOrEmpty(request.PreviousRevisionId) == false || (request.ClearPreviousRevisionId ?? false))
        {
            if (await _permissionService.HasAnyPermissionsAsync(user, PermissionKind.BullseyeUpdatePreviousRevision) == false)
            {
                missing.Add(PermissionKind.BullseyeUpdatePreviousRevision);
            }
        }
        return new(missing);
    }

    public class UpdateRevisionResult
    {
        public bool Success { get; set; }
        public NotFoundResponse? NotFoundResponse { get; set; }
        public RevisionParentAppMismatchResponse? RevisionParentAppMismatchResponse { get; set; }
        public ComparisonResponse<BullseyeAppRevisionModel>? Data { get; set; }
        public void TryGet(out int statusCode, out object? body)
        {
            if (Success)
            {
                if (Data == null)
                    throw new NoNullAllowedException($"{nameof(Data)} is null when {nameof(Success)} is true");
                statusCode = 200;
                body = Data;
                return;
            }
            else
            {
                if (NotFoundResponse != null)
                {
                    statusCode = 404;
                    body = NotFoundResponse;
                    return;
                }
                else if (RevisionParentAppMismatchResponse != null)
                {
                    statusCode = 409;
                    body = RevisionParentAppMismatchResponse;
                    return;
                }
            }
            statusCode = 500;
            body = null;
            throw new NotImplementedException($"{nameof(Success)} is false, but no suitable responses were found.");
        }
    }
    public async Task<UpdateRevisionResult> UpdateRevision(ManageBullseyeV1UpdateRevisionRequest request)
    {
        var revisionModel = await _bullseyeAppRevisionRepo.GetById(request.RevisionId);
        if (revisionModel == null)
        {
            return new()
            {
                Success = false,
                NotFoundResponse = new()
                {
                    Message = $"Could not find {nameof(BullseyeAppRevisionModel)} with Id {request.RevisionId}",
                    PropertyName = nameof(request.RevisionId),
                    PropertyParentType = CockatooHelper.FormatTypeName(request.GetType())
                }
            };
        }

        var before = JsonSerializer.Deserialize<BullseyeAppRevisionModel>(JsonSerializer.Serialize(revisionModel, BaseService.SerializerOptions), BaseService.SerializerOptions)!;

        // update IsLive
        if (request.IsLive != null)
        {
            revisionModel.IsLive = (bool)request.IsLive;
        }

        // update PreviousRevisionId
        if (request.ClearPreviousRevisionId ?? false)
        {
            revisionModel.PreviousRevisionId = null;
        }
        else if (string.IsNullOrEmpty(request.PreviousRevisionId) == false)
        {
            var previousRevisionModel = await _bullseyeAppRevisionRepo.GetById(request.PreviousRevisionId);
            if (previousRevisionModel == null)
            {
                return new()
                {
                    Success = false,
                    NotFoundResponse = new()
                    {
                        Message = $"Could not find Previous Revision ({nameof(BullseyeAppRevisionModel)}) with Id {request.PreviousRevisionId}",
                        PropertyName = nameof(request.PreviousRevisionId),
                        PropertyParentType = CockatooHelper.FormatTypeName(request.GetType())
                    }
                };
            }
            if (previousRevisionModel.BullseyeAppId != revisionModel.BullseyeAppId)
            {
                return new()
                {
                    Success = false,
                    RevisionParentAppMismatchResponse = new()
                    {
                        Items = [
                            new RevisionParentAppMismatchResponse.RevisionParentAppMismtchItem()
                            {
                                RevisionId = revisionModel.Id,
                                AppId = revisionModel.BullseyeAppId,
                                PropertyName = nameof(request.RevisionId),
                                PropertyParent = CockatooHelper.FormatTypeName(request.GetType())
                            },
                            new RevisionParentAppMismatchResponse.RevisionParentAppMismtchItem()
                            {
                                RevisionId = previousRevisionModel.Id,
                                AppId = previousRevisionModel.BullseyeAppId,
                                PropertyName = nameof(request.PreviousRevisionId),
                                PropertyParent = CockatooHelper.FormatTypeName(request.GetType())
                            }
                        ]
                    }
                };
            }
        }

        revisionModel = await _bullseyeAppRevisionRepo.InsertOrUpdate(revisionModel);

        return new()
        {
            Success = true,
            Data = new(before, revisionModel)
        };
    }
}