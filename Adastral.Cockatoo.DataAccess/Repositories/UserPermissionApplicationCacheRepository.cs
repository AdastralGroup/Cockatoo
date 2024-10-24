using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Repositories;

[CockatooDependency]
public class UserPermissionApplicationCacheRepository : BaseRepository<UserPermissionApplicationCacheModel>
{
    public UserPermissionApplicationCacheRepository(IServiceProvider services)
        : base(UserPermissionApplicationCacheModel.CollectionName, services)
    {}

    public async Task<UserPermissionApplicationCacheModel?> GetById(string id)
    {
        var filter = Builders<UserPermissionApplicationCacheModel>
            .Filter
            .Where(v => v.Id == id);
        var result = await BaseFind(filter);
        return result?.FirstOrDefault();
    }
    public async Task<UserPermissionApplicationCacheModel?> GetLatestForUser(string userId)
    {
        var filter = Builders<UserPermissionApplicationCacheModel>
            .Filter
            .Where(v => v.UserId == userId);
        var sort = Builders<UserPermissionApplicationCacheModel>
            .Sort
            .Descending(v => v.UpdatedAt);
        var result = await BaseFind(filter, sort);
        return result?.FirstOrDefault();
    }
    public async Task<List<UserPermissionApplicationCacheModel>> GetAllForUser(string userId)
    {
        var filter = Builders<UserPermissionApplicationCacheModel>
            .Filter
            .Where(v => v.UserId == userId);
        var sort = Builders<UserPermissionApplicationCacheModel>
            .Sort
            .Descending(v => v.UpdatedAt);
        var result = await BaseFind(filter, sort);
        return result?.ToList() ?? [];
    }
    public async Task<List<UserPermissionApplicationCacheModel>> GetAllForApplication(string applicationId)
    {
        var filter = Builders<UserPermissionApplicationCacheModel>
            .Filter
            .Where(v => v.ApplicationId == applicationId);
        var sort = Builders<UserPermissionApplicationCacheModel>
            .Sort
            .Descending(v => v.UpdatedAt);
        var result = await BaseFind(filter, sort);
        return result?.ToList() ?? [];
    }

    public async Task<UserPermissionApplicationCacheModel?> GetLatestForUserAndApp(string userId,
        string applicationId)
    {
        var filter = Builders<UserPermissionApplicationCacheModel>
            .Filter
            .Where(v => v.UserId == userId && v.ApplicationId == applicationId);
        var sort = Builders<UserPermissionApplicationCacheModel>
            .Sort
            .Descending(v => v.UpdatedAt);
        var result = await BaseFind(filter, sort);
        return result?.FirstOrDefault();
    }

    public async Task<long> Delete(params string[] id)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<UserPermissionApplicationCacheModel>
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
        var filter = Builders<UserPermissionApplicationCacheModel>
            .Filter
            .In(v => v.UserId, userId);
        var result = await collection.DeleteManyAsync(filter);
        return result.DeletedCount;
    }
    public async Task<long> DeleteForApplication(params string[] applicationId)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<UserPermissionApplicationCacheModel>
            .Filter
            .In(v => v.ApplicationId, applicationId);
        var result = await collection.DeleteManyAsync(filter);
        return result.DeletedCount;
    }

    public async Task<UserPermissionApplicationCacheModel> InsertOrUpdate(UserPermissionApplicationCacheModel model)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");

        var propFilter = Builders<UserPermissionApplicationCacheModel>
            .Filter
            .Where(v => v.UserId == model.UserId && v.ApplicationId == model.ApplicationId);

        var exists = await collection.CountDocumentsAsync(propFilter);
        if (exists < 1)
        {
            model.CreatedAt = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            await collection.InsertOneAsync(model);
            return model;
        }
        else
        {
            await collection.UpdateManyAsync(propFilter, Builders<UserPermissionApplicationCacheModel>
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