using System.Text.Json;
using System.Text.Json.Serialization;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.DataAccess.Repositories.Group;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NLog;

namespace Adastral.Cockatoo.Services;

[CockatooDependency]
public class GroupService : BaseService
{
    private readonly UserRepository _userRepo;
    private readonly GroupRepository _groupRepo;
    private readonly GroupUserAssociationRepository _groupUserAssocRepo;
    private readonly MongoClient _mongoClient;
    private readonly GroupPermissionGlobalRepository _groupPermGlobalRepo;
    private readonly GroupPermissionApplicationRepository _groupPermAppRepo;
    private readonly PermissionCacheService _permissionCacheService;
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public GroupService(IServiceProvider services)
        : base(services)
    {
        _userRepo = services.GetRequiredService<UserRepository>();
        _groupRepo = services.GetRequiredService<GroupRepository>();
        _groupUserAssocRepo = services.GetRequiredService<GroupUserAssociationRepository>();
        _groupPermGlobalRepo = services.GetRequiredService<GroupPermissionGlobalRepository>();
        _groupPermAppRepo = services.GetRequiredService<GroupPermissionApplicationRepository>();
        _permissionCacheService = services.GetRequiredService<PermissionCacheService>();
        _mongoClient = services.GetRequiredService<MongoClient>();
    }

    #region Get Users In
    /// <summary>
    /// Get a list of all user models in the <paramref name="groupId"/> specified.
    /// </summary>
    /// <remarks>
    /// Calls <see cref="GetUsersInAsync(GroupModel)"/> when the group could be found in <see cref="GroupRepository"/>
    /// </remarks>
    public async Task<List<UserModel>> GetUsersInAsync(string groupId)
    {
        if (string.IsNullOrEmpty(groupId))
        {
            throw new ArgumentException($"Must not be null or empty", nameof(groupId));
        }
        var group = await _groupRepo.GetById(groupId);
        if (group == null)
        {
            throw new ArgumentException($"Could not find {nameof(GroupModel)} with Id {groupId}", nameof(groupId));
        }

        return await GetUsersInAsync(group);
    }

    /// <summary>
    /// Get a list of all user models in the <paramref name="group"/> specified.
    /// </summary>
    public async Task<List<UserModel>> GetUsersInAsync(GroupModel group)
    {
        var associations = await _groupUserAssocRepo.GetAllForGroup(group.Id);
        var result = new List<UserModel>();

        foreach (var item in associations)
        {
            var user = await _userRepo.GetById(item.UserId);
            if (user != null)
            {
                result.Add(user);
            }
        }

        return result;
    }
    #endregion

    #region Add User
    /// <summary>
    /// Add a User to a Group.
    /// </summary>
    /// <param name="groupId">Id of the <see cref="GroupModel"/></param>
    /// <param name="userId">Id of the <see cref="UserModel"/></param>
    /// <remarks>
    /// Fetches the <see cref="GroupModel"/> and <see cref="UserModel"/>, then calls <see cref="AddUserAsync(GroupModel, UserModel)"/>
    /// </remarks>
    public async Task AddUserAsync(string groupId, string userId)
    {
        if (string.IsNullOrEmpty(groupId))
        {
            throw new ArgumentException($"Must not be null or empty", nameof(groupId));
        }
        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException($"Must not be null or empty", nameof(userId));
        }

        var groupModel = await _groupRepo.GetById(groupId);
        var userModel = await _userRepo.GetById(userId);

        if (groupModel == null || userModel == null)
        {
            throw new AggregateException([
                new ArgumentException($"Could not find {nameof(GroupModel)} with Id {groupId}"),
                new ArgumentException($"Could not find {nameof(UserModel)} with Id {userId}")
            ]);
        }
        if (groupModel == null)
        {
            throw new ArgumentException($"Could not find {nameof(GroupModel)} with Id {groupId}");
        }
        if (userModel == null)
        {
            throw new ArgumentException($"Could not find {nameof(UserModel)} with Id {userId}");
        }

        await AddUserAsync(groupModel, userModel);
    }

    /// <summary>
    /// Add a <paramref name="user"/> to the <paramref name="group"/> provided.
    /// </summary>
    /// <param name="group">Group to add the user into.</param>
    /// <param name="user">User to be added into the group</param>
    /// <remarks>
    /// <para>Nothing will be done if <see cref="GroupUserAssociationRepository.ExistsByGroupAndUser(GroupModel, UserModel)"/> returns <see langword="true"/></para>
    ///
    /// <para>Uses <see cref="SentrySdk.CaptureException(Exception, Action{Scope})"/></para>
    /// </remarks>
    public async Task AddUserAsync(GroupModel group, UserModel user)
    {
        var associationExists = await _groupUserAssocRepo.ExistsByGroupAndUser(group, user);
        if (associationExists)
        {
            return;
        }

        var session = await _mongoClient.StartSessionAsync();
        session.StartTransaction();

        try
        {
            var model = new GroupUserAssociationModel()
            {
                UserId = user.Id,
                GroupId = group.Id,
                IsDeleted = false
            };
            model = await _groupUserAssocRepo.InsertOrUpdate(model);

            await session.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await session.AbortTransactionAsync();
            session.Dispose();
            _log.Error($"Failed to add user {user.FormatName()} into group {group.FormatName()} (userId: {user.Id}, groupId: {group.Id})\n{ex}");
            SentrySdk.CaptureException(ex, (scope) =>
            {
                scope.SetExtra("group", group);
                scope.SetExtra("user", user);
                scope.SetTag("param.group.Id", group.Id);
                scope.SetTag("param.user.Id", user.Id);
            });
            throw;
        }
        session.Dispose();
    }
    #endregion

    #region Add Multiple Users
    /// <summary>
    /// Add many users by their IDs to the group provided.
    /// </summary>
    /// <remarks>
    /// Fetches <see cref="GroupModel"/> by the <paramref name="groupId"/> provided, tries to get all instances of <see cref="UserModel"/>
    /// where the ID matches (for all items in <paramref name="userIds"/>), then calls <see cref="AddManyUsersAsync(GroupModel, IEnumerable{UserModel})"/>
    /// </remarks>
    public async Task AddManyUsersAsync(string groupId, IEnumerable<string> userIds)
    {
        if (string.IsNullOrEmpty(groupId))
        {
            throw new ArgumentException($"Must not be null or empty", nameof(groupId));
        }
        var group = await _groupRepo.GetById(groupId);
        if (group == null)
        {
            throw new ArgumentException($"Could not find {nameof(GroupModel)} with Id {groupId}", nameof(groupId));
        }
        await AddManyUsersAsync(group, userIds);
    }
    /// <summary>
    /// Add many users by their IDs to the <paramref name="group"/> provided.
    /// </summary>
    /// <remarks>
    /// Fetches all users in <paramref name="userIds"/>, then passes that through to <see cref="AddManyUsersAsync(GroupModel, IEnumerable{UserModel})"/>
    /// </remarks>
    public async Task AddManyUsersAsync(GroupModel group, IEnumerable<string> userIds)
    {
        if (userIds.Count() < 1)
        {
            throw new ArgumentException($"Must have one or more IDs", nameof(userIds));
        }
        var userList = new List<UserModel>();
        foreach (var id in userIds.Select(v => v.Trim().ToLower()).Distinct())
        {
            if (string.IsNullOrEmpty(id))
                continue;
            try
            {
                var user = await _userRepo.GetById(id);
                if (user != null)
                    userList.Add(user);
            }
            catch (Exception ex)
            {
                _log.Warn($"Failed to get {nameof(UserModel)} with Id {id}\n{ex}");
            }
        }
        if (userList.Count < 1)
        {
            throw new ArgumentException($"No valid users found", nameof(userIds));
        }

        await AddManyUsersAsync(group, userList);
    }
    /// <summary>
    /// Add many <paramref name="users"/> to the <paramref name="group"/> provided (if they're not in it already)
    /// </summary>
    /// <param name="group">Group to add the users into.</param>
    /// <param name="users">Users to add into the group.</param>
    /// <remarks>
    /// Uses <see cref="SentrySdk.CaptureException(Exception, Action{Scope})"/>
    /// </remarks>
    public async Task AddManyUsersAsync(GroupModel group, IEnumerable<UserModel> users)
    {
        var userList = users.ToList();
        // var session = await _mongoClient.StartSessionAsync();
        // session.StartTransaction();
        string? currentUserId = null;
        var documentsAdded = new List<GroupUserAssociationModel>();
        async Task Revert()
        {
            _log.Debug($"Reverting documents added due to an exception");
            foreach (var item in documentsAdded)
            {
                try
                {
                    await _groupUserAssocRepo.Delete(item.Id);
                }
                catch (Exception ex)
                {
                    _log.Error($"Failed to revert {nameof(GroupUserAssociationModel)} with Id {item.Id}\n{ex}");
                }
            }
        }
        try
        {
            foreach (var user in userList)
            {
                currentUserId = user.Id;

                var exists = await _groupUserAssocRepo.ExistsByGroupAndUser(group, user);
                if (exists)
                    continue;

                var model = new GroupUserAssociationModel()
                {
                    UserId = user.Id,
                    GroupId = group.Id,
                    IsDeleted = false
                };
                await _groupUserAssocRepo.InsertOrUpdate(model);
            }
        }
        catch (Exception ex)
        {
            // await session.AbortTransactionAsync();
            // session.Dispose();
            await Revert();

            _log.Error($"Failed to add many users into group {group.FormatName()}\n" +
                       $"groupId: {group.Id}\n" +
                       "users: " + string.Join(", ", userList.Select(v => v.Id)) + $"\n{ex}");
            SentrySdk.CaptureException(ex, (scope) =>
            {
                scope.SetExtra($"param.{nameof(users)}.Id", userList.Select(v => v.Id).ToArray());
                scope.SetExtra($"param.{nameof(group)}.Id", group.Id);
                scope.SetExtra($"currentUserId", currentUserId);
            });
            throw;
        }
    }
    #endregion

    #region Remove User
    /// <summary>
    /// Remove a user from a group.
    /// </summary>
    /// <param name="groupId">Id of the <see cref="GroupModel"/></param>
    /// <param name="userId">Id of the <see cref="UserModel"/></param>
    /// <remarks>
    /// Fetches the <see cref="GroupModel"/> and <see cref="UserModel"/>, then calls <see cref="RemoveUserAsync(GroupModel, UserModel)"/>
    /// </remarks>
    public async Task RemoveUserAsync(string groupId, string userId)
    {
        if (string.IsNullOrEmpty(groupId))
        {
            throw new ArgumentException($"Must not be null or empty", nameof(groupId));
        }
        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException($"Must not be null or empty", nameof(userId));
        }

        var groupModel = await _groupRepo.GetById(groupId);
        var userModel = await _userRepo.GetById(userId);

        if (groupModel == null || userModel == null)
        {
            throw new AggregateException([
                new ArgumentException($"Could not find {nameof(GroupModel)} with Id {groupId}"),
                new ArgumentException($"Could not find {nameof(UserModel)} with Id {userId}")
            ]);
        }
        if (groupModel == null)
        {
            throw new ArgumentException($"Could not find {nameof(GroupModel)} with Id {groupId}");
        }
        if (userModel == null)
        {
            throw new ArgumentException($"Could not find {nameof(UserModel)} with Id {userId}");
        }

        await RemoveUserAsync(groupModel, userModel);
    }
    /// <summary>
    /// Remove a <paramref name="user"/> from the <paramref name="group"/> specified.
    /// </summary>
    /// <remarks>
    /// When any documents exist in <see cref="GroupUserAssociationRepository"/> where the user & group matches, and
    /// <see cref="GroupUserAssociationModel.IsDeleted"/> is set to false, then it will be set to true.
    /// Otherwise, if any documents don't exist in <see cref="GroupUserAssociationRepository"/> where the user & group
    /// matches, then nothing will be done.
    /// </remarks>
    public async Task RemoveUserAsync(GroupModel group, UserModel user)
    {
        var associationExists = await _groupUserAssocRepo.ExistsByGroupAndUser(group, user, false);
        if (!associationExists)
            return;

        var session = await _mongoClient.StartSessionAsync();
        session.StartTransaction();
        try
        {
            var existing = await _groupUserAssocRepo.GetAllWithGroupAndUser(group, user, true);
            // Update models when records exist where IsDeleted is false
            // OR insert a model, when no records exist.
            if (existing.Any(v => v.IsDeleted == false))
            {
                var targetIds = existing.Where(v => v.IsDeleted == false).Select(v => v.Id).ToArray();
                await _groupUserAssocRepo.SetDeleteState(true, targetIds);
            }
            await session.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await session.AbortTransactionAsync();
            session.Dispose();
            _log.Error($"Failed to remove user {user.FormatName()} from group {group.FormatName()} (userId: {user.Id}, groupId: {group.Id})\n{ex}");
            SentrySdk.CaptureException(ex, (scope) =>
            {
                scope.SetExtra("group", group);
                scope.SetExtra("user", user);
                scope.SetTag("param.group.Id", group.Id);
                scope.SetTag("param.user.Id", user.Id);
            });
            throw;
        }
        session.Dispose();
    }
    #endregion

    public class DeleteGroupResult
    {
        public GroupModel? Group { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<GroupPermissionGlobalModel>? GlobalPermissions { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<GroupPermissionApplicationModel>? ApplicationPermissions { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<UserModel>? AffectedUsers { get; set; }
    }
    public async Task<DeleteGroupResult> DeleteGroupAsync(GroupModel group)
    {
        // var session = await _mongoClient.StartSessionAsync();
        var result = new DeleteGroupResult()
        {
            Group = group
        };
        async Task Revert()
        {
            if (result.Group != null)
            {
                if (!await _groupRepo.Exists(result.Group.Id))
                {
                    try
                    {
                        await _groupRepo.InsertOrUpdate(result.Group);
                    }
                    catch (Exception ex)
                    {
                        _log.Warn($"Revert|Failed to re-insert {nameof(GroupModel)}\n{JsonSerializer.Serialize(result.Group, SerializerOptions)}\n{ex}");
                    }
                }
            }
            if (result.AffectedUsers?.Count > 0)
            {
                foreach (var usr in result.AffectedUsers)
                {
                    var model = new GroupUserAssociationModel()
                    {
                        UserId = usr.Id,
                        GroupId = group.Id,
                        IsDeleted = false
                    };
                    try
                    {
                        await _groupUserAssocRepo.InsertOrUpdate(model);
                    }
                    catch (Exception ex)
                    {
                        _log.Warn($"Revert|Failed to re-insert {nameof(GroupUserAssociationModel)}\n{JsonSerializer.Serialize(model, SerializerOptions)}\n{ex}");
                    }
                }
            }
            if (result.GlobalPermissions?.Count > 0)
            {
                foreach (var glb in result.GlobalPermissions)
                {
                    try
                    {
                        await _groupPermGlobalRepo.InsertOrUpdate(glb);
                    }
                    catch (Exception ex)
                    {
                        _log.Warn($"Revert|Failed to re-insert {nameof(GroupPermissionGlobalModel)}\n{JsonSerializer.Serialize(glb, SerializerOptions)}\n{ex}");
                    }
                }
            }
            if (result.ApplicationPermissions?.Count > 0)
            {
                foreach (var app in result.ApplicationPermissions)
                {
                    try
                    {
                        await _groupPermAppRepo.InsertOrUpdate(app);
                    }
                    catch (Exception ex)
                    {
                        _log.Warn($"Revert|Failed to re-insert {nameof(GroupPermissionApplicationModel)}\n{JsonSerializer.Serialize(app, SerializerOptions)}\n{ex}");
                    }
                }
            }
            if (result.AffectedUsers?.Count > 0)
            {
                foreach (var usr in result.AffectedUsers)
                {
                    try
                    {
                        await _permissionCacheService.CalculateUser(usr.Id);
                    }
                    catch (Exception ex)
                    {
                        _log.Warn($"Revert|Failed to recalculate user permissions for {usr.FormatName()} ({usr.Id})\n{ex}");
                    }
                }
            }
        }
        try
        {
            await _groupRepo.Delete(group.Id);

            var userAssociations = await _groupUserAssocRepo.HardDeleteByGroupId(group.Id);
            result.AffectedUsers = [];
            foreach (var x in userAssociations.DistinctBy(v => v.UserId))
            {
                try
                {
                    var user = await _userRepo.GetById(x.UserId);
                    if (user != null)
                    {
                        result.AffectedUsers.Add(user);
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn($"Failed to get user with Id {x.UserId}\n{ex}");
                }
            }

            result.GlobalPermissions = await _groupPermGlobalRepo.GetManyByGroup(group.Id);
            await _groupPermGlobalRepo.Delete(result.GlobalPermissions.Select(v => v.Id).ToArray());

            result.ApplicationPermissions = await _groupPermAppRepo.GetManyByGroup(group.Id);
            await _groupPermAppRepo.Delete(result.ApplicationPermissions.Select(v => v.Id).ToArray());

            _log.Debug($"Recalculating permissions for {result.AffectedUsers.Count} users.");
            foreach (var user in result.AffectedUsers)
            {
                await _permissionCacheService.CalculateUser(user.Id);
            }
        }
        catch (Exception ex)
        {
            await Revert();
            _log.Error($"Failed to delete group {group.FormatName()} (groupId: {group.Id})\n{ex}");
            SentrySdk.CaptureException(ex, (scope) =>
            {
                scope.SetTag($"param.{nameof(group)}.Id", group.Id);
            });
            throw;
        }
        // session.Dispose();
        return result;
    }
}