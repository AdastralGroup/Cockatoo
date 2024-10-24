using System.Reflection;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.Services.WebApi.Models;
using Adastral.Cockatoo.Services.WebApi.Models.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Adastral.Cockatoo.Services.WebApi.Helpers;

public static class WebApiHelper
{
    internal static void HandleNotAuthorizedView(ActionExecutingContext context, bool useJsonResult, NotAuthorizedViewModel viewModel)
    {
        context.Result = HandleNotAuthorizedView(context.HttpContext, useJsonResult, viewModel);
    }

    public static ActionResult HandleNotAuthorizedView(HttpContext context,
        bool useJsonResult,
        NotAuthorizedViewModel viewModel)
    {
        context.Response.StatusCode = 403;
        if (useJsonResult || context.GetType().GetCustomAttribute<ApiControllerAttribute>(true) != null)
        {
            return new JsonResult(new NotAuthorizedResponse()
            {
                Message = string.IsNullOrEmpty(viewModel.Message) ? null : viewModel.Message,
                MissingPermissions = viewModel.MissingPermissions,
            }, BaseService.SerializerOptions);
        }
        else
        {
            return new ViewResult
            {
                ViewName = "NotAuthorized",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = viewModel
                }
            };
        }
    }
}