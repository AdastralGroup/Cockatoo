using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Repositories.Group;

[CockatooDependency]
public class GroupRepository : BaseRepository<GroupModel>
{
    public GroupRepository(IServiceProvider services)
        : base(GroupModel.CollectionName, services)
    { }

    public async Task<GroupModel?> GetById(string id)
    {
        id = id.ToLower().Trim();
        var filter = Builders<GroupModel>
            .Filter
            .Where(v => v.Id == id);
        var result = await BaseFind(filter);
        return result?.FirstOrDefault();
    }

    public async Task<List<GroupModel>> GetAll()
    {
        var filter = Builders<GroupModel>.Filter.Empty;
        var result = await BaseFind(filter);
        return result.ToList();
    }

    public async Task<long> GetAllCount()
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<GroupModel>.Filter.Empty;
        var count = await collection.CountDocumentsAsync(filter);
        return count;
    }

    public async Task<List<GroupModel>> GetManyById(params string[] ids)
    {
        var filter = Builders<GroupModel>
            .Filter
            .In(v => v.Id, ids);
        var sort = Builders<GroupModel>
            .Sort
            .Descending(v => v.Priority);
        var result = await BaseFind(filter, sort);
        return result?.ToList() ?? [];
    }

    public async Task<List<GroupModel>> GetMayByName(params string[] names)
    {
        var filter = Builders<GroupModel>
            .Filter
            .In(v => v.Name, names);
        var sort = Builders<GroupModel>
            .Sort
            .Descending(v => v.Priority);
        var result = await BaseFind(filter, sort);
        return result?.ToList() ?? [];
    }

    public async Task<long> Delete(params string[] ids)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<GroupModel>
            .Filter
            .In(v => v.Id, ids.Select(v => v.Trim().ToLower()));
        var result = await collection.DeleteManyAsync(filter);
        return result.DeletedCount;
    }

    public async Task<bool> Exists(string id)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        id = id.ToLower().Trim();
        var filter = Builders<GroupModel>
            .Filter
            .Where(v => v.Id == id);
        var result = await collection.CountDocumentsAsync(filter);
        return result > 0;
    }

    public async Task InsertOrUpdate(GroupModel model)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");

        var filter = Builders<GroupModel>
            .Filter
            .Where(v => v.Id == model.Id);
        var existsResult = await collection.CountDocumentsAsync(filter);
        if (existsResult <= 0)
        {
            await collection.InsertOneAsync(model);
        }
        else
        {
            await collection.ReplaceOneAsync(filter, model);
        }
    }
}