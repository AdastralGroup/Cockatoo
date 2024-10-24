using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;

namespace Adastral.Cockatoo.Services;

[CockatooDependency]
public class UserService : BaseService
{
    private readonly UserRepository _userRepository;
    private readonly ServiceAccountRepository _serviceAccountRepository;
    private readonly ServiceAccountTokenRepository _serviceAccountTokenRepo;
    private readonly PermissionService _permissionService;

    public UserService(IServiceProvider services)
        : base(services)
    {
        _userRepository = services.GetRequiredService<UserRepository>();
        _serviceAccountRepository = services.GetRequiredService<ServiceAccountRepository>();
        _serviceAccountTokenRepo = services.GetRequiredService<ServiceAccountTokenRepository>();
        _permissionService = services.GetRequiredService<PermissionService>();
    }

    public async Task<UserModel> CreateServiceAccount(UserModel owner, string? name = null)
    {
        if (owner.IsServiceAccount)
        {
            throw new ArgumentException($"Service Accounts cannot create Service Accounts");
        }
        var userModel = new UserModel()
        {
            DisplayName = name ?? "Service Account",
            Email = null,
            IsServiceAccount = true
        };
        userModel.SetCreatedAtTimestamp();
        var saModel = new ServiceAccountModel()
        {
            OwnerUserId = owner.Id,
            UserId = userModel.Id
        };
        await _userRepository.InsertOrUpdate(userModel);
        await _serviceAccountRepository.InsertOrUpdate(saModel);
        return userModel;
    }

    public async Task<ServiceAccountTokenModel> CreateToken(UserModel target, long? expiresAt)
    {
        if (target.IsServiceAccount == false)
        {
            throw new ArgumentException($"Provided user is not a Service Account", nameof(target));
        }
        var model = new ServiceAccountTokenModel()
        {
            ServiceAccountId = target.Id,
            ExpiresAtTimestamp = expiresAt == null ? null : new BsonTimestamp((long)expiresAt)
        };
        return await _serviceAccountTokenRepo.InsertOrUpdate(model);
    }

    public async Task<CanUserCreateTokenKind> CanCreateTokenFor(UserModel requestingUser, UserModel targetUser)
    {
        if (targetUser.IsServiceAccount == false)
        {
            return CanUserCreateTokenKind.TargetUserIsNotServiceAccount;
        }
        if (requestingUser.IsServiceAccount)
        {
            return CanUserCreateTokenKind.RequestingUserIsServiceAccount;
        }
        var serviceAccountModel = await _serviceAccountRepository.GetById(targetUser.Id);
        if (serviceAccountModel == null)
        {
            return CanUserCreateTokenKind.TargetUserIsNotServiceAccount;
        }
        if (serviceAccountModel.OwnerUserId != requestingUser.Id)
        {
            var check = await _permissionService.CheckGlobalPermission(
                requestingUser,
                PermissionService.PermissionFilterType.Any, 
                PermissionKind.ServiceAccountAdmin);
            if (check == false)
            {
                return CanUserCreateTokenKind.RequestingUserIsNotOwner;
            }
        }
        return CanUserCreateTokenKind.Yes;
    }
}