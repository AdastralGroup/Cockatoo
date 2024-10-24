using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Repositories;

[CockatooDependency]
public class BullseyeV1CacheRepository : BaseRepository<BullseyeV1CacheModel>
{
    public BullseyeV1CacheRepository(IServiceProvider services)
        : base(BullseyeV1CacheModel.CollectionName, services)
    {}

    public async Task<BullseyeV1CacheModel?> GetById(string id)
    {
        var filter = Builders<BullseyeV1CacheModel>
            .Filter
            .Where(v => v.Id == id);
        var result = await BaseFind(filter);
        return result?.FirstOrDefault();
    }
    public async Task<BullseyeV1CacheModel?> GetForApp(string appId, bool? includeLive = null)
    {
        var filter = Builders<BullseyeV1CacheModel>
            .Filter
            .Where(v => v.TargetAppId == appId);
        if (includeLive != null)
        {
            filter &= Builders<BullseyeV1CacheModel>
                .Filter
                .Where(v => v.IsLive == includeLive);
        }
        var sort = Builders<BullseyeV1CacheModel>
            .Sort
            .Descending(v => v.CreatedAt);
        var result = await BaseFind(filter, sort);
        return result.FirstOrDefault();
    }
    public async Task<List<BullseyeV1CacheModel>> GetAllForApp(string appId, bool? includeLive = null)
    {
        var filter = Builders<BullseyeV1CacheModel>
            .Filter
            .Where(v => v.TargetAppId == appId);
        if (includeLive != null)
        {
            filter &= Builders<BullseyeV1CacheModel>
                .Filter
                .Where(v => v.IsLive == includeLive);
        }
        var sort = Builders<BullseyeV1CacheModel>
            .Sort
            .Descending(v => v.CreatedAt);
        var result = await BaseFind(filter, sort);
        return result?.ToList() ?? [];
    }
    public async Task<long> DeleteForAppId(params string[] appIds)
    {
        var collection = GetCollection();
        if (collection == null)
            return 0;
        var filter = Builders<BullseyeV1CacheModel>
            .Filter
            .In(v => v.TargetAppId, appIds);
        var result = await collection.DeleteManyAsync(filter);
        return result.DeletedCount;
    }
    public async Task<long> Delete(params string[] ids)
    {
        var collection = GetCollection();
        if (collection == null)
            return 0;
        var filter = Builders<BullseyeV1CacheModel>
            .Filter
            .In(v => v.Id, ids);
        var result = await collection.DeleteManyAsync(filter);
        return result.DeletedCount;
    }

    public async Task InsertOrUpdate(BullseyeV1CacheModel model)
    {
        if (string.IsNullOrEmpty(model.TargetAppId))
        {
            throw new ArgumentException($"Property {nameof(model.TargetAppId)} is required", nameof(model));
        }
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<BullseyeV1CacheModel>
            .Filter
            .Where(v => v.Id == model.Id);
        var exists = await collection.CountDocumentsAsync(filter);
        if (exists > 0)
        {
            await collection.ReplaceOneAsync(filter, model);
        }
        else
        {
            model.CreatedAt = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            await collection.InsertOneAsync(model);
        }
    }
}