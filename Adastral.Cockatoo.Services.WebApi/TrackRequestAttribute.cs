using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Adastral.Cockatoo.Services.WebApi;

/// <summary>
/// Track requests with <see cref="SessionWebService.TrackRequest"/>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class TrackRequestAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        context.HttpContext.Request.EnableBuffering();
        var sessionWebService = context.HttpContext.RequestServices.GetRequiredService<SessionWebService>();
        sessionWebService.TrackRequest(context.HttpContext).Wait();
        base.OnActionExecuting(context);
    }
}