using Adastral.Cockatoo.DataAccess.Models;

namespace Adastral.Cockatoo.Services;

public partial class PermissionService
{
    #region Check Permission
    public async Task<bool> CheckGlobalPermission(
        string userId,
        PermissionFilterType filterType,
        params PermissionKind[] permissions)
    {
        var kinds = permissions.Distinct().ToArray();
        var data = await _permissionCacheService.GetUser(userId);
        if (data.Contains(PermissionKind.Superuser))
            return true;

        if (filterType == PermissionFilterType.Any)
        {
            foreach (var x in kinds)
            {
                if (data.Contains(x))
                    return true;
            }

            return false;
        }
        else if (filterType == PermissionFilterType.All)
        {
            var dict = new Dictionary<PermissionKind, bool>();
            foreach (var x in kinds)
            {
                dict[x] = data.Contains(x);
            }
            var count = dict.Count(v => v.Value);
            return count == dict.Keys.Count;
        }
        else
        {
            throw new NotImplementedException($"Where {nameof(filterType)} equals {filterType}");
        }
    }
    public Task<bool> CheckGlobalPermission(UserModel user, PermissionFilterType filterType, params PermissionKind[] permissions)
        => CheckGlobalPermission(user.Id, filterType, permissions);
    #endregion
    
    #region Grant
    public Task GrantManyGlobalForGroupAsync(GroupModel group, params PermissionKind[] kinds)
        => GrantManyGlobalForGroupAsync(group.Id, kinds);

    public Task GrantManyGlobalForGroupAsync(string groupId, params PermissionKind[] kinds)
    {
        return SetManyGlobalForGroup(groupId, true, kinds);
    }
    #endregion

    #region Deny
    public Task DenyManyGlobalForGroupAsync(GroupModel group, params PermissionKind[] kinds)
        => DenyManyGlobalForGroupAsync(group.Id, kinds);

    public Task DenyManyGlobalForGroupAsync(string groupId, params PermissionKind[] kinds)
    {
        return SetManyGlobalForGroup(groupId, false, kinds);
    }
    #endregion
    
    #region Revoke
    public Task RevokeManyGlobalForGroupAsync(GroupModel group, params PermissionKind[] kinds)
        => RevokeManyGlobalForGroupAsync(group.Id, kinds);
    public async Task RevokeManyGlobalForGroupAsync(string groupId, params PermissionKind[] kinds)
    {
        var data = await _groupPermissionGlobalRepo.GetManyByGroup(groupId);
        var ids = data.Where(v => kinds.Contains(v.Kind)).Select(v => v.Id).ToArray();
        await _groupPermissionGlobalRepo.Delete(ids);
        await _permissionCacheService.CalculateGroup(groupId);
    }
    #endregion

    public async Task SetManyGlobalForGroup(string groupId, bool allowValue, params PermissionKind[] kinds)
    {
        var data = await _groupPermissionGlobalRepo.GetManyByGroup(groupId) ?? [];
        var existsList = new List<PermissionKind>();
        var modelsToPush = new List<GroupPermissionGlobalModel>();
        var modelsToDelete = new List<string>();

        foreach (var item in data)
        {
            if (kinds.Contains(item.Kind))
            {
                if (existsList.Contains(item.Kind))
                {
                    modelsToDelete.Add(item.Id);
                }
                else
                {
                    item.Allow = allowValue;
                    existsList.Add(item.Kind);
                    modelsToPush.Add(item);
                }
            }
        }

        foreach (var item in kinds.Where(v => modelsToPush.Any(x => x.Kind == v) == false))
        {
            modelsToPush.Add(new()
            {
                Kind = item,
                GroupId = groupId,
                Allow = allowValue
            });
        }
        if (modelsToDelete.Count > 0)
        {
            await _groupPermissionGlobalRepo.Delete(modelsToDelete.ToArray());
        }
        foreach (var x in modelsToPush)
        {
            await _groupPermissionGlobalRepo.InsertOrUpdate(x);
        }
        await _permissionCacheService.CalculateGroup(groupId);
    }
}