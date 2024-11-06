using System.Collections.ObjectModel;
using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Repositories;

[CockatooDependency]
public class ApplicationColorRepository : BaseRepository<ApplicationColorModel>
{
    public ApplicationColorRepository(IServiceProvider services)
        : base(ApplicationColorModel.CollectionName, services)
    {}

    public Task<ReadOnlyCollection<ApplicationColorModel>?> GetAllForApplication(ApplicationDetailModel model)
        => GetAllForApplication(model.Id);
    public async Task<ReadOnlyCollection<ApplicationColorModel>?> GetAllForApplication(string appId)
    {
        var filter = Builders<ApplicationColorModel>
            .Filter
            .Where(v => v.ApplicationId == appId);
        var sort = Builders<ApplicationColorModel>
            .Sort
            .Descending(v => v.UpdatedAtTimestamp);
        var res = await BaseFind(filter, sort);
        return res.ToList().AsReadOnly();
    }

    public async Task<ApplicationColorModel?> GetForApplication(string id, ApplicationColorKind kind)
    {
        var filter = Builders<ApplicationColorModel>
            .Filter
            .Where(v => v.ApplicationId == id && v.Kind == kind);
        var res = await BaseFind(filter);
        return res.FirstOrDefault();
    }

    public async Task<ApplicationColorModel?> Get(string id)
    {
        var filter = Builders<ApplicationColorModel>
            .Filter
            .Where(v => v.Id == id);
        var res = await BaseFind(filter);
        return res.FirstOrDefault();
    }

    public async Task<long> DeleteById(params string[] ids)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<ApplicationColorModel>
            .Filter
            .In(v => v.Id, ids);
        var result = await collection.DeleteManyAsync(filter);
        return result.DeletedCount;
    }

    public async Task<ApplicationColorModel> InsertOrUpdate(ApplicationColorModel model)
    {
        if (string.IsNullOrEmpty(model.ApplicationId))
        {
            throw new ArgumentException($"Property {nameof(model.ApplicationId)} is required", nameof(model));
        }
        var collection = GetCollection();
        if (collection == null)
        {
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        }
        var filter = Builders<ApplicationColorModel>
            .Filter
            .Where(v => v.Id == model.Id);
        var existsRes = await BaseFind(filter);
        model.SetUpdatedAtTimestamp();
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