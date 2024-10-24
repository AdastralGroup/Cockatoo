using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.DataAccess.Repositories.Group;
using Adastral.Cockatoo.Services.WebApi.Models.Request;
using Adastral.Cockatoo.Services.WebApi.Models.Response;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using NLog;

namespace Adastral.Cockatoo.Services.WebApi;

[CockatooDependency]
public class AdminGroupWebService : BaseService
{
    private readonly ApplicationDetailRepository _applicationRepo;
    private readonly GroupRepository _groupRepo;
    private readonly GroupPermissionGlobalRepository _groupPermGlobalRepo;
    private readonly GroupPermissionApplicationRepository _groupPermAppRepo;
    private readonly GroupUserAssociationRepository _groupUserAssociationRepository;
    private readonly UserRepository _userRepository;
    private readonly PermissionCacheService _permissionCacheService;
    private readonly PermissionService _permissionService;
    private readonly GroupService _groupService;
    private readonly MongoClient _mongoClient;
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public AdminGroupWebService(IServiceProvider services)
        : base(services)
    {
        _applicationRepo = services.GetRequiredService<ApplicationDetailRepository>();
        _groupRepo = services.GetRequiredService<GroupRepository>();
        _groupPermGlobalRepo = services.GetRequiredService<GroupPermissionGlobalRepository>();
        _groupPermAppRepo = services.GetRequiredService<GroupPermissionApplicationRepository>();
        _groupUserAssociationRepository = services.GetRequiredService<GroupUserAssociationRepository>();
        _userRepository = services.GetRequiredService<UserRepository>();
        _permissionCacheService = services.GetRequiredService<PermissionCacheService>();
        _permissionService = services.GetRequiredService<PermissionService>();
        _groupService = services.GetRequiredService<GroupService>();
        _mongoClient = services.GetRequiredService<MongoClient>();
    }

    public async Task<AdminGroupV1ListResponse> List()
    {
        var response = new AdminGroupV1ListResponse();
        foreach (var x in await _groupRepo.GetAll())
        {
            var item = await Details(x.Id);
            if (item != null)
            {
                response.Items.Add(item);
            }
        }

        return response;
    }

    public async Task<AdminGroupV1DetailResponse?> Details(string groupId)
    {
        var model = await _groupRepo.GetById(groupId);
        if (model == null)
            return null;

        AdminGroupV1DetailResponse result = new()
        {
            Id = model.Id,
            Name = model.Name,
            Priority = model.Priority,
            CreatedAt = model.CreatedAt.Value
        };
        var associations = await _groupUserAssociationRepository.GetAllForGroup(groupId);
        foreach (var item in associations ?? [])
        {
            var user = await _userRepository.GetById(item.UserId);
            if (user != null)
            {
                var x = new UserV1StrippedResponse();
                x.FromModel(user);
                result.Users.Add(x);
            }
        }
        result.GlobalPermissions = await _groupPermGlobalRepo.GetManyByGroup(groupId);
        result.ApplicationPermissions = await _groupPermAppRepo.GetManyByGroup(groupId);

        return result;
    }

    [Flags]
    public enum DeleteResult
    {
        Unknown = 0,
        Success = 1,
        NotFound = 2
    }

    public async Task<DeleteResult> Delete(string groupId)
    {
        var model = await _groupRepo.GetById(groupId);
        if (model == null)
        {
            return DeleteResult.NotFound;
        }

        await _groupService.DeleteGroupAsync(model);

        return DeleteResult.Success;
    }

    [Flags]
    public enum AddUserToGroupResult
    {
        Unknown = 0,
        Success = 1,
        GroupNotFound = 2,
        UserNotFound = 4
    }

    public async Task<AddUserToGroupResult> AddUserToGroup(string groupId, string userId)
    {
        if (string.IsNullOrEmpty(groupId))
        {
            return AddUserToGroupResult.GroupNotFound;
        }
        if (string.IsNullOrEmpty(userId))
        {
            return AddUserToGroupResult.UserNotFound;
        }

        var group = await _groupRepo.GetById(groupId);
        if (group == null)
        {
            return AddUserToGroupResult.GroupNotFound;
        }
        var user = await _userRepository.GetById(userId);
        if (user == null)
        {
            return AddUserToGroupResult.UserNotFound;
        }

        var existing = await _groupUserAssociationRepository.GetAllWithUserAndGroup(userId, groupId);
        if (existing.Count < 1)
        {
            await _groupService.AddUserAsync(group, user);
        }
        await _permissionCacheService.CalculateUser(userId);
        return AddUserToGroupResult.Success;
    }

    [Flags]
    public enum InsertPermissionResult
    {
        Unknown = 0,
        Success = 1,
        Failure = 2,
        GroupNotFound = 4,
        PermissionAlreadyExists = 8,
        ApplicationNotFound = 16
    }
    
    public Task<InsertPermissionResult> GrantGlobalPermission(string groupId, PermissionKind kind)
    {
        return InsertGlobalPermission(groupId, kind, true);
    }
    public Task<InsertPermissionResult> DenyGlobalPermission(string groupId, PermissionKind kind)
    {
        return InsertGlobalPermission(groupId, kind, false);
    }
    public async Task<InsertPermissionResult> InsertGlobalPermission(string groupId, PermissionKind kind, bool allow)
    {
        if (string.IsNullOrEmpty(groupId))
        {
            return InsertPermissionResult.Failure | InsertPermissionResult.GroupNotFound;
        }

        var group = await _groupRepo.GetById(groupId);
        if (group == null)
        {
            return InsertPermissionResult.Failure | InsertPermissionResult.GroupNotFound;
        }

        var existingRole = await _groupPermGlobalRepo.GetManyBy(groupId: groupId, kinds: [kind], allow: allow);
        if (existingRole.Count > 0)
        {
            return InsertPermissionResult.Success | InsertPermissionResult.PermissionAlreadyExists;
        }

        var model = new GroupPermissionGlobalModel()
        {
            GroupId = groupId,
            Kind = kind,
            Allow = allow
        };
        await _groupPermGlobalRepo.InsertOrUpdate(model);
        await _permissionCacheService.CalculateUser(groupId);
        return InsertPermissionResult.Success;
    }


    public Task<InsertPermissionResult> GrantApplicationPermission(string groupId,
        string applicationId,
        ScopedApplicationPermissionKind kind)
    {
        return InsertApplicationPermission(groupId, applicationId, kind, true);
    }

    public Task<InsertPermissionResult> DenyApplicationPermission(string groupId,
        string applicationId,
        ScopedApplicationPermissionKind kind)
    {
        return InsertApplicationPermission(groupId, applicationId, kind, false);
    }
    
    public async Task<InsertPermissionResult> InsertApplicationPermission(
        string groupId,
        string applicationId,
        ScopedApplicationPermissionKind kind,
        bool allow)
    {
        if (string.IsNullOrEmpty(groupId))
        {
            return InsertPermissionResult.Failure | InsertPermissionResult.GroupNotFound;
        }
        if (string.IsNullOrEmpty(applicationId))
        {
            return InsertPermissionResult.Failure | InsertPermissionResult.ApplicationNotFound;
        }
        
        var group = await _groupRepo.GetById(groupId);
        if (group == null)
        {
            return InsertPermissionResult.Failure | InsertPermissionResult.GroupNotFound;
        }

        if (await _applicationRepo.ExistsById(applicationId) == false)
        {
            return InsertPermissionResult.Failure | InsertPermissionResult.ApplicationNotFound;
        }

        var existingRole = await _groupPermAppRepo.GetManyBy(new()
        {
            GroupId = groupId,
            ApplicationId = applicationId,
            KindsIn = [kind],
            Allow = allow
        });
        if (existingRole.Count > 0)
        {
            return InsertPermissionResult.Success | InsertPermissionResult.PermissionAlreadyExists;
        }

        var model = new GroupPermissionApplicationModel()
        {
            GroupId = groupId,
            ApplicationId = applicationId,
            Kind = kind,
            Allow = allow
        };
        await _groupPermAppRepo.InsertOrUpdate(model);
        await _permissionCacheService.CalculateUser(groupId);
        return InsertPermissionResult.Success;
    }

    [Flags]
    public enum RevokePermissionResult
    {
        Unknown = 0,
        Success = 1,
        Failure = 2,
        GroupNotFound = 4,
        PermissionNotFound = 8,
        ApplicationNotFound = 16
    }
    public async Task<RevokePermissionResult> RevokeGlobalPermission(string groupId, PermissionKind kind)
    {
        var group = await _groupRepo.GetById(groupId);
        if (group == null)
        {
            return RevokePermissionResult.Failure | RevokePermissionResult.GroupNotFound;
        }

        var existingRole = await _groupPermGlobalRepo.GetManyBy(groupId: groupId, kinds: [kind]);
        if (existingRole.Count < 1)
        {
            return RevokePermissionResult.Failure | RevokePermissionResult.PermissionNotFound;
        }

        await _groupPermGlobalRepo.Delete(existingRole.Select(v => v.Id).ToArray());
        await _permissionCacheService.CalculateGroup(groupId);
        return RevokePermissionResult.Success;
    }

    public async Task<RevokePermissionResult> RevokeApplicationPermission(
        string groupId,
        string applicationId,
        ScopedApplicationPermissionKind kind)
    {
        var group = await _groupRepo.GetById(groupId);
        if (group == null)
        {
            return RevokePermissionResult.Failure | RevokePermissionResult.GroupNotFound;
        }
        
        var application = await _applicationRepo.GetById(applicationId);
        if (application == null)
        {
            return RevokePermissionResult.Failure | RevokePermissionResult.ApplicationNotFound;
        }

        var existingRole = await _groupPermAppRepo.GetManyBy(new()
        {
            GroupId = groupId,
            ApplicationId = applicationId,
            KindsIn = [kind]
        });
        if (existingRole.Count < 1)
        {
            return RevokePermissionResult.Failure | RevokePermissionResult.PermissionNotFound;
        }

        await _permissionService.RevokeManyApplicationForGroupAsync(group, application, kind);
        return RevokePermissionResult.Success;
    }

    [Flags]
    public enum CreateResultKind
    {
        Unknown = 0,
        Success = 1,
        NameNotProvided = 2,
        NameAlreadyExists = 4
    }
    public class CreateResult
    {
        public CreateResultKind Kind { get; set; }
        /// <remarks>
        /// Will be set when <see cref="Kind"/> is equal to <see cref="CreateResultKind.Success"/>
        /// </remarks>
        public string? GroupId { get; set; }
        public CreateResult(CreateResultKind kind)
            : this(kind, null)
        {}
        public CreateResult(CreateResultKind kind, string? groupId)
        {
            Kind = kind;
            GroupId = groupId;
        }
    }
    public Task<CreateResult> Create(AdminGroupV1CreateRequest data)
    {
        return Create(
            data.Name,
            data.InitialUserIds ?? [],
            data.InitialPermissionGrant ?? [],
            data.InitialPermissionDeny ?? [],
            data.Priority
        );
    }
    public async Task<CreateResult> Create(
        string name,
        IEnumerable<string>? initialUsers = null,
        IEnumerable<PermissionKind>? initialPermissionGrant = null,
        IEnumerable<PermissionKind>? initialPermissionDeny = null,
        uint priority = 0)
    {
        name = name.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return new(CreateResultKind.NameNotProvided);
        }

        var existing = await _groupRepo.GetMayByName(name);
        if (existing.Count > 0)
        {
            return new(CreateResultKind.NameAlreadyExists);
        }

        var session = await _mongoClient.StartSessionAsync();
        session.StartTransaction();
        try
        {
            var group = new GroupModel()
            {
                Name = name,
                Priority = priority
            };
            await _groupRepo.InsertOrUpdate(group);

            if (initialPermissionGrant != null)
            {
                await _permissionService.GrantManyGlobalForGroupAsync(group, initialPermissionGrant.ToArray());
            }
            if (initialPermissionDeny != null)
            {
                if (initialPermissionGrant != null)
                {
                    await _permissionService.DenyManyGlobalForGroupAsync(group, initialPermissionDeny.Where(v => initialPermissionGrant.Contains(v) == false).ToArray());
                }
                else
                {
                    await _permissionService.DenyManyGlobalForGroupAsync(group, initialPermissionDeny.ToArray());
                }
            }

            if (initialUsers != null)
            {
                await _groupService.AddManyUsersAsync(group, initialUsers.Distinct());
            }
            await session.CommitTransactionAsync();

            return new(CreateResultKind.Success, group.Id);
        }
        catch (Exception ex)
        {
            await session.AbortTransactionAsync();
            session.Dispose();
            _log.Error($"Failed to create group\n" + string.Join("\n", [
                $"name: {name}",
                $"initialUsers: " + string.Join(", ", initialUsers ?? []),
                $"initialPermissionGrant: " + string.Join(", ", initialPermissionGrant ?? []),
                $"initialPermissionDeny: " + string.Join(", ", initialPermissionDeny ?? []),
                $"priority: {priority}"
            ]));
            SentrySdk.CaptureException(ex, (scope) =>
            {
                scope.SetExtra($"param.{nameof(name)}", name);
                scope.SetExtra($"param.{nameof(initialUsers)}", initialUsers);
                scope.SetExtra($"param.{nameof(initialPermissionGrant)}", initialPermissionGrant);
                scope.SetExtra($"param.{nameof(initialPermissionDeny)}", initialPermissionDeny);
                scope.SetExtra($"param.{nameof(priority)}", priority);
            });
            throw;
        }
    }
}