using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.DataAccess.Repositories.Group;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NLog;

namespace Adastral.Cockatoo.Services;

[CockatooDependency]
public partial class PermissionService : BaseService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly PermissionCacheService _permissionCacheService;
    private readonly GroupPermissionGlobalRepository _groupPermissionGlobalRepo;
    private readonly GroupPermissionApplicationRepository _groupPermissionAppRepo;
    private readonly GroupUserAssociationRepository _groupUserAssocRepo;
    private readonly GroupRepository _groupRepo;
    private readonly UserRepository _userRepo;
    private readonly MongoClient _mongoClient;

    public PermissionService(IServiceProvider services)
        : base(services)
    {
        _permissionCacheService = services.GetRequiredService<PermissionCacheService>();
        _groupPermissionGlobalRepo = services.GetRequiredService<GroupPermissionGlobalRepository>();
        _groupPermissionAppRepo = services.GetRequiredService<GroupPermissionApplicationRepository>();
        _groupUserAssocRepo = services.GetRequiredService<GroupUserAssociationRepository>();
        _groupRepo = services.GetRequiredService<GroupRepository>();
        _userRepo = services.GetRequiredService<UserRepository>();
        _mongoClient = services.GetRequiredService<MongoClient>();
    }

    [Obsolete($"Use {nameof(CheckGlobalPermission)} instead")]
    public Task<bool> HasAnyPermissionsAsync(UserModel user, params PermissionKind[] kinds)
        => HasAnyPermissionsAsync(user.Id, kinds);
    [Obsolete($"Use {nameof(CheckGlobalPermission)} instead")]
    public Task<bool> HasAnyPermissionsAsync(string userId, params PermissionKind[] permissions)
    {
        return CheckGlobalPermission(userId, PermissionFilterType.Any, permissions);
    }
    [Obsolete($"Use {nameof(CheckGlobalPermission)} instead")]
    public Task<bool> HasAllPermissionsAsync(UserModel user, params PermissionKind[] kinds)
        => HasAllPermissionsAsync(user.Id, kinds);
    [Obsolete($"Use {nameof(CheckGlobalPermission)} instead")]
    public Task<bool> HasAllPermissionsAsync(string userId, params PermissionKind[] permissions)
    {
        return CheckGlobalPermission(userId, PermissionFilterType.All, permissions);
    }

    public enum PermissionFilterType
    {
        Any = 0,
        All = 1
    }
}