using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Repositories;

[CockatooDependency]
public class BlogTagRepository : BaseRepository<BlogTagModel>
{
    public BlogTagRepository(IServiceProvider services)
        : base(BlogTagModel.CollectionName, services)
    { }

    public async Task<BlogTagModel?> GetById(string id)
    {
        var filter = Builders<BlogTagModel>
            .Filter
            .Where(v => v.Id == id);
        var result = await BaseFind(filter);
        return result?.FirstOrDefault();
    }

    public async Task<List<BlogTagModel>> GetManyById(params string[] ids)
    {
        var filter = Builders<BlogTagModel>
            .Filter
            .In(v => v.Id, ids);
        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }

    public async Task<bool> Exists(string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;
        var filter = Builders<BlogTagModel>
            .Filter
            .Where(v => v.Id == id);
        var result = await BaseFind(filter);
        return result?.Any() ?? false;
    }

    public async Task<long> Delete(params string[] ids)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<BlogTagModel>
            .Filter
            .In(v => v.Id, ids);
        var result = await collection.DeleteManyAsync(filter);
        return result.DeletedCount;
    }

    public async Task<BlogTagModel> InsertOrUpdate(BlogTagModel model)
    {
        var collection = GetCollection();
        if (collection == null)
        {
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        }
        var filter = Builders<BlogTagModel>
            .Filter
            .Where(v => v.Id == model.Id);
        var idExistsResult = await BaseFind(filter);
        if (idExistsResult.Any())
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