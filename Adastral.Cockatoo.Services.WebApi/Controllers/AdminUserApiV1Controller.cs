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
[Route("~/api/v1/Admin/User")]
[AuthRequired]
[TrackRequest]
public class AdminUserApiV1Controller : Controller
{
    private readonly ServiceAccountTokenRepository _serviceAccountTokenRepo;
    private readonly UserRepository _userRepo;
    private readonly ServiceAccountRepository _serviceAccountRepo;
    private readonly AuthWebService _authWebService;
    private readonly PermissionService _permissionService;
    private readonly UserService _userService;
    public AdminUserApiV1Controller(IServiceProvider services)
        : base()
    {
        _serviceAccountTokenRepo = CoreContext.Instance!.GetRequiredService<ServiceAccountTokenRepository>();
        _userRepo = CoreContext.Instance!.GetRequiredService<UserRepository>();
        _serviceAccountRepo = CoreContext.Instance!.GetRequiredService<ServiceAccountRepository>();
        _authWebService = services.GetRequiredService<AuthWebService>();
        _permissionService = services.GetRequiredService<PermissionService>();
        _userService = services.GetRequiredService<UserService>();
    }
    
    [HttpGet("List")]
    [ProducesResponseType(typeof(IEnumerable<UserModel>), 200, "application/json")]
    [ProducesResponseType(typeof(NotAuthorizedResponse), 403, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    [PermissionRequired(PermissionKind.UserAdminViewAll)]
    public async Task<ActionResult> List()
    {
        try
        {
            var users = await _userRepo.GetAll();
            Response.StatusCode = 200;
            return Json(users, BaseService.SerializerOptions);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    [HttpDelete("Token/{tokenId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ExceptionWebResponse), 401, "application/json")]
    [ProducesResponseType(typeof(NotFoundWebResponse), 404, "application/json")]
    [ProducesResponseType(typeof(ExceptionWebResponse), 500, "application/json")]
    public async Task<ActionResult> DeleteToken(string tokenId)
    {
        try
        {
            var tokenModel = await _serviceAccountTokenRepo.GetById(tokenId);
            if (tokenModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(ServiceAccountTokenModel),
                    nameof(ServiceAccountTokenModel.Id),
                    tokenId,
                    $"Could not find model in {nameof(ServiceAccountTokenRepository)} with Id {tokenId}"), BaseService.SerializerOptions);
            }

            var tokenUserModel = await _userRepo.GetById(tokenModel.ServiceAccountId);
            if (tokenUserModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(UserModel),
                    nameof(UserModel.Id),
                    tokenModel.ServiceAccountId,
                    $"Could not find model in {nameof(UserRepository)} with Id {tokenModel.ServiceAccountId}"), BaseService.SerializerOptions);
            }
            var tokenSAModel = await _serviceAccountRepo.GetById(tokenModel.ServiceAccountId);
            if (tokenSAModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(ServiceAccountModel),
                    nameof(ServiceAccountModel.UserId),
                    tokenModel.ServiceAccountId,
                    $"Could not find model in {nameof(ServiceAccountRepository)} with UserId {tokenModel.ServiceAccountId}"), BaseService.SerializerOptions);
            }
            var tokenUserOwnerModel = await _userRepo.GetById(tokenSAModel.OwnerUserId);
            if (tokenUserOwnerModel == null)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundWebResponse(
                    typeof(UserModel),
                    nameof(UserModel.Id),
                    tokenSAModel.OwnerUserId,
                    $"Could not find model in {nameof(UserRepository)} with Id {tokenSAModel.OwnerUserId}"), BaseService.SerializerOptions);
            }

            var requestingUserModel = await _authWebService.GetCurrentUser(HttpContext);
            if (requestingUserModel == null)
                throw new NoNullAllowedException($"{nameof(AuthWebService)}.{nameof(AuthWebService.GetCurrentUser)} returned null");

            if (requestingUserModel.Id != tokenUserOwnerModel.Id)
            {
                if (await _permissionService.HasAnyPermissionsAsync(requestingUserModel, PermissionKind.PermissionAdmin) == false)
                {
                    Response.StatusCode = 401;
                    return Json(
                        new ExceptionWebResponse(
                            new UnauthorizedAccessException(
                                $"You do not own the Service Account that you are requesting access for")));
                }
            }

            Response.StatusCode = 200;
            await _serviceAccountTokenRepo.Delete(tokenModel.Id);
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            Response.StatusCode = 500;
            return Json(new ExceptionWebResponse(ex), BaseService.SerializerOptions);
        }
    }

    /// <summary>
    /// Create a Token for a Service Account
    /// </summary>
    /// <remarks>
    /// Will return 401 when you don't own the Service Account, which can be overridden with <see cref="PermissionKind.ServiceAccountAdmin"/>
    /// </remarks>
    [HttpPost("Token")]
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