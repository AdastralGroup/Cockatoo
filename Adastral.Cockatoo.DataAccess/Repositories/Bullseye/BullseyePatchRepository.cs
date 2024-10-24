using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Repositories;

[CockatooDependency]
public class BullseyePatchRepository : BaseRepository<BullseyePatchModel>
{
    public BullseyePatchRepository(IServiceProvider services)
        : base(BullseyePatchModel.CollectionName, services)
    {}

    public async Task<BullseyePatchModel?> GetById(string id)
    {
        var filter = Builders<BullseyePatchModel>
            .Filter
            .Where(v => v.Id == id);
        var result = await BaseFind(filter);
        return result?.FirstOrDefault();
    }
    public async Task<List<BullseyePatchModel>> GetAllRevisionFrom(string fromRevisionId)
    {
        var filter = Builders<BullseyePatchModel>
            .Filter
            .Where(v => v.FromRevisionId == fromRevisionId);
        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }
    public async Task<List<BullseyePatchModel>> GetAllRevisionTo(string toRevisionId)
    {
        var filter = Builders<BullseyePatchModel>
            .Filter
            .Where(v => v.ToRevisionId == toRevisionId);
        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }
    public async Task<List<BullseyePatchModel>> GetAllWithRevision(string revisionId)
    {
        var filter = Builders<BullseyePatchModel>
            .Filter
            .Where(v => v.ToRevisionId == revisionId);
        filter |= Builders<BullseyePatchModel>
            .Filter
            .Where(v => v.FromRevisionId == revisionId);
        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }
    public Task<List<BullseyePatchModel>> GetAllUsingFile(StorageFileModel file)
        => GetAllUsingFile(file.Id);
    public async Task<List<BullseyePatchModel>> GetAllUsingFile(string storageFileId)
    {
        var filter = Builders<BullseyePatchModel>
            .Filter
            .Where(v => v.PeerToPeerStorageFileId == storageFileId);
        filter |= Builders<BullseyePatchModel>
            .Filter
            .Where(v => v.StorageFileId == storageFileId);

        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }
    public async Task<List<BullseyePatchModel>> GetAll()
    {
        var filter = Builders<BullseyePatchModel>
            .Filter
            .Empty;
        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }

    public Task<long> Delete(params BullseyePatchModel[] models)
        => Delete(models.Select(v => v.Id).Distinct().ToArray());
    public async Task<long> Delete(params string[] ids)
    {
        var collection = GetCollection();
        if (collection == null)
            return 0;
        var filter = Builders<BullseyePatchModel>
            .Filter
            .In(v => v.Id, ids);
        var result = await collection.DeleteManyAsync(filter);
        return result.DeletedCount;
    }

    public async Task<BullseyePatchModel> InsertOrUpdate(BullseyePatchModel model)
    {
        if (string.IsNullOrEmpty(model.FromRevisionId))
        {
            throw new ArgumentException($"Property {nameof(model.FromRevisionId)} is required", nameof(model));
        }
        if (string.IsNullOrEmpty(model.ToRevisionId))
        {
            throw new ArgumentException($"Property {nameof(model.ToRevisionId)} is required", nameof(model));
        }
        if (string.IsNullOrEmpty(model.StorageFileId))
        {
            throw new ArgumentException($"Property {nameof(model.StorageFileId)} is required", nameof(model));
        }
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        
        var idFilter = Builders<BullseyePatchModel>
            .Filter
            .Where(v => v.Id == model.Id);
        
        var exists = await collection.CountDocumentsAsync(idFilter);
        if (exists > 0)
        {
            await collection.ReplaceOneAsync(idFilter, model);
        }
        else
        {
            model.CreatedAt = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            await collection.InsertOneAsync(model);
        }
        return model;
    }
}