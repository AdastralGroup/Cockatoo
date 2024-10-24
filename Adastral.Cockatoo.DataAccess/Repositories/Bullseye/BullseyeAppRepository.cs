using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Repositories;

[CockatooDependency]
public class BullseyeAppRepository : BaseRepository<BullseyeAppModel>
{
    public BullseyeAppRepository(IServiceProvider services)
        : base(BullseyeAppModel.CollectionName, services)
    {}

    public async Task<BullseyeAppModel?> GetById(string id)
    {
        var filter = Builders<BullseyeAppModel>
            .Filter
            .Where(v => v.ApplicationDetailModelId == id);
        var result = await BaseFind(filter);
        return result?.FirstOrDefault();
    }

    public Task<long> Delete(params BullseyeAppModel[] models)
        => Delete(models.Select(v => v.Id).ToArray());
    public async Task<long> Delete(params string[] ids)
    {
        var collection = GetCollection();
        if (collection == null)
            return 0;
        var filter = Builders<BullseyeAppModel>
            .Filter
            .In(v => v.ApplicationDetailModelId, ids);
        var result = await collection.DeleteManyAsync(filter);
        return result.DeletedCount;
    }

    public Task<bool> Exists(BullseyeAppModel model)
        => Exists(model.Id);
    public async Task<bool> Exists(string id)
    {
        var collection = GetCollection();
        if (collection == null)
            return false;
        var filter = Builders<BullseyeAppModel>
            .Filter
            .Where(v => v.ApplicationDetailModelId == id);
        var result = await collection.CountDocumentsAsync(filter);
        return result > 0;
    }

    public async Task<BullseyeAppModel> InsertOrUpdate(BullseyeAppModel model)
    {
        if (string.IsNullOrEmpty(model.ApplicationDetailModelId))
        {
            throw new ArgumentException($"Property {nameof(model.ApplicationDetailModelId)} is required", nameof(model));
        }
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");

        var filter = Builders<BullseyeAppModel>
            .Filter
            .Where(v => v.ApplicationDetailModelId == model.Id);

        var exists = await Exists(model);
        if (exists)
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