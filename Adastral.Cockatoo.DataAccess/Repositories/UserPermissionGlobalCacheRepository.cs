using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Repositories;

[CockatooDependency]
public class UserPermissionGlobalCacheRepository : BaseRepository<UserPermissionGlobalCacheModel>
{
    public UserPermissionGlobalCacheRepository(IServiceProvider services)
        : base(UserPermissionGlobalCacheModel.CollectionName, services)
    {}

    public async Task<UserPermissionGlobalCacheModel?> GetById(string id)
    {
        var filter = Builders<UserPermissionGlobalCacheModel>
            .Filter
            .Where(v => v.Id == id);
        var result = await BaseFind(filter);
        return result?.FirstOrDefault();
    }
    public async Task<UserPermissionGlobalCacheModel?> GetLatestForUser(string userId)
    {
        var filter = Builders<UserPermissionGlobalCacheModel>
            .Filter
            .Where(v => v.UserId == userId);
        var sort = Builders<UserPermissionGlobalCacheModel>
            .Sort
            .Descending(v => v.UpdatedAt);
        var result = await BaseFind(filter, sort);
        return result?.FirstOrDefault();
    }
    public async Task<List<UserPermissionGlobalCacheModel>> GetAllForUser(string userId)
    {
        var filter = Builders<UserPermissionGlobalCacheModel>
            .Filter
            .Where(v => v.UserId == userId);
        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }

    public async Task<long> Delete(params string[] id)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<UserPermissionGlobalCacheModel>
            .Filter
            .In(v => v.Id, id);
        var result = await collection.DeleteManyAsync(filter);
        return result.DeletedCount;
    }

    public async Task<long> DeleteForUser(params string[] userId)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<UserPermissionGlobalCacheModel>
            .Filter
            .In(v => v.UserId, userId);
        var result = await collection.DeleteManyAsync(filter);
        return result.DeletedCount;
    }

    public async Task<UserPermissionGlobalCacheModel> InsertOrUpdate(UserPermissionGlobalCacheModel model)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");

        var propFilter = Builders<UserPermissionGlobalCacheModel>
            .Filter
            .Where(v => v.UserId == model.UserId);

        var exists = await collection.CountDocumentsAsync(propFilter);
        if (exists < 1)
        {
            model.CreatedAt = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            await collection.InsertOneAsync(model);
            return model;
        }
        else
        {
            await collection.UpdateManyAsync(propFilter, Builders<UserPermissionGlobalCacheModel>
                .Update
                .Set(v => v.Permissions, model.Permissions)
                .Set(v => v.UpdatedAt, new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())));
            var result = await GetLatestForUser(model.UserId);
            if (result == null)
            {
                model.CreatedAt = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                await collection.InsertOneAsync(model);
            }
            return result ?? model;
        }
    }
}