using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.Services.WebApi.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using CheckScopedPermissionResult = Adastral.Cockatoo.Services.WebApi.ScopedPermissionWebService.CheckScopedPermissionResult;

namespace Adastral.Cockatoo.Services.WebApi;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class ScopedPermissionRequiredAttribute : ActionFilterAttribute
{
    public string ArgumentName { get; private set; }
    public ScopedPermissionKeyKind ArgumentKind { get; private set; }
    public ReadOnlyCollection<PermissionKind> RequiredPermission { get; private set; }
    public bool UseJsonResult { get; set; }
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.Controller.GetType().GetCustomAttribute<ApiControllerAttribute>() != null)
        {
            UseJsonResult = true;
        }
        if (!context.ActionArguments.TryGetValue(ArgumentName, out var actionArgument))
        {
            base.OnActionExecuting(context);
            return;
        }
        var service = context.HttpContext.RequestServices.GetRequiredService<ScopedPermissionWebService>();
        var result = service.CheckScopedPermission(ArgumentKind, actionArgument, RequiredPermission.ToList(), context.HttpContext).Result;
        if (result == CheckScopedPermissionResult.Continue)
        {
            base.OnActionExecuting(context);
        }
        else
        {
            WebApiHelper.HandleNotAuthorizedView(context, UseJsonResult, new()
            {
                ShowLoginButton = result == CheckScopedPermissionResult.LoginRequired
            });
        }
    }

    public ScopedPermissionRequiredAttribute(string argument, ScopedPermissionKeyKind keyKind, params PermissionKind[] permissionsRequired)
    {
        ArgumentName = argument;
        ArgumentKind = keyKind;
        RequiredPermission = new ReadOnlyCollection<PermissionKind>(permissionsRequired);
    }
}

public enum ScopedPermissionKeyKind
{
    ApplicationId
}