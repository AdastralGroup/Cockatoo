using System.Text.Json;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.DataAccess.Repositories.Group;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using NLog;

namespace Adastral.Cockatoo.DataAccess.Services;

[CockatooDependency(Priority = 0)]
public class MigratePermissionGroupService(IServiceProvider services) : BaseService(services)
{
    private readonly PermissionGroupRepository _permissionGroupRepository =
        services.GetRequiredService<PermissionGroupRepository>();

    private readonly GroupRepository _groupRepository = services.GetRequiredService<GroupRepository>();

    private readonly PermissionGroupUserAssociationRepository _permissionGroupUserAssociationRepository =
        services.GetRequiredService<PermissionGroupUserAssociationRepository>();

    private readonly GroupUserAssociationRepository _groupUserAssociationRepository =
        services.GetRequiredService<GroupUserAssociationRepository>();

    private readonly PermissionRoleRepository _permissionRoleRepository =
        services.GetRequiredService<PermissionRoleRepository>();

    private readonly GroupPermissionGlobalRepository _groupPermissionGlobalRepository =
        services.GetRequiredService<GroupPermissionGlobalRepository>();

    private readonly UserRepository _userRepo = services.GetRequiredService<UserRepository>();

    private readonly MongoClient _mongoClient = services.GetRequiredService<MongoClient>();

    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public override async Task InitializeAsync()
    {
        await Run();
    }

    private async Task Run()
    {
        var groupCount = await _groupRepository.GetAllCount();
        var permissionGroupCount = await _permissionGroupRepository.GetAllCount();
        if (permissionGroupCount < 1)
        {
            _log.Trace($"No permission groups found. Skipping");
            return;
        }
        if (groupCount > 0)
        {
            _log.Debug($"Not going to migrate since {nameof(GroupRepository)} has one or more documents");
            return;
        }

        // key: PermissionGroupModel.Id
        // value: GroupModel.Id
        var mapping = new Dictionary<string, string>();

        List<(PermissionGroupModel, GroupModel)> groupAssociations = [];
        List<(PermissionGroupUserAssociationModel, GroupUserAssociationModel)> userAssociations = [];
        List<(PermissionRoleModel, GroupPermissionGlobalModel)> permissionAssociations = [];

        async Task Revert()
        {
            foreach (var (_, group) in groupAssociations)
            {
                try
                {
                    await _groupRepository.Delete(group.Id);
                }
                catch (Exception ex)
                {
                    _log.Error($"Revert|Failed to delete {nameof(GroupModel)} with Id {group.Id}\n{JsonSerializer.Serialize(group, SerializerOptions)}\n{ex}");
                    SentrySdk.CaptureException(
                        new ApplicationException($"Failed to delete {nameof(GroupModel)} with Id {group.Id} due to aborting transaction", ex),
                        (scope) =>
                        {
                            scope.SetExtra(nameof(group), group);
                            scope.SetExtra($"{nameof(group)}.{nameof(group.Id)}", group.Id);
                        });
                }
            }
            foreach (var (_, association) in userAssociations)
            {
                try
                {
                    await _groupUserAssociationRepository.Delete(association.Id);
                }
                catch (Exception ex)
                {
                    _log.Error($"Revert|Failed to delete {nameof(GroupUserAssociationModel)} with Id {association.Id}\n{JsonSerializer.Serialize(association, SerializerOptions)}\n{ex}");
                    SentrySdk.CaptureException(
                        new ApplicationException($"Failed to delete {nameof(GroupUserAssociationModel)} with Id {association.Id} due to aborting transaction", ex),
                        (scope) =>
                        {
                            scope.SetExtra(nameof(association), association);
                            scope.SetExtra($"{nameof(association)}.{nameof(association.Id)}", association.Id);
                        });
                }
            }
            foreach (var (_, perm) in permissionAssociations)
            {
                try
                {
                    await _groupPermissionGlobalRepository.Delete(perm.Id);
                }
                catch (Exception ex)
                {
                    _log.Error($"Revert|Failed to delete {nameof(GroupPermissionGlobalModel)} with Id {perm.Id}\n{JsonSerializer.Serialize(perm, SerializerOptions)}\n{ex}");
                    SentrySdk.CaptureException(
                        new ApplicationException($"Failed to delete {nameof(GroupPermissionGlobalModel)} with Id {perm.Id} due to aborting transaction", ex),
                        (scope) =>
                        {
                            scope.SetExtra(nameof(perm), perm);
                            scope.SetExtra($"{nameof(perm)}.{nameof(perm.Id)}", perm.Id);
                        });
                }
            }
        }

        var permissionGroupModels = await _permissionGroupRepository.GetAll();
        var currentStep = nameof(PermissionGroupModel);
        try
        {
            foreach (var permissionGroup in permissionGroupModels)
            {
                var groupModel = await MigratePermissionGroup(permissionGroup);
                mapping[permissionGroup.Id] = groupModel.Id;
                groupAssociations.Add((permissionGroup, groupModel));
            }
            currentStep = nameof(PermissionGroupUserAssociationModel);
            userAssociations = await MigratePermissionGroupUserAssociation(mapping);
            currentStep = nameof(PermissionRoleModel);
            permissionAssociations = await MigratePermissionRoles(mapping);
            currentStep = "CommitTransaction";
        }
        catch (Exception ex)
        {
            await Revert();
            _log.Error($"Failed to migrate groups (step: {currentStep})\n{ex}");
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetExtra(nameof(currentStep), currentStep);

                scope.SetExtra(nameof(groupAssociations), groupAssociations);
                scope.SetExtra(nameof(userAssociations), userAssociations);
                scope.SetExtra(nameof(permissionAssociations), permissionAssociations);
            });
            throw new ApplicationException("Failed to migrate groups.", ex);
        }
    }

    private async Task<GroupModel> MigratePermissionGroup(PermissionGroupModel permissionGroup)
    {
        var model = new GroupModel()
        {
            Name = permissionGroup.Name,
            Priority = permissionGroup.Priority,
            CreatedAt = new BsonTimestamp(permissionGroup.GetCreatedAtTimestamp())
        };
        try
        {
            await _groupRepository.InsertOrUpdate(model);
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to migrate record {nameof(PermissionGroupModel)} ({permissionGroup.Id}) to {nameof(GroupModel)}\n{ex}");
            try
            {
                await _groupRepository.Delete(model.Id);
            }
            catch (Exception iex)
            {
                _log.Error($"Failed to revert insert of {nameof(GroupModel)} ({model.Id})\n{iex}");
            }
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetExtra(nameof(permissionGroup), JsonSerializer.Serialize(permissionGroup, SerializerOptions));
                scope.SetExtra(nameof(model), JsonSerializer.Serialize(model, SerializerOptions));
            });
            throw new ApplicationException($"Failed to migrate record {nameof(PermissionGroupModel)} ({permissionGroup.Id}) to {nameof(GroupModel)}", ex);
        }
        return model;
    }

    /// <summary>
    /// Migrate <see cref="PermissionGroupUserAssociationModel"/> to <see cref="GroupUserAssociationModel"/>
    /// </summary>
    private async Task<List<(PermissionGroupUserAssociationModel, GroupUserAssociationModel)>> MigratePermissionGroupUserAssociation(Dictionary<string, string> mapping)
    {
        var result = new List<(PermissionGroupUserAssociationModel, GroupUserAssociationModel)>();
        try
        {
            foreach (var (permissionGroupId, groupId) in mapping)
            {
                var associations = await _permissionGroupUserAssociationRepository.GetAllWithGroup(permissionGroupId);
                foreach (var assoc in associations)
                {
                    var user = await _userRepo.GetById(assoc.UserId);
                    if (user == null)
                    {
                        _log.Debug($"assoc={assoc.Id}| Ignoring document since user doesn't exist anymore ({assoc.UserId})");
                        continue;
                    }
                    var model = new GroupUserAssociationModel()
                    {
                        GroupId = groupId,
                        UserId = assoc.UserId
                    };
                    model = await _groupUserAssociationRepository.InsertOrUpdate(model);
                    result.Add((assoc, model));
                }
            }
        }
        catch (Exception ex)
        {
            foreach (var (p, x) in result)
            {
                try
                {
                    await _groupUserAssociationRepository.Delete(x.Id);
                }
                catch (Exception iex)
                {
                    _log.Error($"Failed to delete record to revert {nameof(GroupPermissionGlobalModel)} with Id {x.Id} (for {nameof(PermissionGroupUserAssociationModel)} {p.Id})\n{iex}");
                }
            }
            _log.Error($"Failed to migrate records from {nameof(PermissionGroupUserAssociationModel)} to {nameof(GroupUserAssociationModel)}\n{ex}");
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetExtra(nameof(mapping), JsonSerializer.Serialize(mapping, SerializerOptions));
                scope.SetExtra($"{nameof(result)}[0]", JsonSerializer.Serialize(result.Select(v => v.Item1).ToArray(), SerializerOptions));
                scope.SetExtra($"{nameof(result)}[1]", JsonSerializer.Serialize(result.Select(v => v.Item2).ToArray(), SerializerOptions));
            });
            throw new ApplicationException($"Failed to migrate records from {nameof(PermissionGroupUserAssociationModel)} to {nameof(GroupUserAssociationModel)}", ex);
        }

        return result;
    }

    /// <summary>
    /// Migrate <see cref="PermissionRoleModel"/> to <see cref="GroupPermissionGlobalModel"/>
    /// </summary>
    private async Task<List<(PermissionRoleModel, GroupPermissionGlobalModel)>> MigratePermissionRoles(Dictionary<string, string> mapping)
    {
        var result = new List<(PermissionRoleModel, GroupPermissionGlobalModel)>();
        try
        {
            foreach (var (permissionGroupId, groupId) in mapping)
            {
                var roles = await _permissionRoleRepository.GetForGroup(permissionGroupId);
                foreach (var role in roles)
                {
                    var model = new GroupPermissionGlobalModel()
                    {
                        GroupId = groupId,
                        Allow = role.Allow,
                        Kind = role.Kind,
                    };
                    model = await _groupPermissionGlobalRepository.InsertOrUpdate(model);
                    result.Add((role, model));
                }
            }
        }
        catch (Exception ex)
        {
            foreach (var (p, x) in result)
            {
                try
                {
                    await _groupPermissionGlobalRepository.Delete(x.Id);
                }
                catch (Exception iex)
                {
                    _log.Error($"Failed to delete record to revert {nameof(GroupPermissionGlobalModel)} with Id {x.Id} (for {nameof(PermissionRoleModel)} {p.Id})\n{iex}");
                }
            }
            _log.Error($"Failed to migrate records from {nameof(PermissionRoleModel)} to {nameof(GroupPermissionGlobalModel)}\n{ex}");
            SentrySdk.CaptureException(ex, scope =>
            {
                scope.SetExtra(nameof(mapping), JsonSerializer.Serialize(mapping, SerializerOptions));
                scope.SetExtra($"{nameof(result)}[0]", JsonSerializer.Serialize(result.Select(v => v.Item1).ToArray(), SerializerOptions));
                scope.SetExtra($"{nameof(result)}[1]", JsonSerializer.Serialize(result.Select(v => v.Item2).ToArray(), SerializerOptions));
            });
            throw new ApplicationException($"Failed to migrate records from {nameof(PermissionRoleModel)} to {nameof(GroupPermissionGlobalModel)}", ex);
        }
        return result;
    }
}