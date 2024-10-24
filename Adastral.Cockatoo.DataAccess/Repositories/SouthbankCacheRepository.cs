using System.Collections.ObjectModel;
using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Repositories;

[CockatooDependency]
public class SouthbankCacheRepository : BaseRepository<SouthbankCacheModel>
{
    public SouthbankCacheRepository(IServiceProvider services)
        : base(SouthbankCacheModel.CollectionName, services)
    {}

    public async Task<SouthbankCacheModel?> GetLatest()
    {
        var filter = Builders<SouthbankCacheModel>
            .Filter
            .Empty;
        var sort = Builders<SouthbankCacheModel>
            .Sort
            .Descending(v => v.Timestamp);
        var res = await BaseFind(filter, sort);
        return res.FirstOrDefault();
    }

    public async Task<ReadOnlyCollection<SouthbankCacheModel>?> GetAll()
    {
        var filter = Builders<SouthbankCacheModel>
            .Filter
            .Empty;
        var sort = Builders<SouthbankCacheModel>
            .Sort
            .Descending(v => v.Timestamp);
        var res = await BaseFind(filter, sort);
        return res.ToList().AsReadOnly();
    }
    /// <summary>
    /// Delete all documents from the database where it's not the latest document (<see cref="GetLatest"/>)
    /// </summary>
    public async Task<long> Clean()
    {
        var latest = await GetLatest();
        if (latest == null) return 0;

        var filter = Builders<SouthbankCacheModel>
            .Filter
            .Where(v => v.Id != latest.Id);
        var collection = GetCollection();
        var rm = await collection!.DeleteManyAsync(filter);
        return rm.DeletedCount;
    }

    public async Task<SouthbankCacheModel> InsertOrUpdate(SouthbankCacheModel model)
    {
        var filter = Builders<SouthbankCacheModel>
            .Filter
            .Where(v => v.Id == model.Id);
        var existsRes = await BaseFind(filter);

        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        if (await existsRes.AnyAsync())
        {
            await collection.ReplaceOneAsync(filter, model);
        }
        else
        {
            await collection.InsertOneAsync(model);
        }

        return model;
    }
}