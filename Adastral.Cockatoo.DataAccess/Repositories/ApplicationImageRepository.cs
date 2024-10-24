using System.Collections.ObjectModel;
using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Repositories;

[CockatooDependency]
public class ApplicationImageRepository : BaseRepository<ApplicationImageModel>
{
    public ApplicationImageRepository(IServiceProvider services)
        : base(ApplicationImageModel.CollectionName, services)
    {}

    public async Task<ApplicationImageModel?> GetById(string id)
    {
        var filter = Builders<ApplicationImageModel>
            .Filter
            .Where(v => v.Id == id);
        var res = await BaseFind(filter);
        return res.FirstOrDefault();
    }

    public async Task<ReadOnlyCollection<ApplicationImageModel>?> GetAllForApplication(string applicationId)
    {
        var filter = Builders<ApplicationImageModel>
            .Filter
            .Where(v => v.ApplicationId == applicationId);
        var res = await BaseFind(filter);
        return res.ToList().AsReadOnly();
    }
    public Task<ReadOnlyCollection<ApplicationImageModel>?> GetAllForApplication(ApplicationDetailModel app)
        => GetAllForApplication(app.Id);

    public async Task<ApplicationImageModel?> GetForApplication(string applicationId, ApplicationImageKind kind)
    {
        var filter = Builders<ApplicationImageModel>
            .Filter
            .Where(v => v.ApplicationId == applicationId && v.Kind == kind);
        var sort = Builders<ApplicationImageModel>
            .Sort
            .Descending(v => v.UpdatedAt);
        var res = await BaseFind(filter, sort);
        return res.FirstOrDefault();
    }

    public Task<List<ApplicationImageModel>> GetAllUsingFile(StorageFileModel file)
        => GetAllUsingFile(file.Id);
    public async Task<List<ApplicationImageModel>> GetAllUsingFile(string storageFileId)
    {
        var filter = Builders<ApplicationImageModel>
            .Filter
            .Where(v => v.IsManagedFile == true);
        filter &= Builders<ApplicationImageModel>
            .Filter
            .Where(v => v.ManagedFileId != null);
        filter &= Builders<ApplicationImageModel>
            .Filter
            .Where(v => v.ManagedFileId == storageFileId);
        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }

    public async Task<ReadOnlyCollection<ApplicationImageModel>?> GetAll()
    {
        var filter = Builders<ApplicationImageModel>
            .Filter
            .Empty;
        var res = await BaseFind(filter);
        return res.ToList().AsReadOnly();
    }

    public async Task<ApplicationImageModel> InsertOrUpdate(ApplicationImageModel model)
    {
        if (string.IsNullOrEmpty(model.ApplicationId))
        {
            throw new ArgumentException($"Property {nameof(model.ApplicationId)} is required", nameof(model));
        }
        var filter = Builders<ApplicationImageModel>
            .Filter
            .Where(v => v.Id == model.Id);
        var existsRes = await BaseFind(filter);

        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        model.SetUpdatedAt();
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