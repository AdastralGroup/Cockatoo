using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Helpers;
using Adastral.Cockatoo.DataAccess.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Repositories;

[CockatooDependency]
public class BullseyeAppRevisionRepository : BaseRepository<BullseyeAppRevisionModel>
{
    public BullseyeAppRevisionRepository(IServiceProvider services)
        : base(BullseyeAppRevisionModel.CollectionName, services)
    {}

    public async Task<BullseyeAppRevisionModel?> GetById(string id)
    {
        var filter = Builders<BullseyeAppRevisionModel>
            .Filter
            .Where(v => v.Id == id);
        var result = await BaseFind(filter);
        return result?.FirstOrDefault();
    }
    public async Task<BullseyeAppRevisionModel?> GetByTagForApp(string appId, string? tag)
    {
        if (string.IsNullOrEmpty(tag))
        {
            return null;
        }
        var filter = Builders<BullseyeAppRevisionModel>
            .Filter
            .Where(v => v.BullseyeAppId == appId);
        filter &= Builders<BullseyeAppRevisionModel>
            .Filter
            .Where(v => v.Tag != null);
        filter &= Builders<BullseyeAppRevisionModel>
            .Filter
            .Where(v => v.Tag == tag);
        var result = await BaseFind(filter);
        return result?.FirstOrDefault();
    }
    public async Task<List<BullseyeAppRevisionModel>> GetAllForApp(string appId, bool? includeLive = null)
    {
        var filter = Builders<BullseyeAppRevisionModel>
            .Filter
            .Where(v => v.BullseyeAppId == appId);
        if (includeLive != null)
        {
            filter &= MongoHelpers.PublishDelayFilter<BullseyeAppRevisionModel>((bool)includeLive);
        }
        var results = await BaseFind(filter);
        return results?.ToList() ?? [];
    }
    public async Task<List<BullseyeAppRevisionModel>> GetAll()
    {
        var filter = Builders<BullseyeAppRevisionModel>
            .Filter
            .Empty;
        var results = await BaseFind(filter);
        return results?.ToList() ?? [];
    }

    public async Task<List<BullseyeAppRevisionModel>> GetAllForAppWithVersion(string appId, uint version)
    {
        var versionStr = version.ToString();
        var filter = Builders<BullseyeAppRevisionModel>
            .Filter
            .Where(v => v.BullseyeAppId == appId && v.Version == versionStr);
        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }

    public Task<List<BullseyeAppRevisionModel>> GetAllUsingFile(StorageFileModel file)
        => GetAllUsingFile(file.Id);
    public async Task<List<BullseyeAppRevisionModel>> GetAllUsingFile(string storageFileId)
    {
        var filter = Builders<BullseyeAppRevisionModel>
            .Filter
            .Where(v => v.ArchiveStorageFileId == storageFileId);
        filter |= Builders<BullseyeAppRevisionModel>
            .Filter
            .Where(v => v.PeerToPeerStorageFileId == storageFileId);
        filter |= Builders<BullseyeAppRevisionModel>
            .Filter
            .Where(v => v.SignatureStorageFileId == storageFileId);

        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }

    public async Task<bool> ExistsById(string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;
        var filter = Builders<BullseyeAppRevisionModel>
            .Filter
            .Where(v => v.Id == id);
        var result = await BaseFind(filter);
        return result?.Any() ?? false;
    }

    public async Task<long> Delete(params string[] ids)
    {
        var collection = GetCollection();
        if (collection == null)
            return 0;
        var filter = Builders<BullseyeAppRevisionModel>
            .Filter
            .In(v => v.Id, ids);
        var result = await collection.DeleteManyAsync(filter);
        return result.DeletedCount;
    }

    public async Task<BullseyeAppRevisionModel> InsertOrUpdate(BullseyeAppRevisionModel model)
    {
        if (string.IsNullOrEmpty(model.BullseyeAppId))
        {
            throw new ArgumentException($"Property {nameof(model.BullseyeAppId)} is required", nameof(model));
        }
        if (string.IsNullOrEmpty(model.ArchiveStorageFileId))
        {
            throw new ArgumentException($"Property {nameof(model.ArchiveStorageFileId)} is required", nameof(model));
        }
        if (string.IsNullOrEmpty(model.Tag) == false)
        {
            var existingWithTag = await GetByTagForApp(model.BullseyeAppId, model.Tag);
            if (existingWithTag != null)
            {
                throw new ArgumentException($"A Revision for this app with the tag {model.Tag} already exists (revision: {existingWithTag.Id}, app: {model.BullseyeAppId})", nameof(model));
            }
        }
        else
        {
            model.Tag = null;
        }
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<BullseyeAppRevisionModel>
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
        return model;
    }
}