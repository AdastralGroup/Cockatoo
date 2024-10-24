using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Repositories;

[CockatooDependency]
public class BlogPostRepository : BaseRepository<BlogPostModel>
{
    private readonly ApplicationDetailRepository _applicationDetailRepo;
    private readonly BullseyeAppRevisionRepository _bullseyeAppRevisionRepo;
    public BlogPostRepository(IServiceProvider services)
        : base(BlogPostModel.CollectionName, services)
    {
        _applicationDetailRepo = services.GetRequiredService<ApplicationDetailRepository>();
        _bullseyeAppRevisionRepo = services.GetRequiredService<BullseyeAppRevisionRepository>();
    }
    private static SortDefinition<BlogPostModel> SortByCreatedAtDesc;
    static BlogPostRepository()
    {
        SortByCreatedAtDesc = Builders<BlogPostModel>
            .Sort
            .Descending(v => v.CreatedAtTimestamp);
    }
    public async Task<BlogPostModel?> GetById(string id)
    {
        var filter = Builders<BlogPostModel>
            .Filter
            .Where(v => v.Id == id);
        var result = await BaseFind(filter);
        return result?.FirstOrDefault();
    }
    public async Task<List<BlogPostModel>> GetAll(bool? liveState = null)
    {
        var filter = Builders<BlogPostModel>
            .Filter
            .Empty;
        if (liveState != null)
        {
            var isLive = (bool)liveState;
            filter = Builders<BlogPostModel>
                .Filter
                .Where(v => v.IsLive == isLive);
        }
        var result = await BaseFind(filter, SortByCreatedAtDesc);
        return result?.ToList() ?? [];
    }
    public async Task<List<BlogPostModel>> GetManyForRevision(string revisionId, bool onlyLive)
    {
        if (string.IsNullOrEmpty(revisionId))
        return [];
        var filter = Builders<BlogPostModel>
            .Filter
            .Where(v => v.BullseyeRevisionId == revisionId);
        if (onlyLive)
        {
            filter &= Builders<BlogPostModel>
                .Filter
                .Where(v => v.IsLive == true);
        }
        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }

    public async Task<bool> Exists(string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;
        var filter = Builders<BlogPostModel>
            .Filter
            .Where(v => v.Id == id);
        var result = await BaseFind(filter);
        return result?.Any() ?? false;
    }
    public async Task<bool> SlugExists(string slug)
    {
        if (string.IsNullOrEmpty(slug))
            return false;
        var filter = Builders<BlogPostModel>
            .Filter
            .Where(v => v.Slug == slug);
        var result = await BaseFind(filter);
        return result?.Any() ?? false;
    }
    public Task<long> Delete(params BlogPostModel[] models)
        => Delete(models.Select(v => v.Id).ToArray());
    public async Task<long> Delete(params string[] ids)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<BlogPostModel>
            .Filter
            .In(v => v.Id, ids);
        var result = await collection.DeleteManyAsync(filter);
        return result.DeletedCount;
    }
    public async Task InsertOrUpdate(BlogPostModel model)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        if (string.IsNullOrEmpty(model.Slug) == false)
        {
            if (await SlugExists(model.Slug))
            {
                throw new ArgumentException($"Slug \"{model.Slug}\" already exists");
            }
        }
        if (string.IsNullOrEmpty(model.ApplicationId) == false)
        {
            if (await _applicationDetailRepo.ExistsById(model.ApplicationId) == false)
            {
                throw new ArgumentException($"Application with Id \"{model.ApplicationId}\" does not exist", nameof(model));
            }
        }
        if (string.IsNullOrEmpty(model.BullseyeRevisionId) == false)
        {
            if (await _bullseyeAppRevisionRepo.ExistsById(model.BullseyeRevisionId) == false)
            {
                throw new ArgumentException($"Bullseye Revision with Id \"{model.BullseyeRevisionId}\" does not exist", nameof(model));
            }
        }
        var filter = Builders<BlogPostModel>
            .Filter
            .Where(v => v.Id == model.Id);
        var existsResult = await BaseFind(filter);
        if (existsResult.Any())
        {
            await collection.ReplaceOneAsync(filter, model);
        }
        else
        {
            await collection.InsertOneAsync(model);
        }
    }
}