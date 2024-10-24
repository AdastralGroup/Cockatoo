using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.Common.Helpers;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.Services.WebApi.Helpers;
using Adastral.Cockatoo.Services.WebApi.Models.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Sentry.Protocol;

namespace Adastral.Cockatoo.Services.WebApi;

[CockatooDependency]
public class ScopedPermissionWebService : BaseService
{
    private readonly AuthWebService _authWebService;
    private readonly ApplicationDetailRepository _applicationDetailRepository;
    private readonly PermissionService _permissionService;
    private readonly PermissionCacheService _permissionCacheService;
    public ScopedPermissionWebService(IServiceProvider services)
        : base(services)
    {
        _authWebService = services.GetRequiredService<AuthWebService>();
        _applicationDetailRepository = services.GetRequiredService<ApplicationDetailRepository>();
        _permissionService = services.GetRequiredService<PermissionService>();
        _permissionCacheService = services.GetRequiredService<PermissionCacheService>();
    }

    public class HandleManualCheckResult
    {
        public required int StatusCode { get; init; }
        public required ActionResult ActionResult { get; init; }
    }

    public async Task<HandleManualCheckResult?> HandleManualCheck(HttpContext httpContext, string applicationId, params PermissionKind[] kinds)
    {
        var user = await _authWebService.GetCurrentUser(httpContext);
        if (user == null)
        {
            throw new InvalidOperationException(
                $"User authentication failed, but this action has {nameof(AuthRequiredAttribute)}");
        }
        var permissionCheckResult = await CheckApplicationScopedPermission(
            applicationId, user, [..kinds]);
        if (permissionCheckResult != CheckScopedPermissionResult.Continue)
        {
            if (permissionCheckResult == CheckScopedPermissionResult.NotFound)
            {
                return new HandleManualCheckResult()
                {
                    StatusCode = 404,
                    ActionResult = new JsonResult(new NotFoundResponse()
                    {
                        Message = $"Could not find {nameof(ApplicationDetailModel)} with Id {applicationId}",
                        PropertyName = nameof(applicationId),
                    }, SerializerOptions)
                };
            }
            return new HandleManualCheckResult()
            {
                StatusCode = 403,
                ActionResult = WebApiHelper.HandleNotAuthorizedView(httpContext, true, new()
                {
                    ShowLoginButton = permissionCheckResult == CheckScopedPermissionResult.LoginRequired
                })
            };
        }

        return null;
    }

    public bool TryHandleManualCheck(
        HttpContext httpContext,
        string applicationId,
        PermissionKind kind,
        out HandleManualCheckResult? result)
    {
        result = HandleManualCheck(httpContext, applicationId, kind).Result;
        return result != null;
    }
    
    public enum CheckScopedPermissionResult
    {
        NotAuthorized,
        NotFound,
        LoginRequired,
        Continue,
    }
    public async Task<CheckScopedPermissionResult> CheckScopedPermission(
        ScopedPermissionKeyKind kind,
        object? kindValue,
        List<PermissionKind> permissionRequired,
        HttpContext context)
    {
        var user = await _authWebService.GetCurrentUser(context);
        if (user == null)
        {
            return CheckScopedPermissionResult.LoginRequired;
        }

        // Allow when user has any of those global permissions and/or they're a superuser
        bool check = await _permissionService.HasAnyPermissionsAsync(
            user, [..permissionRequired, PermissionKind.Superuser]);
        if (check)
        {
            return CheckScopedPermissionResult.Continue;
        }

        switch (kind)
        {
            case ScopedPermissionKeyKind.ApplicationId:
                return await CheckApplicationScopedPermission(kindValue?.ToString() ?? "", user, permissionRequired);
        }
        throw new NotImplementedException($"Kind {kind} has not been implemented!");
    }

    public async Task<CheckScopedPermissionResult> CheckApplicationScopedPermission(
        string applicationId,
        UserModel user,
        List<PermissionKind> permissionsRequired)
    {
        var applicationModel = await _applicationDetailRepository.GetById(applicationId);
        if (applicationModel == null)
        {
            return CheckScopedPermissionResult.NotFound;
        }

        var result = await _permissionService.CheckApplicationPermission(
            user,
            applicationModel,
            permissionsRequired.ToArray());

        return result
            ? CheckScopedPermissionResult.Continue
            : CheckScopedPermissionResult.NotAuthorized;
    }
}