using System.Data;
using System.Security.AccessControl;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.Common.Helpers;
using Adastral.Cockatoo.DataAccess;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.DataAccess.Repositories.Group;
using Adastral.Cockatoo.Services.WebApi.Models.Request;
using Adastral.Cockatoo.Services.WebApi.Models.Response;
using kate.shared.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using InsertPermissionResult = Adastral.Cockatoo.Services.WebApi.AdminGroupWebService.InsertPermissionResult;
using RevokePermissionResult = Adastral.Cockatoo.Services.WebApi.AdminGroupWebService.RevokePermissionResult;
using CreateResult = Adastral.Cockatoo.Services.WebApi.AdminGroupWebService.CreateResult;
using CreateResultKind = Adastral.Cockatoo.Services.WebApi.AdminGroupWebService.CreateResultKind;

namespace Adastral.Cockatoo.Services.WebApi.Controllers;

[ApiController]
[AuthRequired]
[Route("~/api/v1/Admin/Group")]
[TrackRequest]
public class AdminGroupApiV1Controller : Controller
{
    private readonly ApplicationDetailRepository _applicationRepo;
    private readonly GroupRepository _groupRepo;
    private readonly GroupPermissionGlobalRepository _groupPermGlobalRepo;
    private readonly GroupPermissionApplicationRepository _groupPermApplicationRepo;
    private readonly GroupUserAssociationRepository _groupUserAssocRepo;
    private readonly AdminGroupWebService _adminGroupWebService;
    public AdminGroupApiV1Controller(IServiceProvider services)
        : base()
    {
        _applicationRepo = services.GetRequiredService<ApplicationDetailRepository>();
        _groupRepo = services.GetRequiredService<GroupRepository>();
        _groupPermGlobalRepo = services.GetRequiredService<GroupPermissionGlobalRepository>();
        _groupPermApplicationRepo = services.GetRequiredService<GroupPermissionApplicationRepository>();
        _groupUserAssocRepo = services.GetRequiredService<GroupUserAssociationRepository>();
        _adminGroupWebService = services.GetRequiredService<AdminGroupWebService>();
    }

    [HttpPost("Create")]
    [ProducesResponseType(typeof(AdminGroupV1DetailResponse), 200, "application/json")]
    [ProducesResponseType(typeof(PropertyErrorResponse), 400, "application/json")]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 401, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 409, "application/json")]
    [PermissionRequired(PermissionKind.GroupAdminCreate)]
    public async Task<ActionResult> Create(AdminGroupV1CreateRequest data)
    {
        var result = await _adminGroupWebService.Create(data);
        switch (result.Kind)
        {
            case AdminGroupWebService.CreateResultKind.Success:
                var details = await _adminGroupWebService.Details(result.GroupId!);
                if (details == null)
                {
                    throw new NoNullAllowedException($"{nameof(CreateResult)}.{nameof(AdminGroupWebService.CreateResult.Kind)} is {nameof(AdminGroupWebService.CreateResultKind.Success)}, but " +
                    $"{nameof(AdminGroupWebService)}.{nameof(AdminGroupWebService.Details)} returned null when trying to pass through {nameof(result)}.{nameof(result.GroupId)} ({result.GroupId})");
                }
                Response.StatusCode = 200;
                return Json(details, BaseService.SerializerOptions);
            case AdminGroupWebService.CreateResultKind.NameNotProvided:
                Response.StatusCode = 400;
                return Json(new PropertyErrorResponse()
                {
                    Message = $"Property is required",
                    PropertyName = nameof(data.Name),
                    PropertyParentType = CockatooHelper.FormatTypeName(data.GetType())
                }, BaseService.SerializerOptions);
            case AdminGroupWebService.CreateResultKind.NameAlreadyExists:
                Response.StatusCode = 409;
                return Json(new ExceptionWebResponse(new Exception($"A Group with the name \"{data.Name.Trim()}\" already exists.")), BaseService.SerializerOptions);
            default:
                throw new NotImplementedException($"{nameof(CreateResult)}.{nameof(AdminGroupWebService.CreateResult.Kind)}={result.Kind}\ndata={data.ToJson()}");
        }
    }

    [HttpGet("List")]
    [ProducesResponseType(typeof(AdminGroupV1ListResponse), 200, "application/json")]
    [PermissionRequired(PermissionKind.GroupAdminViewAll)]
    public async Task<ActionResult> List()
    {
        var data = await _adminGroupWebService.List();
        return Json(data, BaseService.SerializerOptions);
    }

    [HttpGet("{groupId}")]
    [ProducesResponseType(typeof(AdminGroupV1DetailResponse), 200, "application/json")]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 401, "application/json")]
    [ProducesResponseType(typeof(NotFoundResponse), 404, "application/json")]
    [PermissionRequired(PermissionKind.GroupAdminViewAll)]
    public async Task<ActionResult> Details(string groupId)
    {
        var data = await _adminGroupWebService.Details(groupId);
        if (data == null)
        {
            Response.StatusCode = 404;
            return Json(new NotFoundResponse()
            {
                Message = "Could not find Group with Id {groupId}",
                PropertyName = nameof(groupId)
            }, BaseService.SerializerOptions);
        }
        return Json(data, BaseService.SerializerOptions);
    }

    [HttpDelete("{groupId}")]
    [ProducesResponseType(typeof(EmptyResult), 200, "application/json")]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 401, "application/json")]
    [ProducesResponseType(typeof(NotFoundResponse), 404, "application/json")]
    [PermissionRequired(PermissionKind.GroupAdminViewAll)]
    public async Task<ActionResult> Delete(string groupId)
    {
        var response = await _adminGroupWebService.Delete(groupId);
        switch (response)
        {
            case AdminGroupWebService.DeleteResult.Success:
                return new OkResult();
            case AdminGroupWebService.DeleteResult.NotFound:
                Response.StatusCode = 404;
                return Json(new NotFoundResponse()
                {
                    Message = $"Could not find Group with Id {groupId}",
                    PropertyName = nameof(groupId)
                }, BaseService.SerializerOptions);
            default:
                throw new NotImplementedException(
                    $"Where result of {nameof(AdminGroupWebService)}.{nameof(AdminGroupWebService.Delete)} is not implemented (was {response})");
        }
    }

    [HttpPost("{groupId}/User/{userId}")]
    [ProducesResponseType(typeof(EmptyResult), 200, "application/json")]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 401, "application/json")]
    [ProducesResponseType(typeof(NotFoundResponse), 404, "application/json")]
    [PermissionRequired(PermissionKind.UserAdminAddToGroup)]
    public async Task<ActionResult> AddUserToGroup(string groupId, string userId)
    {
        var response = await _adminGroupWebService.AddUserToGroup(groupId, userId);
        switch (response)
        {
            case AdminGroupWebService.AddUserToGroupResult.Success:
                return new OkResult();
            case AdminGroupWebService.AddUserToGroupResult.GroupNotFound:
                Response.StatusCode = 404;
                return Json(new NotFoundResponse()
                {
                    Message = $"Could not find Group with Id {groupId}",
                    PropertyName = nameof(groupId)
                }, BaseService.SerializerOptions);
            case AdminGroupWebService.AddUserToGroupResult.UserNotFound:
                Response.StatusCode = 404;
                return Json(new NotFoundResponse()
                {
                    Message = $"Could not find User with Id {userId}",
                    PropertyName = nameof(userId)
                }, BaseService.SerializerOptions);
            default:
                throw new NotImplementedException(
                    $"Result={response}");
        }
    }

    [HttpDelete("{groupId}/User/{userId}")]
    [ProducesResponseType(typeof(List<string>), 200, "application/json")]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 401, "application/json")]
    [ProducesResponseType(404)]
    [AuthRequired]
    [PermissionRequired(PermissionKind.GroupAdmin, PermissionKind.UserAdmin)]
    public async Task<ActionResult> RemoveUserFromGroup(string groupId, string userId)
    {
        var deletedList = new List<string>();
        var targets = await _groupUserAssocRepo.GetAllWithUserAndGroup(userId, groupId);
        if (targets.Count < 1)
        {
            Response.StatusCode = 404;
            return new EmptyResult();
        }
        foreach (var item in targets)
        {
            deletedList.Add(item.Id);
        }
        await _groupUserAssocRepo.Delete(deletedList.ToArray());
        Response.StatusCode = 200;
        return Json(deletedList, BaseService.SerializerOptions);
    }

    #region Global Permissions
    /// <remarks>
    /// Returns 404 when the group could not be found.
    /// </remarks>
    [HttpPost("PermissionGrant/Global")]
    [ProducesResponseType(typeof(AdminGroupV1GrantGlobalPermissionResponse), 200, "application/json")]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 401, "application/json")]
    [ProducesResponseType(typeof(NotFoundResponse), 404, "application/json")]
    [PermissionRequired(PermissionKind.GroupAdminManagePermissions)]
    public async Task<ActionResult> GrantGlobalPermission(
        [ModelBinder(typeof(JsonModelBinder))] [FromBody]
        AdminGroupV1GrantGlobalPermissionRequest data)
    {
        var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var result = await _adminGroupWebService.GrantGlobalPermission(data.GroupId, data.Kind);
        var groupModel = await _groupRepo.GetById(data.GroupId);
        List<GroupPermissionGlobalModel> matchingPermissions = [];
        if (groupModel == null)
        {
            result = InsertPermissionResult.Failure | InsertPermissionResult.GroupNotFound;
        }
        else
        {
            matchingPermissions = await _groupPermGlobalRepo.GetManyBy(
                groupId: groupModel.Id, kinds: [data.Kind], allow: true);
        }

        if (result.HasFlag(InsertPermissionResult.Success))
        {
            if (matchingPermissions.Count < 1)
            {
                throw new InvalidOperationException(
                    $"{nameof(matchingPermissions)} is empty, even though {nameof(AdminGroupWebService)}.{nameof(AdminGroupWebService.GrantGlobalPermission)} returned {result} (which included {nameof(InsertPermissionResult.Success)})");
            }

            var response = new AdminGroupV1GrantGlobalPermissionResponse()
            {
                Group = groupModel!,
                Permission = matchingPermissions.First(),
                AlreadyExists = result.HasFlag(InsertPermissionResult.PermissionAlreadyExists),
                Duration = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start
            };
            Response.StatusCode = 200;
            return Json(response, BaseService.SerializerOptions);
        }
        else if (result.HasFlag(InsertPermissionResult.Failure))
        {
            if (result.HasFlag(InsertPermissionResult.GroupNotFound))
            {
                Response.StatusCode = 404;
                return Json(new NotFoundResponse()
                {
                    Message = $"Could not find Group with Id {data.GroupId}",
                    PropertyName = nameof(data.GroupId),
                    PropertyParentType = CockatooHelper.FormatTypeName(data.GetType())
                }, BaseService.SerializerOptions);
            }
        }

        throw new NotImplementedException($"All possible values were handled for {nameof(InsertPermissionResult)}! (result: {result})");
    }

    [HttpPost("PermissionDeny/Global")]
    [ProducesResponseType(typeof(AdminGroupV1DenyGlobalPermissionResponse), 200, "application/json")]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 401, "application/json")]
    [ProducesResponseType(typeof(NotFoundResponse), 404, "application/json")]
    [PermissionRequired(PermissionKind.GroupAdminDenyPermission)]
    public async Task<ActionResult> DenyGlobalPermission(
        [ModelBinder(typeof(JsonModelBinder))] [FromBody] AdminGroupV1DenyGlobalPermissionRequest data)
    {
        var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var result = await _adminGroupWebService.DenyGlobalPermission(data.GroupId, data.Kind);
        var groupModel = await _groupRepo.GetById(data.GroupId);

        List<GroupPermissionGlobalModel> matchingPermissions = [];
        if (groupModel == null)
        {
            result = InsertPermissionResult.Failure | InsertPermissionResult.GroupNotFound;
        }
        else
        {
            matchingPermissions = await _groupPermGlobalRepo.GetManyBy(
                groupId: groupModel.Id, kinds: [data.Kind], allow: true);
        }

        if (result.HasFlag(InsertPermissionResult.Success))
        {
            if (matchingPermissions.Count < 1)
            {
                throw new Exception($"{matchingPermissions} is empty, even though {nameof(AdminGroupWebService)}.{nameof(AdminGroupWebService.DenyGlobalPermission)} returned {result} (which included {nameof(AdminGroupWebService.InsertPermissionResult.Success)})");
            }
            var response = new AdminGroupV1DenyGlobalPermissionResponse()
            {
                Group = groupModel!, // will never be null since the success flag will be removed if it is.
                Permission = matchingPermissions.First()!,
                AlreadyExists = result.HasFlag(InsertPermissionResult.PermissionAlreadyExists),
                Duration = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start
            };
            Response.StatusCode = 200;
            return Json(response, BaseService.SerializerOptions);
        }
        else if (result.HasFlag(InsertPermissionResult.Failure))
        {
            if (result.HasFlag(InsertPermissionResult.GroupNotFound))
            {
                Response.StatusCode = 404;
                return Json(new NotFoundResponse()
                {
                    Message = $"Could not find Group with Id {data.GroupId}",
                    PropertyName = nameof(data.GroupId),
                    PropertyParentType = CockatooHelper.FormatTypeName(data.GetType())
                }, BaseService.SerializerOptions);
            }
        }

        throw new NotImplementedException($"All possible values were handled for {nameof(InsertPermissionResult)}! (result: {result})");
    }

    /// <remarks>
    /// Returns 200 even if the permission wasn't there in the first place. Returns 404 when the group could not be found.
    /// </remarks>
    [HttpPost("PermissionRevoke/Global")]
    [ProducesResponseType(typeof(AdminGroupV1DetailResponse), 200, "application/json")]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 401, "application/json")]
    [ProducesResponseType(typeof(NotFoundResponse), 404, "application/json")]
    public async Task<ActionResult> RevokeGlobalPermission(
        [ModelBinder(typeof(JsonModelBinder))] [FromBody] AdminGroupV1RevokeGlobalPermissionRequest data)
    {
        var result = await _adminGroupWebService.RevokeGlobalPermission(data.GroupId, data.Kind);

        var groupModel = await _groupRepo.GetById(data.GroupId);
        if (groupModel == null)
        {
            result = RevokePermissionResult.Failure | RevokePermissionResult.GroupNotFound;
        }
        if (result.HasFlag(RevokePermissionResult.Success) || result.HasFlag(RevokePermissionResult.PermissionNotFound))
        {
            var details = await _adminGroupWebService.Details(data.GroupId);
            if (details == null)
            {
                throw new NoNullAllowedException($"{nameof(AdminGroupWebService)}.{nameof(AdminGroupWebService.Details)} returned null " +
                                                 $"when {nameof(result)} contains {nameof(RevokePermissionResult.Success)}!");
            }
            Response.StatusCode = 200;
            return Json(details, BaseService.SerializerOptions);
        }
        else
        {
            if (result.HasFlag(RevokePermissionResult.GroupNotFound))
            {
                Response.StatusCode = 404;
                return Json(new NotFoundResponse()
                {
                    Message = $"Could not find Group with Id {data.GroupId}",
                    PropertyName = nameof(data.GroupId),
                    PropertyParentType = CockatooHelper.FormatTypeName(data.GetType())
                }, BaseService.SerializerOptions);
            }
        }

        throw new NotImplementedException($"All possible values were handled for {nameof(InsertPermissionResult)}! (result: {result})");
    }
    #endregion

    #region Application Permissions
    [HttpPost("PermissionGrant/Application")]
    [ProducesResponseType(typeof(AdminGroupV1GrantApplicationPermissionResponse), 200, "application/json")]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 401, "application/json")]
    [ProducesResponseType(typeof(NotFoundResponse), 404, "application/json")]
    [PermissionRequired(PermissionKind.GroupAdminManagePermissions)]
    public async Task<ActionResult> GrantApplicationPermission(
        [ModelBinder(typeof(JsonModelBinder))] [FromBody]
        AdminGroupV1GrantApplicationPermissionRequest data)
    {
        var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var result = await _adminGroupWebService.GrantApplicationPermission(data.GroupId, data.ApplicationId, data.Kind);
        var groupModel = await _groupRepo.GetById(data.GroupId);
        var applicationModel = await _applicationRepo.GetById(data.ApplicationId);
        List<GroupPermissionApplicationModel> matchingPermissions = [];
        if (groupModel == null)
        {
            result = InsertPermissionResult.Failure | InsertPermissionResult.GroupNotFound;
        }
        else
        {
            matchingPermissions = await _groupPermApplicationRepo.GetManyBy(new()
            {
                GroupId = groupModel.Id,
                ApplicationId = data.ApplicationId,
                KindsEq = [data.Kind],
                Allow = true
            });
        }

        if (applicationModel == null)
        {
            if (result.HasFlag(InsertPermissionResult.Failure))
            {
                result |= InsertPermissionResult.ApplicationNotFound;
            }
            else
            {
                result = InsertPermissionResult.Failure | InsertPermissionResult.ApplicationNotFound;
            }
        }

        if (result.HasFlag(InsertPermissionResult.Success))
        {
            if (matchingPermissions.Count < 1)
            {
                throw new InvalidOperationException(
                    $"{nameof(matchingPermissions)} is empty, even though {nameof(AdminGroupWebService)}.{nameof(AdminGroupWebService.GrantApplicationPermission)} returned {result} (which included {nameof(InsertPermissionResult.Success)})");
            }

            var response = new AdminGroupV1GrantApplicationPermissionResponse()
            {
                Group = groupModel!,
                Permission = matchingPermissions.First(),
                AlreadyExists = result.HasFlag(InsertPermissionResult.PermissionAlreadyExists),
                Duration = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start
            };
            Response.StatusCode = 200;
            return Json(response, BaseService.SerializerOptions);
        }
        else if (result.HasFlag(InsertPermissionResult.Failure))
        {
            if (result.HasFlag(InsertPermissionResult.GroupNotFound))
            {
                Response.StatusCode = 404;
                return Json(new NotFoundResponse()
                {
                    Message = $"Could not find Group with Id {data.GroupId}",
                    PropertyName = nameof(data.GroupId),
                    PropertyParentType = CockatooHelper.FormatTypeName(data.GetType())
                }, BaseService.SerializerOptions);
            }

            if (result.HasFlag(InsertPermissionResult.ApplicationNotFound))
            {
                Response.StatusCode = 404;
                return Json(new NotFoundResponse()
                {
                    Message = $"Could not find Application with Id {data.ApplicationId}",
                    PropertyName = nameof(data.ApplicationId),
                    PropertyParentType = CockatooHelper.FormatTypeName(data.GetType())
                }, BaseService.SerializerOptions);
            }
        }

        throw new NotImplementedException($"All possible values were handled for {nameof(InsertPermissionResult)}! (result: {result})");
    }

    [HttpPost("PermissionDeny/Application")]
    [ProducesResponseType(typeof(AdminGroupV1DenyApplicationPermissionResponse), 200, "application/json")]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 401, "application/json")]
    [ProducesResponseType(typeof(NotFoundResponse), 404, "application/json")]
    [PermissionRequired(PermissionKind.GroupAdminDenyPermission)]
    public async Task<ActionResult> DenyApplicationPermission(
        [ModelBinder(typeof(JsonModelBinder))] [FromBody]
        AdminGroupV1DenyApplicationPermissionRequest data)
    {
        var start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var result = await _adminGroupWebService.DenyApplicationPermission(data.GroupId, data.ApplicationId, data.Kind);
        var groupModel = await _groupRepo.GetById(data.GroupId);
        var applicationModel = await _applicationRepo.GetById(data.ApplicationId);

        List<GroupPermissionApplicationModel> matchingPermissions = [];
        if (groupModel == null)
        {
            result = InsertPermissionResult.Failure | InsertPermissionResult.GroupNotFound;
        }
        else
        {
            matchingPermissions = await _groupPermApplicationRepo.GetManyBy(new()
            {
                GroupId = data.GroupId,
                ApplicationId = data.ApplicationId,
                KindsIn = [data.Kind],
                Allow = false
            });
        }

        if (applicationModel == null)
        {
            if (result.HasFlag(InsertPermissionResult.Failure))
            {
                result |= InsertPermissionResult.ApplicationNotFound;
            }
            else
            {
                result = InsertPermissionResult.Failure | InsertPermissionResult.ApplicationNotFound;
            }
        }

        if (result.HasFlag(InsertPermissionResult.Success))
        {
            if (matchingPermissions.Count < 1)
            {
                throw new Exception($"{matchingPermissions} is empty, even though {nameof(AdminGroupWebService)}.{nameof(AdminGroupWebService.DenyApplicationPermission)} returned {result} (which included {nameof(AdminGroupWebService.InsertPermissionResult.Success)})");
            }
            var response = new AdminGroupV1DenyApplicationPermissionResponse()
            {
                Group = groupModel!, // will never be null since the success flag will be removed if it is.
                Application = applicationModel!, // will never be null since the success flag will be removed if it is.
                Permission = matchingPermissions.First()!,
                AlreadyExists = result.HasFlag(InsertPermissionResult.PermissionAlreadyExists),
                Duration = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start
            };
            Response.StatusCode = 200;
            return Json(response, BaseService.SerializerOptions);
        }
        else if (result.HasFlag(InsertPermissionResult.Failure))
        {
            if (result.HasFlag(InsertPermissionResult.GroupNotFound))
            {
                Response.StatusCode = 404;
                return Json(new NotFoundResponse()
                {
                    Message = $"Could not find Group with Id {data.GroupId}",
                    PropertyName = nameof(data.GroupId),
                    PropertyParentType = CockatooHelper.FormatTypeName(data.GetType())
                }, BaseService.SerializerOptions);
            }

            if (result.HasFlag(InsertPermissionResult.ApplicationNotFound))
            {
                Response.StatusCode = 404;
                return Json(new NotFoundResponse()
                {
                    Message = $"Could not find Application with Id {data.ApplicationId}",
                    PropertyName = nameof(data.ApplicationId),
                    PropertyParentType = CockatooHelper.FormatTypeName(data.GetType())
                }, BaseService.SerializerOptions);
            }
        }

        throw new NotImplementedException($"All possible values were handled for {nameof(InsertPermissionResult)}! (result: {result})");
    }

    /// <remarks>
    /// Returns 200 even if the permission wasn't there in the first place. Returns 404 when the group or application could not be found.
    /// </remarks>
    [HttpPost("PermissionRevoke/Application")]
    [ProducesResponseType(typeof(AdminGroupV1DetailResponse), 200, "application/json")]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 401, "application/json")]
    [ProducesResponseType(typeof(NotFoundResponse), 404, "application/json")]
    public async Task<ActionResult> RevokeApplicationPermission(
        [ModelBinder(typeof(JsonModelBinder))] [FromBody]
        AdminGroupV1RevokeApplicationPermissionRequest data)
    {
        var result = await _adminGroupWebService.RevokeApplicationPermission(data.GroupId, data.ApplicationId, data.Kind);

        var groupModel = await _groupRepo.GetById(data.GroupId);
        var applicationModel = await _applicationRepo.GetById(data.ApplicationId);
        if (groupModel == null)
        {
            result = RevokePermissionResult.Failure | RevokePermissionResult.GroupNotFound;
        }

        if (applicationModel == null)
        {
            if (result.HasFlag(RevokePermissionResult.Failure))
            {
                result |= RevokePermissionResult.ApplicationNotFound;
            }
            else
            {
                result = RevokePermissionResult.Failure | RevokePermissionResult.ApplicationNotFound;
            }
        }

        if (result.HasFlag(RevokePermissionResult.Success) || result.HasFlag(RevokePermissionResult.PermissionNotFound))
        {
            var details = await _adminGroupWebService.Details(data.GroupId);
            if (details == null)
            {
                throw new NoNullAllowedException($"{nameof(AdminGroupWebService)}.{nameof(AdminGroupWebService.Details)} returned null " +
                                                 $"when {nameof(result)} contains {nameof(RevokePermissionResult.Success)}!");
            }
            Response.StatusCode = 200;
            return Json(details, BaseService.SerializerOptions);
        }
        else
        {
            if (result.HasFlag(RevokePermissionResult.GroupNotFound))
            {
                Response.StatusCode = 404;
                return Json(new NotFoundResponse()
                {
                    Message = $"Could not find Group with Id {data.GroupId}",
                    PropertyName = nameof(data.GroupId),
                    PropertyParentType = CockatooHelper.FormatTypeName(data.GetType())
                }, BaseService.SerializerOptions);
            }

            if (result.HasFlag(InsertPermissionResult.ApplicationNotFound))
            {
                Response.StatusCode = 404;
                return Json(new NotFoundResponse()
                {
                    Message = $"Could not find Application with Id {data.ApplicationId}",
                    PropertyName = nameof(data.ApplicationId),
                    PropertyParentType = CockatooHelper.FormatTypeName(data.GetType())
                }, BaseService.SerializerOptions);
            }
        }

        throw new NotImplementedException($"All possible values were handled for {nameof(InsertPermissionResult)}! (result: {result})");
    }
    #endregion
}