using System.Threading.Tasks;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Adastral.Cockatoo.Services.WebApi;

[CockatooDependency]
public class PermissionWebService : BaseService
{
    private readonly AuthWebService _authWebService;
    private readonly PermissionService _permissionService;
    public PermissionWebService(IServiceProvider services)
        : base(services)
    {
        _authWebService = services.GetRequiredService<AuthWebService>();
        _permissionService = services.GetRequiredService<PermissionService>();
    }

    public async Task<bool> CurrentHasAll(HttpContext context, params PermissionKind[] permissions)
    {
        var user = await _authWebService.GetCurrentUser(context);
        if (user == null)
            return false;
        return await _permissionService.HasAllPermissionsAsync(user, permissions);
    }

    public async Task<bool> CurrentHasAny(HttpContext context, params PermissionKind[] permissions)
    {
        var user = await _authWebService.GetCurrentUser(context);
        if (user == null)
            return false;
        return await _permissionService.HasAnyPermissionsAsync(user, permissions);
    }
}