using System.Reflection;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.Services.WebApi.Helpers;
using Adastral.Cockatoo.Services.WebApi.Models.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Adastral.Cockatoo.Services.WebApi;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class PermissionRequiredAttribute : ActionFilterAttribute
{
    /// <summary>
    /// <para>Return <see cref="NotAuthorizedResponse"/> as JSON 403 result when <see langword="true"/>, and auth fails.</para>
    ///
    /// <para>This will be overridden and ignored when the controller that this is associated with has <see cref="Microsoft.AspNetCore.Mvc.ApiControllerAttribute"/> on it.</para>
    /// </summary>
    public bool UseJsonResult { get; set; } = false;
    /// <summary>
    /// What permission kinds is required. It will allow the user if any of the Permissions are found.
    /// </summary>
    public PermissionKind[] Kinds { get; private set; }
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.Controller.GetType().GetCustomAttribute<ApiControllerAttribute>() != null)
        {
            UseJsonResult = true;
        }
        var authWebService = context.HttpContext.RequestServices.GetRequiredService<AuthWebService>();
        var permissionService = context.HttpContext.RequestServices.GetRequiredService<PermissionService>();
        var user = authWebService.GetCurrentUser(context.HttpContext).Result;
        if (user == null)
        {
            WebApiHelper.HandleNotAuthorizedView(context, UseJsonResult, new()
            {
                ShowLoginButton = true
            });
            return;
        }

        PermissionKind[] permissions = Kinds.Concat([PermissionKind.Superuser]).Distinct().ToArray();
        var has = permissionService.HasAnyPermissionsAsync(user, permissions).Result;
        if (has == false)
        {
            WebApiHelper.HandleNotAuthorizedView(
                context,
                UseJsonResult,
                new()
                {
                    Message = "Missing Permissions",
                    MissingPermissions = Kinds.ToList(),
                    ShowLoginButton = false
                });
            return;
        }

        base.OnActionExecuting(context);
    }

    public PermissionRequiredAttribute(params PermissionKind[] kinds)
        : base()
    {
        Kinds = kinds.Distinct().ToArray();
    }
}