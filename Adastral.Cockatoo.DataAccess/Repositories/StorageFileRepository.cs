using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Repositories;

[CockatooDependency]
public class StorageFileRepository : BaseRepository<StorageFileModel>
{
    public StorageFileRepository(IServiceProvider services)
        : base(StorageFileModel.CollectionName, services)
    {}

    public async Task<StorageFileModel?> GetById(string id)
    {
        var filter = Builders<StorageFileModel>
            .Filter
            .Where(v => v.Id == id);
        var res = await BaseFind(filter);
        return res.FirstOrDefault();
    }
    public Task<StorageFileModel?> Get(StorageFileModel model) => GetById(model.Id);

    public Task<bool> Exists(StorageFileModel file)
        => Exists(file.Id);
    public async Task<bool> Exists(string id)
    {
        var filter = Builders<StorageFileModel>
            .Filter
            .Where(v => v.Id == id);
        var result = await BaseFind(filter);
        return result?.Any() ?? false;
    }

    public async Task<long> Delete(string id)
    {
        var filter = Builders<StorageFileModel>
            .Filter
            .Where(v => v.Id == id);
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");

        var res = await collection.DeleteManyAsync(filter);
        return res.DeletedCount;
    }

    public Task<long> Delete(StorageFileModel model) => Delete(model.Id);

    public async Task<StorageFileModel> InsertOrUpdate(StorageFileModel model)
    {
        if (string.IsNullOrEmpty(model.Location))
        {
            throw new ArgumentException($"Property {nameof(model.Location)} is required in", nameof(model));
        }
        var filter = Builders<StorageFileModel>
            .Filter
            .Where(v => v.Id == model.Id);
        var existsRes = await BaseFind(filter);

        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
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