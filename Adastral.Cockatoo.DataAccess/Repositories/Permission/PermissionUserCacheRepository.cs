using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Repositories;

[CockatooDependency]
public class PermissionUserCacheRepository : BaseRepository<PermissionUserCacheModel>
{
    public PermissionUserCacheRepository(IServiceProvider services)
        : base(PermissionUserCacheModel.CollectionName, services)
    {}

    private SortDefinition<PermissionUserCacheModel> DescByTimestamp
        => Builders<PermissionUserCacheModel>
            .Sort
            .Descending(v => v.Timestamp);
    
    public async Task<PermissionUserCacheModel?> GetById(string id)
    {
        var filter = Builders<PermissionUserCacheModel>
            .Filter
            .Where(v => v.Id == id);
        var result = await BaseFind(filter);
        return result?.FirstOrDefault();
    }

    public async Task<PermissionUserCacheModel?> GetLatestForUser(string userId)
    {
        var filter = Builders<PermissionUserCacheModel>
            .Filter
            .Where(v => v.UserId == userId);
        var result = await BaseFind(filter, DescByTimestamp);
        return result?.FirstOrDefault();
    }

    public async Task<long> Delete(params string[] ids)
    {
        var collection = GetCollection();
        if (collection == null)
            return 0;
        var filter = Builders<PermissionUserCacheModel>
            .Filter
            .In(v => v.Id, ids);
        var result = await collection.DeleteManyAsync(filter);
        return result.DeletedCount;
    }

    public async Task InsertOrUpdate(PermissionUserCacheModel model)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<PermissionUserCacheModel>
            .Filter
            .Where(v => v.Id == model.Id);
        var existsResult = await BaseFind(filter);
        if (existsResult?.Any() ?? false)
        {
            await collection.FindOneAndReplaceAsync(filter, model);
        }
        else
        {
            await collection.InsertOneAsync(model);
        }
    }
}