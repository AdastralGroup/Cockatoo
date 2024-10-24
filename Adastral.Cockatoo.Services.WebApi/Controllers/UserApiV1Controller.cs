using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.Common.Helpers;
using Adastral.Cockatoo.DataAccess;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.Services.WebApi.Models.Request;
using Adastral.Cockatoo.Services.WebApi.Models.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Adastral.Cockatoo.Services.WebApi.Controllers;

[ApiController]
[AuthRequired]
[Route("~/api/v1/User")]
[TrackRequest]
public class UserApiV1Controller : Controller
{
    private readonly UserRepository _userRepo;
    private readonly UserService _userService;
    private readonly AuthWebService _authWebService;
    public UserApiV1Controller(IServiceProvider services)
        : base()
    {
        _userRepo = services.GetRequiredService<UserRepository>();
        _userService = services.GetRequiredService<UserService>();
        _authWebService = services.GetRequiredService<AuthWebService>();
    }

    [HttpGet("@me")]
    [ProducesResponseType(typeof(UserModel), 200, "application/json")]
    public async Task<ActionResult> GetSelf()
    {
        var aspUserModel = await _authWebService.GetCurrentUser(HttpContext);
        if (aspUserModel == null)
        {
            throw new NoNullAllowedException($"Failed to get UserModel, but user is authenticated!");
        }
        var userModel = await _userRepo.GetById(aspUserModel.Id);
        return Json(userModel, BaseService.SerializerOptions);
    }

    [PermissionRequired(PermissionKind.UserAdminViewAll)]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserModel), 200, "application/json")]
    [ProducesResponseType(typeof(NotFoundWebResponse), 404, "application/json")]
    public async Task<ActionResult> GetById(string id)
    {
        var userModel = await _userRepo.GetById(id);
        if (userModel == null)
        {
            Response.StatusCode = 404;
            return Json(new NotFoundWebResponse(
                typeof(UserModel),
                nameof(UserModel.Id),
                id,
                $"Could not find model in {nameof(UserRepository)} with Id {id}"), BaseService.SerializerOptions);
        }
        return Json(userModel, BaseService.SerializerOptions);
    }

    /// <summary>
    /// Create a Token for a Service Account
    /// </summary>
    /// <remarks>
    /// Will return 401 when you don't own the Service Account, which can be overridden with <see cref="PermissionKind.ServiceAccountAdmin"/>
    /// </remarks>
    [HttpPost("CreateToken")]
    [ProducesResponseType(typeof(ServiceAccountTokenModel), 200, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 400, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 401, "application/json")]
    [ProducesResponseType(typeof(NotFoundWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [AuthRequired]
    public async Task<ActionResult> CreateToken([ModelBinder(typeof(JsonModelBinder))] [FromBody] UserV1CReateTokenRequest data)
    {
        try
        {
            var requestingUser = await _authWebService.GetCurrentUser(HttpContext);
            if (requestingUser == null)
            {
                throw new NoNullAllowedException($"{nameof(AuthWebService)}.{nameof(AuthWebService.GetCurrentUser)} returned null");
            }
            var targetUser = await _userRepo.GetById(data.UserId);
            if (targetUser == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundResponse()
                {
                    Message = $"Could not find {nameof(UserModel)} with Id {data.UserId}",
                    PropertyName = nameof(data.UserId),
                    PropertyParentType = CockatooHelper.FormatTypeName(data.GetType())
                }, BaseService.SerializerOptions);
            }
            var checkResult = await _userService.CanCreateTokenFor(requestingUser, targetUser);
            if (checkResult != CanUserCreateTokenKind.Yes)
            {
                switch (checkResult)
                {
                    case CanUserCreateTokenKind.RequestingUserIsNotOwner:
                        Response.StatusCode = 401;
                        return Json(new ExceptionWebResponse(new Exception($"You do not own the Service Account you are trying to access.")), BaseService.SerializerOptions);
                    case CanUserCreateTokenKind.RequestingUserIsServiceAccount:
                        Response.StatusCode = 400;
                        return Json(new ExceptionWebResponse(new Exception($"Service Accounts cannot create tokens.")), BaseService.SerializerOptions);
                    case CanUserCreateTokenKind.TargetUserIsNotServiceAccount:
                        Response.StatusCode = 400;
                        return Json(new ExceptionWebResponse(new Exception($"The User you are trying to create a token for, is not a Service Account")), BaseService.SerializerOptions);
                    default:
                        throw new NotImplementedException($"{nameof(checkResult)}={checkResult}");
                }
            }

            if (data.ExpiresAt != null)
            {
                var parsed = DateTimeOffset.FromUnixTimeSeconds((long)data.ExpiresAt);
                if (parsed < DateTimeOffset.UtcNow)
                {
                    Response.StatusCode = 401;
                    return Json(new ExceptionWebResponse(new Exception($"Expiry cannot be in the past.")), BaseService.SerializerOptions);
                }
            }

            var result = await _userService.CreateToken(targetUser, data.ExpiresAt);
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