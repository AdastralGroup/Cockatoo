using System.Buffers;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.DataAccess.Repositories.Group;
using kate.shared.Helpers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using NLog;

namespace Adastral.Cockatoo.Services;

[CockatooDependency]
public class PermissionCacheService : BaseService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly UserRepository _userRepo;
    private readonly GroupRepository _groupRepo;
    private readonly GroupPermissionGlobalRepository _groupPermGlobalRepo;
    private readonly GroupPermissionApplicationRepository _groupPermAppRepo;
    private readonly GroupUserAssociationRepository _groupUserAssocRepo;

    private readonly ApplicationDetailRepository _appRepo;
    private readonly UserPermissionGlobalCacheRepository _userPermGlobalCacheRepo;
    private readonly UserPermissionApplicationCacheRepository _userPermAppCacheRepo;
    private readonly IDistributedCache _distCache;

    public PermissionCacheService(IServiceProvider services)
        : base(services)
    {
        _userRepo = services.GetRequiredService<UserRepository>();

        _groupRepo = services.GetRequiredService<GroupRepository>();
        _groupPermGlobalRepo = services.GetRequiredService<GroupPermissionGlobalRepository>();
        _groupPermAppRepo = services.GetRequiredService<GroupPermissionApplicationRepository>();
        _groupUserAssocRepo = services.GetRequiredService<GroupUserAssociationRepository>();

        _userPermGlobalCacheRepo = services.GetRequiredService<UserPermissionGlobalCacheRepository>();
        _userPermAppCacheRepo = services.GetRequiredService<UserPermissionApplicationCacheRepository>();
        _appRepo = services.GetRequiredService<ApplicationDetailRepository>();
        _distCache = services.GetRequiredService<IDistributedCache>();
    }

    public static List<PermissionKind> GetInheritedPermissions(PermissionKind kind)
    {
        return GetInheritedPermissionsInternal(kind, 0, []);
    }
    private static List<PermissionKind> GetInheritedPermissionsInternal(PermissionKind kind,
        uint depth,
        List<PermissionKind> permissionStack)
    {
        permissionStack.Add(kind);
        if (depth > 64)
        {
            throw new StackOverflowException($"Circular Reference when trying to get inherited permissions for {kind}.\npermissionStack: "
                                             + string.Join('.', permissionStack.Select(v => $"{v}")));
        }
        var result = new List<PermissionKind>();

        var enumType = typeof(PermissionKind);
        var memberInfos = enumType.GetMember(kind.ToString());
        var enumValueMemberInfo = memberInfos.FirstOrDefault(v => v.DeclaringType == enumType);
        var valueAttributes = enumValueMemberInfo?.GetCustomAttributes<PermissionInheritAttribute>(false);
        foreach (var attr in valueAttributes ?? [])
        {
            result.Add(attr.InheritFrom);
        }

        if (result.Count > 0)
        {
            uint di = depth + 1;
            result.AddRange(result.SelectMany(v => GetInheritedPermissionsInternal(v, di, [..permissionStack])).ToList());
        }

        return result.Distinct().ToList();
    }

    /// <summary>
    /// Get user global permissions
    /// </summary>
    /// <param name="userId">Id of <see cref="UserModel"/></param>
    /// <returns>List of permissions the user has.</returns>
    public async Task<List<PermissionKind>> GetUser(string userId)
    {
        var stringContent = await _distCache.GetStringAsync(GetGlobalUserKey(userId));
        if (string.IsNullOrEmpty(stringContent))
        {
            var res = await CalculateUser(userId);
            return res.GlobalCache.Permissions;
        }
        else
        {
            return JsonSerializer.Deserialize<List<PermissionKind>>(stringContent!, SerializerOptions) ?? [];
        }
    }

    /// <summary>
    /// Get all permissions for a specific application.
    /// </summary>
    /// <param name="userId">Id of <see cref="UserModel"/></param>
    /// <param name="applicationId">Id of <see cref="ApplicationDetailModel"/></param>
    /// <returns>List of application permissions this user has for the specified application.</returns>
    public async Task<List<ScopedApplicationPermissionKind>> GetUserByApplication(string userId, string applicationId)
    {
        var userPermissions = await GetUser(userId);

        var stringContent = await _distCache.GetStringAsync(GetApplicationUserKey(userId, applicationId));
        if (string.IsNullOrEmpty(stringContent))
        {
            var res = await CalculateUser(userId);
            if (res.ApplicationCache.TryGetValue(applicationId, out var item))
            {
                if (userPermissions.Contains(PermissionKind.Superuser))
                {
                    return item.Permissions.Concat([ScopedApplicationPermissionKind.Admin]).Distinct().ToList();
                }
                return item.Permissions;
            }
            else
            {
                if (userPermissions.Contains(PermissionKind.Superuser))
                {
                    return [ScopedApplicationPermissionKind.Admin];
                }
                return [];
            }
        }
        else
        {
            var data = JsonSerializer.Deserialize<List<ScopedApplicationPermissionKind>>(stringContent!, SerializerOptions) ?? [];
            if (userPermissions.Contains(PermissionKind.Superuser))
            {
                return data.Concat([ScopedApplicationPermissionKind.Admin]).Distinct().ToList();
            }
            return data;
        }
    }

    /// <summary>
    /// Result for <see cref="CalculateUser"/>
    /// </summary>
    public class RecalculateUserResult
    {
        public required UserPermissionGlobalCacheModel GlobalCache { get; set; }
        public Dictionary<string, UserPermissionApplicationCacheModel> ApplicationCache { get; set; } = [];
    }

    /// <summary>
    /// Calculate permissions for the <paramref name="userId"/> provided.
    /// </summary>
    /// <param name="userId">Id of <see cref="UserModel"/></param>
    public async Task<RecalculateUserResult> CalculateUser(string userId)
    {
        var groupAssociations = await _groupUserAssocRepo.GetAllForUser(userId);
        var groups = await _groupRepo.GetManyById(groupAssociations.Select(v => v.GroupId).ToArray());
        var globalPermissions = new Dictionary<PermissionKind, bool>();
        var applicationPermissions = new Dictionary<string, Dictionary<ScopedApplicationPermissionKind, bool>>();
        foreach (var app in await _appRepo.GetAll())
        {
            applicationPermissions[app.Id] = [];
        }
        foreach (var group in groups.OrderByDescending(v => v.Priority))
        {
            var groupGlobalPermissions = await _groupPermGlobalRepo.GetManyByGroup(group.Id);
            foreach (var item in groupGlobalPermissions)
            {
                foreach (var k in GetInheritedPermissions(item.Kind))
                {
                    globalPermissions[k] = item.Allow;
                }
                globalPermissions[item.Kind] = item.Allow;
            }

            var groupApplicationPermissions = await _groupPermAppRepo.GetManyByGroup(group.Id);
            // do stuff that has an ApplicationId first, then override stuff when it's not set.
            foreach (var item in groupApplicationPermissions.OrderBy(v => string.IsNullOrEmpty(v.ApplicationId) ? 1 : 0))
            {
                if (string.IsNullOrEmpty(item.ApplicationId))
                {
                    foreach (var i in applicationPermissions)
                    {
                        applicationPermissions[i.Key][item.Kind] = item.Allow;
                    }
                }
                else
                {
                    if (!applicationPermissions.ContainsKey(item.ApplicationId))
                    {
                        applicationPermissions[item.ApplicationId] = [];
                    }

                    applicationPermissions[item.ApplicationId][item.Kind] = item.Allow;
                }
            }
        }

        var globalCacheModel = new UserPermissionGlobalCacheModel()
        {
            UserId = userId,
            Permissions = globalPermissions.Where(v => v.Value == true).Select(v => v.Key).ToList()
        };
        await _userPermGlobalCacheRepo.InsertOrUpdate(globalCacheModel);
        await _distCache.SetStringAsync(GetGlobalUserKey(userId), JsonSerializer.Serialize(globalCacheModel.Permissions, SerializerOptions));
        var result = new RecalculateUserResult()
        {
            GlobalCache = globalCacheModel
        };
        foreach (var (appId, data) in applicationPermissions)
        {
            var appCacheModel = new UserPermissionApplicationCacheModel()
            {
                UserId = userId,
                ApplicationId = appId,
                Permissions = data.Where(v => v.Value == true).Select(v => v.Key).ToList()
            };
            await _userPermAppCacheRepo.InsertOrUpdate(appCacheModel);
            await _distCache.SetStringAsync(GetApplicationUserKey(userId, appId), JsonSerializer.Serialize(appCacheModel.Permissions, SerializerOptions));
            result.ApplicationCache[appId] = appCacheModel;
        }
        return result;
    }
    /// <summary>
    /// Calculate permissions for the <paramref name="groupId"/> provided.
    /// </summary>
    /// <param name="groupId">Id of <see cref="GroupModel"/></param>
    public async Task CalculateGroup(string groupId)
    {
        var associations = await _groupUserAssocRepo.GetAllForGroup(groupId);
        foreach (var assoc in associations)
        {
            try
            {
                var userModel = await _userRepo.GetById(assoc.UserId);
                if (userModel != null)
                {
                    await CalculateUser(assoc.UserId);
                }
                else
                {
                    try
                    {
                        await _groupUserAssocRepo.Delete(assoc.Id);
                    }
                    catch (Exception ex)
                    {
                        _log.Warn($"Failed to delete unreferenced Group->User association (since the user doesn't exist anymore)\n{ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new AggregateException($"Failed to process user {assoc.UserId} for group {groupId}", ex);
            }
        }
    }

    private string GetGlobalUserKey(string userId)
    {
        return $"{nameof(PermissionCacheService)},global,{nameof(userId)}={userId}";
    }

    private string GetApplicationUserKey(string userId, string appId)
    {
        return $"{nameof(PermissionCacheService)},application,{nameof(userId)}={userId},{nameof(appId)}={appId}";
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _log.Debug($"Calculating permissons for all users.");
        foreach (var user in await _userRepo.GetAll())
        {
            try
            {
                await CalculateUser(user.Id);
            }
            catch (Exception ex)
            {
                _log.Error($"Could not calculate permissions for user {user.Id}\n{ex}");
            }
        }
    }
}