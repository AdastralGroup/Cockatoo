using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using MongoDB.Driver;
using NLog;

namespace Adastral.Cockatoo.DataAccess.Repositories.Group;

[CockatooDependency]
public class GroupPermissionGlobalRepository : BaseRepository<GroupPermissionGlobalModel>
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    public GroupPermissionGlobalRepository(IServiceProvider services)
        : base(GroupPermissionGlobalModel.CollectionName, services)
    {}

    public async Task<GroupPermissionGlobalModel?> GetById(string id)
    {
        var filter = Builders<GroupPermissionGlobalModel>
            .Filter
            .Where(v => v.Id == id);
        var result = await BaseFind(filter);
        return result?.FirstOrDefault();
    }
    public async Task<List<GroupPermissionGlobalModel>> GetManyBy(
        string? id = null,
        string? groupId = null,
        PermissionKind[]? kinds = null,
        bool? allow = null)
    {
        var filter = Builders<GroupPermissionGlobalModel>.Filter.Empty;
        if (!string.IsNullOrEmpty(id))
        {
            filter &= Builders<GroupPermissionGlobalModel>.Filter.Where(v => v.Id == id);
        }
        if (!string.IsNullOrEmpty(groupId))
        {
            filter &= Builders<GroupPermissionGlobalModel>.Filter.Where(v => v.GroupId == groupId);
        }
        if (kinds?.Length >= 1)
        {
            filter &= Builders<GroupPermissionGlobalModel>.Filter.In(v => v.Kind, kinds);
        }
        if (allow != null)
        {
            filter &= Builders<GroupPermissionGlobalModel>.Filter.Where(v => v.Allow == allow);
        }
        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }
    public async Task<List<GroupPermissionGlobalModel>> GetManyById(params string[] id)
    {
        var filter = Builders<GroupPermissionGlobalModel>
            .Filter
            .In(v => v.Id, id);
        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }
    public async Task<List<GroupPermissionGlobalModel>> GetManyByGroup(params string[] groupId)
    {
        var filter = Builders<GroupPermissionGlobalModel>
            .Filter
            .In(v => v.GroupId, groupId);
        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }

    public async Task<long> Delete(params string[] id)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<GroupPermissionGlobalModel>
            .Filter
            .In(v => v.Id, id);
        var result = await collection.DeleteManyAsync(filter);
        return result.DeletedCount;
    }

    public async Task<bool> Exists(string id)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<GroupPermissionGlobalModel>
            .Filter
            .Where(v => v.Id == id);
        var count = await collection.CountDocumentsAsync(filter);
        return count > 0;
    }
    public async Task<GroupPermissionGlobalModel> InsertOrUpdate(GroupPermissionGlobalModel model)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var idFilter = Builders<GroupPermissionGlobalModel>
            .Filter
            .Where(v => v.Id == model.Id);
        var allExistsFilter = Builders<GroupPermissionGlobalModel>
            .Filter
            .Where(v => v.Allow == model.Allow && v.Kind == model.Kind && v.GroupId == model.GroupId);
        if (!await Exists(model.Id))
        {
            var existingByPropsResult = await collection.FindAsync(allExistsFilter);
            var existingByProps = existingByPropsResult.ToList();
            if (existingByProps.Any())
            {
                var first = existingByProps.First();
                if (existingByProps.Count > 1)
                {
                    var deleteFilter = Builders<GroupPermissionGlobalModel>
                        .Filter
                        .Where(v => v.Id != first.Id);
                    deleteFilter &= allExistsFilter;

                    var count = await collection.DeleteManyAsync(deleteFilter);
                    _log.Debug($"Deleted {count.DeletedCount} unnecessary documents with duplicate values (remaining ID: {first.Id})");
                }
                _log.Trace($"Returning existing model ({first.Id})");
                return first;
            }
            else
            {
                await collection.InsertOneAsync(model);
                _log.Trace($"Inserted {model.Id}");
                return model;
            }
        }
        else
        {
            var result = await collection.UpdateManyAsync(idFilter, Builders<GroupPermissionGlobalModel>.Update
                .Set(v => v.Allow, model.Allow)
                .Set(v => v.Kind, model.Kind)
                .Set(v => v.GroupId, model.GroupId));
            if (result.IsAcknowledged)
            {
                _log.Trace($"Updated {result.ModifiedCount} documents (for ID: {model.Id})");
            }
            else
            {
                _log.Trace($"Updated some documents (for ID: {model.Id})");
            }
            return model;
        }
    }
}