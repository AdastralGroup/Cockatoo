using System.Collections.ObjectModel;
using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Repositories;

[CockatooDependency]
public class ApplicationDetailRepository : BaseRepository<ApplicationDetailModel>
{
    public ApplicationDetailRepository(IServiceProvider services)
        : base(ApplicationDetailModel.CollectionName, services)
    {}

    public async Task<ApplicationDetailModel?> GetById(string id)
    {
        var filter = Builders<ApplicationDetailModel>
            .Filter
            .Where(v => v.Id == id);
        var result = await BaseFind(filter);
        return result.FirstOrDefault();
    }

    public async Task<List<ApplicationDetailModel>> GetManyById(params string[] ids)
    {
        var filter = Builders<ApplicationDetailModel>
            .Filter
            .In(v => v.Id, ids);
        var result = await BaseFind(filter);
        return result.ToList() ?? [];
    }

    public async Task<bool> ExistsById(string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;
        var filter = Builders<ApplicationDetailModel>
            .Filter
            .Where(v => v.Id == id);
        var result = await BaseFind(filter);
        return result?.Any() ?? false;
    }

    public async Task DeleteById(string id)
    {
        var filter = Builders<ApplicationDetailModel>
            .Filter
            .Where(v => v.Id == id);
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        await collection.DeleteManyAsync(filter);
    }

    public async Task<ReadOnlyCollection<ApplicationDetailModel>> GetAll(bool includePrivate = true)
    {
        var filter = Builders<ApplicationDetailModel>
            .Filter
            .Empty;
        if (includePrivate == false)
        {
            filter &= Builders<ApplicationDetailModel>
                .Filter
                .Where(v => v.IsPrivate == false && v.IsHidden == false);
        }

        var result = await BaseFind(filter);
        return result.ToList().AsReadOnly();
    }

    /// <summary>
    /// Insert or Update the <paramref name="model"/> provided.
    /// </summary>
    /// <exception cref="NoNullAllowedException">Thrown when <see cref="BaseRepository{UserModel}.GetCollection()"/> returns <see langword="null"/></exception>
    public async Task<ApplicationDetailModel> InsertOrUpdate(ApplicationDetailModel model)
    {
        var filter = Builders<ApplicationDetailModel>
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
