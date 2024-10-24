using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Repositories;

[CockatooDependency]
public class GroupUserAssociationRepository : BaseRepository<GroupUserAssociationModel>
{
    public GroupUserAssociationRepository(IServiceProvider services)
        : base(GroupUserAssociationModel.CollectionName, services)
    {

    }

    public async Task<GroupUserAssociationModel?> GetById(string associationId, bool includeDeleted = false)
    {
        var filter = Builders<GroupUserAssociationModel>
            .Filter
            .Where(v => v.Id == associationId);
        if (!includeDeleted)
        {
            filter &= Builders<GroupUserAssociationModel>
                .Filter
                .Where(v => v.IsDeleted == false);
        }
        var result = await BaseFind(filter);
        return result?.FirstOrDefault();
    }

    public async Task<List<GroupUserAssociationModel>> GetAllForGroup(string groupId, bool includeDeleted = false)
    {
        var filter = Builders<GroupUserAssociationModel>
            .Filter
            .Where(v => v.GroupId == groupId);
        if (!includeDeleted)
        {
            filter &= Builders<GroupUserAssociationModel>
                .Filter
                .Where(v => v.IsDeleted == false);
        }
        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }
    public async Task<List<GroupUserAssociationModel>> GetAllForUser(string userId, bool includeDeleted = false)
    {
        var filter = Builders<GroupUserAssociationModel>
            .Filter
            .Where(v => v.UserId == userId);
        if (!includeDeleted)
        {
            filter &= Builders<GroupUserAssociationModel>
                .Filter
                .Where(v => v.IsDeleted == false);
        }
        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }
    public async Task<List<GroupUserAssociationModel>> GetAllWithGroupAndUser(GroupModel group, UserModel user, bool includeDeleted = false)
    {
        var filter = Builders<GroupUserAssociationModel>
            .Filter
            .Where(v => v.UserId == user.Id && v.GroupId == group.Id);
        if (!includeDeleted)
        {
            filter &= Builders<GroupUserAssociationModel>
                .Filter
                .Where(v => v.IsDeleted == false);
        }
        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }
    public async Task<List<GroupUserAssociationModel>> GetAllWithUserAndGroup(string userId, string groupId, bool includeDeleted = false)
    {
        var filter = Builders<GroupUserAssociationModel>
            .Filter
            .Where(v => v.UserId == userId && v.GroupId == groupId);
        if (!includeDeleted)
        {
            filter &= Builders<GroupUserAssociationModel>
                .Filter
                .Where(v => v.IsDeleted == false);
        }
        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }
    public Task<UpdateResult> SoftDeleteById(params string[] ids)
    {
        return SetDeleteState(true, ids);
    }
    public async Task<UpdateResult> SetDeleteState(bool state, params string[] ids)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<GroupUserAssociationModel>
            .Filter
            .In(v => v.Id, ids);
        var req = Builders<GroupUserAssociationModel>
            .Update
            .Set(v => v.IsDeleted, state);
        var result = await collection.UpdateManyAsync(filter, req);
        return result;
    }
    public async Task<long> Delete(params string[] ids)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<GroupUserAssociationModel>
            .Filter
            .In(v => v.Id, ids);
        var result = await collection.DeleteManyAsync(filter);
        return result.DeletedCount;
    }
    public async Task<List<GroupUserAssociationModel>> HardDeleteByGroupId(params string[] groupIds)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<GroupUserAssociationModel>
            .Filter
            .In(v => v.GroupId, groupIds);
        var result = await BaseFind(filter);
        await collection.DeleteManyAsync(filter);
        return result?.ToList() ?? [];
    }
    public async Task<bool> ExistsById(string id)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<GroupUserAssociationModel>
            .Filter
            .Where(v => v.Id == id);
        var result = await collection.CountDocumentsAsync(filter);
        return result > 0;
    }
    public Task<bool> ExistsByGroupAndUser(GroupModel group, UserModel user, bool includeDeleted = false)
        => ExistsByGroupAndUser(group.Id, user.Id, includeDeleted);
    public async Task<bool> ExistsByGroupAndUser(string groupId, string userId, bool includeDeleted = false)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<GroupUserAssociationModel>
            .Filter
            .Where(v => v.UserId == userId && v.GroupId == groupId);
        if (includeDeleted == false)
        {
            filter &= Builders<GroupUserAssociationModel>
                .Filter
                .Where(v => v.IsDeleted == false);
        }
        var result = await collection.CountDocumentsAsync(filter);
        return result > 0;
    }
    public async Task<GroupUserAssociationModel> InsertOrUpdate(GroupUserAssociationModel model)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        if (await ExistsById(model.Id))
        {
            var filter = Builders<GroupUserAssociationModel>
                .Filter
                .Where(v => v.Id == model.Id);
            var result = await collection.UpdateManyAsync(filter, model.CreateUpdateDefinition());
        }
        else
        {
            model.CreatedAt = new(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            await collection.InsertOneAsync(model);
        }
        return model;
    }
}