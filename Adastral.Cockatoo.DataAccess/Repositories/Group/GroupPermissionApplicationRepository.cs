using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Repositories.Group;

[CockatooDependency]
public class GroupPermissionApplicationRepository : BaseRepository<GroupPermissionApplicationModel>
{
    private readonly MongoClient _mongoClient;

    public GroupPermissionApplicationRepository(IServiceProvider services)
        : base(GroupPermissionApplicationModel.CollectionName, services)
    {
        _mongoClient = services.GetRequiredService<MongoClient>();
    }

    public async Task<GroupPermissionApplicationModel?> GetById(string id)
    {
        id = id.ToLower().Trim();
        var filter = Builders<GroupPermissionApplicationModel>
            .Filter
            .Where(v => v.Id == id);
        var result = await BaseFind(filter);
        return result?.FirstOrDefault();
    }

    public async Task<List<GroupPermissionApplicationModel>> GetAll()
    {
        var filter = Builders<GroupPermissionApplicationModel>.Filter.Empty;
        var result = await BaseFind(filter);
        return result.ToList();
    }
    public async Task<List<GroupPermissionApplicationModel>> GetManyByGroup(string groupId)
    {
        groupId = groupId.ToLower().Trim();
        var filter = Builders<GroupPermissionApplicationModel>
            .Filter
            .Where(v => v.GroupId == groupId);
        var result = await BaseFind(filter);
        return result.ToList();
    }

    public async Task<List<GroupPermissionApplicationModel>> GetManyByApplication(string applicationId)
    {
        applicationId = applicationId.ToLower().Trim();
        var filter = Builders<GroupPermissionApplicationModel>
            .Filter
            .Where(v => v.ApplicationId == applicationId);
        var result = await BaseFind(filter);
        return result.ToList();
    }

    public class GetManyByOptions
    {
        public string? Id { get; init; }
        public string? GroupId { get; init; }
        public string? ApplicationId { get; init; }
        public ScopedApplicationPermissionKind[]? KindsIn { get; init; }
        public ScopedApplicationPermissionKind[]? KindsEq { get; init; }
        public bool? Allow { get; init; }
    }
    public async Task<List<GroupPermissionApplicationModel>> GetManyBy(GetManyByOptions options)
    {
        var filter = Builders<GroupPermissionApplicationModel>.Filter.Empty;

        if (!string.IsNullOrEmpty(options.Id))
        {
            filter &= Builders<GroupPermissionApplicationModel>
                .Filter
                .Where(v => v.Id == options.Id);
        }

        if (!string.IsNullOrEmpty(options.GroupId))
        {
            filter &= Builders<GroupPermissionApplicationModel>
                .Filter
                .Where(v => v.GroupId == options.GroupId);
        }

        if (!string.IsNullOrEmpty(options.ApplicationId))
        {
            filter &= Builders<GroupPermissionApplicationModel>
                .Filter
                .Where(v => v.ApplicationId == options.ApplicationId);
        }

        if (options.KindsIn?.Length > 0)
        {
            filter &= Builders<GroupPermissionApplicationModel>
                .Filter
                .In(v => v.Kind, options.KindsIn);
        }

        if (options.KindsEq?.Length > 0)
        {
            foreach (var x in options.KindsEq)
            {
                filter &= Builders<GroupPermissionApplicationModel>
                    .Filter
                    .Eq(nameof(GroupPermissionApplicationModel.Kind), x);
            }
        }

        if (options.Allow != null)
        {
            filter &= Builders<GroupPermissionApplicationModel>
                .Filter
                .Where(v => v.Allow == options.Allow);
        }

        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }

    public async Task<long> Delete(params string[] ids)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<GroupPermissionApplicationModel>
            .Filter
            .In(v => v.Id, ids.Select(v => v.Trim().ToLower()));
        var result = await collection.DeleteManyAsync(filter);
        return result.DeletedCount;
    }

    public async Task InsertOrUpdate(GroupPermissionApplicationModel model)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");

        var filter = Builders<GroupPermissionApplicationModel>
            .Filter
            .Where(v => v.Id == model.Id);
        
        var filterProp = Builders<GroupPermissionApplicationModel>
            .Filter
            .Where(v => v.GroupId == model.GroupId);
        filterProp &= Builders<GroupPermissionApplicationModel>
            .Filter
            .Where(v => v.ApplicationId == model.ApplicationId);
        filterProp &= Builders<GroupPermissionApplicationModel>
            .Filter
            .Where(v => v.Kind == model.Kind);
        filterProp &= Builders<GroupPermissionApplicationModel>
            .Filter
            .Where(v => v.Allow == model.Allow);
        
        var filterNotModelId = Builders<GroupPermissionApplicationModel>
            .Filter
            .Where(v => v.Id != model.Id);

        var existsResult = await collection.CountDocumentsAsync(filter);
        if (existsResult <= 0)
        {
            await collection.InsertOneAsync(model);
            await collection.DeleteManyAsync(filterProp & filterNotModelId);
        }
        else
        {
            var updateDefinition = Builders<GroupPermissionApplicationModel>
                .Update
                .Set(v => v.GroupId, model.GroupId)
                .Set(v => v.ApplicationId, model.ApplicationId)
                .Set(v => v.Kind, model.Kind)
                .Set(v => v.Allow, model.Allow);
            await collection.UpdateManyAsync(filter, updateDefinition);
        }
    }
}