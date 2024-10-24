using Adastral.Cockatoo.DataAccess.Models;

namespace Adastral.Cockatoo.Services;

public partial class PermissionService
{
    public static List<ScopedApplicationPermissionKind> MapPermission(List<PermissionKind> permissions)
    {
        var mapped = new List<ScopedApplicationPermissionKind>();
        var mappingTable = new Dictionary<PermissionKind, ScopedApplicationPermissionKind>()
        {
            { PermissionKind.ApplicationDetailAdmin, ScopedApplicationPermissionKind.Admin },
            { PermissionKind.ApplicationDetailViewAll, ScopedApplicationPermissionKind.ReadOnly },
            { PermissionKind.ApplicationDetailEditAppearance, ScopedApplicationPermissionKind.EditAppearance },
            { PermissionKind.ApplicationDetailEditDetails, ScopedApplicationPermissionKind.EditDetails },

            { PermissionKind.ApplicationDetailAUDNAdmin, ScopedApplicationPermissionKind.Admin },
            { PermissionKind.ApplicationDetailAUDNView, ScopedApplicationPermissionKind.ReadOnly },
            { PermissionKind.ApplicationDetailAUDNDeleteRevision, ScopedApplicationPermissionKind.ManageRevisions },
            { PermissionKind.ApplicationDetailAUDNSubmitRevision, ScopedApplicationPermissionKind.ManageRevisions },
            { PermissionKind.ApplicationDetailAUDNToggleRevisionEnableState, ScopedApplicationPermissionKind.ManageRevisions },

            { PermissionKind.RefreshSouthbank, ScopedApplicationPermissionKind.UpdateCache },

            { PermissionKind.BullseyeAdmin, ScopedApplicationPermissionKind.Admin },
            { PermissionKind.BullseyeGenerateCache, ScopedApplicationPermissionKind.UpdateCache },
            { PermissionKind.BullseyeDeletePatch, ScopedApplicationPermissionKind.ManageRevisions },
            { PermissionKind.BullseyeDeleteRevision, ScopedApplicationPermissionKind.ManageRevisions },
            { PermissionKind.BullseyeRegisterPatch, ScopedApplicationPermissionKind.ManageRevisions },
            { PermissionKind.BullseyeRegisterRevision, ScopedApplicationPermissionKind.ManageRevisions },
            { PermissionKind.BullseyeUpdatePreviousRevision, ScopedApplicationPermissionKind.ManageRevisions },
            { PermissionKind.BullseyeAppMarkLatestRevision, ScopedApplicationPermissionKind.ManageRevisions },
            { PermissionKind.BullseyeUpdateRevisionLiveState, ScopedApplicationPermissionKind.ManageRevisions },
            { PermissionKind.BullseyeViewPrivateModels, ScopedApplicationPermissionKind.ReadOnly },
        };
        var mappingTableMany = new Dictionary<PermissionKind, List<ScopedApplicationPermissionKind>>()
        {
            {
                PermissionKind.ApplicationDetailAUDNSubmitRevision,
                [
                    ScopedApplicationPermissionKind.ManageRevisions,
                    ScopedApplicationPermissionKind.SubmitRevisions
                ]
            },
            {
                PermissionKind.BullseyeRegisterPatch,
                [
                    ScopedApplicationPermissionKind.ManageRevisions,
                    ScopedApplicationPermissionKind.SubmitRevisions
                ]
            },
            {
                PermissionKind.BullseyeRegisterRevision,
                [
                    ScopedApplicationPermissionKind.ManageRevisions,
                    ScopedApplicationPermissionKind.SubmitRevisions
                ]
            },
        };
        foreach (var x in permissions)
        {
            if (mappingTable.TryGetValue(x, out var i))
            {
                mapped.Add(i);
            }
            if (mappingTableMany.TryGetValue(x, out var p))
            {
                mapped.AddRange(p);
            }
        }

        return mapped.Distinct().ToList();
    }
    #region Check Permission
    public async Task<bool> CheckApplicationPermission(
        string userId,
        string applicationId,
        params ScopedApplicationPermissionKind[] permissions)
    {
        var kinds = permissions.Distinct().ToArray();
        var userPermissions = await _permissionCacheService.GetUserByApplication(userId, applicationId);
        foreach (var x in kinds)
        {
            if (userPermissions.Contains(x))
                return true;
        }

        return false;
    }
    public Task<bool> CheckApplicationPermission(
        UserModel user,
        ApplicationDetailModel application,
        params ScopedApplicationPermissionKind[] permissions)
        => CheckApplicationPermission(user.Id, application.Id, permissions);
    public Task<bool> CheckApplicationPermission(
        UserModel user,
        string applicationId,
        params ScopedApplicationPermissionKind[] permissions)
        => CheckApplicationPermission(user.Id, applicationId, permissions);

    public async Task<bool> CheckApplicationPermission(
        string userId,
        string applicationId,
        params PermissionKind[] permissions)
    {
        // Allow when user has any of those global permissions and/or they're a superuser
        bool check = await CheckGlobalPermission(
            userId,
            PermissionFilterType.Any, 
            [.. permissions, PermissionKind.Superuser ]);
        if (check)
        {
            return true;
        }
        var mapped = MapPermission(permissions.Distinct().ToList());
        return await CheckApplicationPermission(userId, applicationId, mapped.ToArray());
    }
    public Task<bool> CheckApplicationPermission(
        UserModel user,
        ApplicationDetailModel application,
        params PermissionKind[] permissions)
    => CheckApplicationPermission(user.Id, application.Id, permissions);

    public Task<bool> CheckApplicationPermission(
        UserModel user,
        string applicationId,
        params PermissionKind[] permissions)
        => CheckApplicationPermission(user.Id, applicationId, permissions);
    #endregion
    
    #region Grant
    public async Task GrantManyApplicationForGroupAsync(
        string groupId,
        string applicationId,
        params ScopedApplicationPermissionKind[] kinds)
    {
        var session = await _mongoClient.StartSessionAsync();
        session.StartTransaction();
        try
        {

            var existingToUpdate = await _groupPermissionAppRepo.GetManyBy(new()
            {
                GroupId = groupId,
                ApplicationId = applicationId,
                KindsIn = kinds,
                Allow = false
            });

            foreach (var item in existingToUpdate)
            {
                item.Allow = true;
                await _groupPermissionAppRepo.InsertOrUpdate(item);
            }

            var existing = await _groupPermissionAppRepo.GetManyBy(
                new()
                {
                    GroupId = groupId,
                    ApplicationId = applicationId,
                    KindsIn = kinds,
                });
            var f = existing.Select(v => v.Kind).Distinct().ToArray();

            foreach (var x in kinds.Where(v => f.Contains(v) == false))
            {
                await _groupPermissionAppRepo.InsertOrUpdate(new()
                {
                    GroupId = groupId,
                    ApplicationId = applicationId,
                    Kind = x,
                    Allow = true
                });
            }

            await session.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex, (scope) =>
            {
                scope.SetTag($"param.{nameof(groupId)}", groupId);
                scope.SetTag($"param.{nameof(applicationId)}", applicationId);
                scope.SetTag($"param.{nameof(kinds)}", string.Join(", ", kinds.Select(v => v.ToString())));
            });
            await session.AbortTransactionAsync();
            throw;
        }

        await _permissionCacheService.CalculateGroup(groupId);
    }
    public Task GrantManyApplicationForGroupAsync(
        GroupModel group,
        ApplicationDetailModel application,
        params ScopedApplicationPermissionKind[] kinds)
        => GrantManyApplicationForGroupAsync(group.Id, application.Id, kinds);

    public Task GrantManyApplicationForGroupAsync(
        GroupModel group,
        string applicationId,
        params ScopedApplicationPermissionKind[] kinds) =>
        GrantManyApplicationForGroupAsync(group.Id, applicationId, kinds);
    #endregion
    
    #region Deny
    public async Task DenyManyApplicationForGroupAsync(
        string groupId,
        string applicationId,
        params ScopedApplicationPermissionKind[] kinds)
    {
        var session = await _mongoClient.StartSessionAsync();
        session.StartTransaction();

        try
        {
            var existingToUpdate = await _groupPermissionAppRepo.GetManyBy(new()
            {
                GroupId = groupId,
                ApplicationId = applicationId,
                KindsIn = kinds,
                Allow = true
            });

            foreach (var item in existingToUpdate)
            {
                item.Allow = false;
                await _groupPermissionAppRepo.InsertOrUpdate(item);
            }

            var existing = await _groupPermissionAppRepo.GetManyBy(
                new()
                {
                    GroupId = groupId,
                    ApplicationId = applicationId,
                    KindsIn = kinds,
                });
            var f = existing.Select(v => v.Kind).Distinct().ToArray();

            foreach (var x in kinds.Where(v => f.Contains(v) == false))
            {
                await _groupPermissionAppRepo.InsertOrUpdate(new()
                {
                    GroupId = groupId,
                    ApplicationId = applicationId,
                    Kind = x,
                    Allow = false
                });
            }

            await session.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex, (scope) =>
            {
                scope.SetTag($"param.{nameof(groupId)}", groupId);
                scope.SetTag($"param.{nameof(applicationId)}", applicationId);
                scope.SetTag($"param.{nameof(kinds)}", string.Join(", ", kinds.Select(v => v.ToString())));
            });
            await session.AbortTransactionAsync();
            throw;
        }

        await _permissionCacheService.CalculateGroup(groupId);
    }
    public Task DenyManyApplicationForGroupAsync(
        GroupModel group,
        ApplicationDetailModel application,
        params ScopedApplicationPermissionKind[] kinds)
        => DenyManyApplicationForGroupAsync(group.Id, application.Id, kinds);

    public Task DenyManyApplicationForGroupAsync(
        GroupModel group,
        string applicationId,
        params ScopedApplicationPermissionKind[] kinds) =>
        DenyManyApplicationForGroupAsync(group.Id, applicationId, kinds);
    #endregion
    
    #region Revoke
    public async Task RevokeManyApplicationForGroupAsync(
        string groupId,
        string applicationId,
        params ScopedApplicationPermissionKind[] kinds)
    {
        var session = await _mongoClient.StartSessionAsync();
        session.StartTransaction();
        try
        {

            var existing = await _groupPermissionAppRepo.GetManyBy(
                new()
                {
                    GroupId = groupId,
                    ApplicationId = applicationId,
                    KindsIn = kinds,
                });
            var ids = existing.Select(v => v.Id).Distinct().ToArray();
            await _groupPermissionAppRepo.Delete(ids);

            await session.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex, (scope) =>
            {
                scope.SetTag($"param.{nameof(groupId)}", groupId);
                scope.SetTag($"param.{nameof(applicationId)}", applicationId);
                scope.SetTag($"param.{nameof(kinds)}", string.Join(", ", kinds.Select(v => v.ToString())));
            });
            await session.AbortTransactionAsync();
            throw;
        }

        await _permissionCacheService.CalculateGroup(groupId);
    }

    public Task RevokeManyApplicationForGroupAsync(
        GroupModel group,
        ApplicationDetailModel application,
        params ScopedApplicationPermissionKind[] kinds)
        => RevokeManyApplicationForGroupAsync(group.Id, application.Id, kinds);
    
    public Task RevokeManyApplicationForGroupAsync(
        GroupModel group,
        string applicationId,
        params ScopedApplicationPermissionKind[] kinds)
        => RevokeManyApplicationForGroupAsync(group.Id, applicationId, kinds);
    #endregion
}