using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Models.AutoUpdaterDotNet;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Repositories.AutoUpdaterDotNet;

[CockatooDependency]
public class AUDNRevisionRepository : BaseRepository<AUDNRevisionModel>
{
    private readonly ApplicationDetailRepository _appDetailRepo;
    private readonly StorageFileRepository _storageFileRepo;
    public AUDNRevisionRepository(IServiceProvider services)
        : base(AUDNRevisionModel.CollectionName, services)
    {
        _appDetailRepo = services.GetRequiredService<ApplicationDetailRepository>();
        _storageFileRepo = services.GetRequiredService<StorageFileRepository>();
    }
    static AUDNRevisionRepository()
    {
        SortCreatedAtDesc = Builders<AUDNRevisionModel>
            .Sort
            .Descending(v => v.CreatedAt);
    }
    internal static SortDefinition<AUDNRevisionModel> SortCreatedAtDesc;
    public async Task<AUDNRevisionModel?> Get(string id)
    {
        var filter = Builders<AUDNRevisionModel>
            .Filter
            .Where(v => v.Id == id);
        var result = await BaseFind(filter);
        return result?.FirstOrDefault();
    }

    public async Task<AUDNRevisionModel?> GetLatestForApp(string appId, bool includeDisabled = false)
    {
        var filter = Builders<AUDNRevisionModel>
            .Filter
            .Where(v => v.ApplicationId == appId);
        if (!includeDisabled)
        {
            filter &= Builders<AUDNRevisionModel>
                .Filter
                .Where(v => v.IsEnabled);
        }
        var result = await BaseFind(filter, SortCreatedAtDesc);
        return result?.FirstOrDefault();
    }
    public async Task<List<AUDNRevisionModel>> GetAllForApp(string appId)
    {
        var filter = Builders<AUDNRevisionModel>
            .Filter
            .Where(v => v.ApplicationId == appId);
        var result = await BaseFind(filter, SortCreatedAtDesc);
        return result?.ToList() ?? [];
    }

    public Task<List<AUDNRevisionModel>> GetAllUsingFile(StorageFileModel file) => GetAllUsingFile(file.Id);
    public async Task<List<AUDNRevisionModel>> GetAllUsingFile(string fileId)
    {
        var filter = Builders<AUDNRevisionModel>
            .Filter
            .Where(v => v.StorageFileId == fileId);
        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }
    public async Task<List<AUDNRevisionModel>> GetAll()
    {
        var filter = Builders<AUDNRevisionModel>
            .Filter
            .Empty;
        var result = await BaseFind(filter, SortCreatedAtDesc);
        return result?.ToList() ?? [];
    }
    public async Task<bool> Exists(string id)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<AUDNRevisionModel>
            .Filter
            .Where(v => v.Id == id);
        var count = await collection.CountDocumentsAsync(filter);
        return count > 0;        
    }
    public async Task<long> Delete(params string[] ids)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<AUDNRevisionModel>
            .Filter
            .In(v => v.Id, ids);
        var result = await collection.DeleteManyAsync(filter);
        return result.DeletedCount;
    }

    public async Task<AUDNRevisionModel?> InsertOrUpdate(AUDNRevisionModel model)
    {
        if (string.IsNullOrEmpty(model.ApplicationId))
        {
            throw new ArgumentException($"Property {nameof(model.ApplicationId)} is required", nameof(model));
        }
        if (string.IsNullOrEmpty(model.StorageFileId))
        {
            throw new ArgumentException($"Property {nameof(model.StorageFileId)} is required", nameof(model)); 
        }
        var existingApp = await _appDetailRepo.GetById(model.ApplicationId);
        if (existingApp == null)
        {
            throw new ArgumentException($"Could not find model {nameof(ApplicationDetailModel)} with Id {model.ApplicationId} (from property {nameof(model.ApplicationId)})", nameof(model));
        }
        else if (existingApp.Type != ApplicationDetailType.AutoUpdaterDotNet)
        {
            throw new ArgumentException($"Application {model.ApplicationId} has incorrect type {existingApp.Type}, must be {ApplicationDetailType.AutoUpdaterDotNet} (property {nameof(model.ApplicationId)})", nameof(model));
        }
        if (!await _storageFileRepo.Exists(model.StorageFileId))
        {
            throw new ArgumentException($"Could not find model {nameof(StorageFileModel)} with Id {model.StorageFileId} (from property {nameof(model.StorageFileId)})", nameof(model));
        }
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<AUDNRevisionModel>
            .Filter
            .Where(v => v.Id == model.Id);
        if (await Exists(model.Id))
        {
            model.UpdatedAt = new MongoDB.Bson.BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            await collection.FindOneAndReplaceAsync(filter, model);
        }
        else
        {
            model.CreatedAt = new MongoDB.Bson.BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            await collection.InsertOneAsync(model);
        }
        return model;
    }
}