using System.ComponentModel;
using System.Reflection;
using Adastral.Cockatoo.Services.WebApi.Helpers;
using Adastral.Cockatoo.Services.WebApi.Models.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Adastral.Cockatoo.Services.WebApi;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class AuthRequiredAttribute : ActionFilterAttribute
{
    /// <summary>
    /// <para>Return <see cref="NotAuthorizedResponse"/> as JSON 403 result when <see langword="true"/>, and auth fails.</para>
    ///
    /// <para>This will be overridden and ignored when the controller that this is associated with has <see cref="ApiControllerAttribute"/> on it.</para>
    /// </summary>
    [DefaultValue(false)]
    public bool UseJsonResult { get; set; } = false;
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        context.HttpContext.Request.EnableBuffering();
        if (context.Controller.GetType().GetCustomAttribute<ApiControllerAttribute>() != null)
        {
            UseJsonResult = true;
        }
        var authWebService = context.HttpContext.RequestServices.GetRequiredService<AuthWebService>();
        if (authWebService.IsAuthenticated(context.HttpContext).Result == false)
        {
            WebApiHelper.HandleNotAuthorizedView(context, UseJsonResult, new()
            {
                ShowLoginButton = true
            });
            return;
        }
        base.OnActionExecuting(context);
    }
}