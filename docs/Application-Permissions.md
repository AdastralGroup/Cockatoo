# Scoped Permissions (for Applications)

If you wish to only check if a user has a permission for a specific application, you can do so on a controller action with the [`ScopedPermissionRequiredAttribute`](../Adastral.Cockatoo.Services.WebApi/ScopedPermissionRequiredAttribute.cs) attribute.

The only downside of the [`ScopedPermissionRequiredAttribute`](../Adastral.Cockatoo.Services.WebApi/ScopedPermissionRequiredAttribute.cs) attribute is that it requires the Application Id to be in the action arguments.

In the following example, it will only allow access to the `ProcessLogic` action if the requesting user is logged in, and they have the `BullseyeRegisterPatch` for the Application Id which is defined in the `appId` action argument.
```csharp
// ..
[ApiController]
public class ExampleApiV1Controller : BaseController
{
// ...
    [ScopedPermissionRequired("appId", ScopedPermissionKeyKind.ApplicationId, PermissionKind.BullseyeRegisterPatch)]
    public async Task<ActionResult> ProcessLogic(string appId)
    {
        // etc...
    }
}
```

If you are fetching the Application Id from inside of the action, then you can use the following example;
```csharp
[ApiController]
public class ExampleApiV1Controller(IServiceProvider services) : BaseController()
{
    private readonly BullseyeRevisionRepository _bullseyeRevisionRepo = services.GetRequiredService<BullseyeRevisionRepository>();
    private readonly ScopedPermissionWebService _scopedPermissionWebService = services.GetRequiredService<ScopedPermissionWebService>();

    [AuthRequired]
    public async Task<ActionResult> ProcessLogic(
        [ModelBinder(typeof(JsonModelBinder))]
        [FromBody]
        ManageBullseyeV1RegisterPatchRequest data)
    {
        var fromRevision = await _bullseyeRevisionRepo.GetById(data.FromRevisionId);
        // logic to check if fromRevision exists

        var user = await _authWebService.GetCurrentUser(HttpContext);
        if (user == null)
        {
            throw new InvalidOperationException(
                $"User authentication failed, but this action has {nameof(AuthRequiredAttribute)}");
        }

        //---------------- USAGE EXAMPLE
        // check if the requesting user has the BullseyeRegisterPatch permission, for the App Id that
        // is associated with fromRevision.
        var permissionCheckResult = await _scopedPermissionWebService.CheckApplicationScopedPermission(
            toRevision.BullseyeAppId,
            user,
            [PermissionKind.BullseyeRegisterPatch]);
        // return error response when not authorized.
        if (permissionCheckResult != CheckScopedPermissionResult.Continue)
        {
            if (permissionCheckResult == CheckScopedPermissionResult.NotFound)
            {
                Response.StatusCode = 404;
                return Json(new NotFoundResponse()
                {
                    Message = $"Could not find {nameof(BullseyeAppModel)} with Id {toRevision.BullseyeAppId}",
                    PropertyName = nameof(toRevision.BullseyeAppId),
                    PropertyParentType = CockatooHelper.FormatTypeName(toRevision.GetType())
                });
            }
            Response.StatusCode = 403;
            return WebApiHelper.HandleNotAuthorizedView(HttpContext, true, new()
            {
                ShowLoginButton = permissionCheckResult == CheckScopedPermissionResult.LoginRequired
            });
        }

        // ...
    }
}
```