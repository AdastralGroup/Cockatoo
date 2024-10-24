using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Repositories;

[CockatooDependency]
public class BlogPostTagRepository : BaseRepository<BlogPostTagModel>
{
    private readonly BlogTagRepository _blogTagRepo;
    private readonly BlogPostRepository _blogPostRepo;
    public BlogPostTagRepository(IServiceProvider services)
        : base(BlogPostTagModel.CollectionName, services)
    {
        _blogTagRepo = services.GetRequiredService<BlogTagRepository>();
        _blogPostRepo = services.GetRequiredService<BlogPostRepository>();
    }

    public async Task<BlogPostTagModel?> GetById(string id)
    {
        var filter = Builders<BlogPostTagModel>
            .Filter
            .Where(v => v.Id == id);
        var result = await BaseFind(filter);
        return result?.FirstOrDefault();
    }
    public async Task<List<BlogPostTagModel>> GetManyForPost(string postId)
    {
        var filter = Builders<BlogPostTagModel>
            .Filter
            .Where(v => v.BlogPostId == postId);
        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }

    public async Task<bool> AssociationExists(string blogPostId, string tagId)
    {
        var filter = Builders<BlogPostTagModel>
            .Filter
            .Where(v => v.BlogPostId == blogPostId);
        filter &= Builders<BlogPostTagModel>
            .Filter
            .Where(v => v.TagId == tagId);
        var result = await BaseFind(filter);
        return result?.Any() ?? false;
    }

    public async Task<long> Delete(params string[] ids)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<BlogPostTagModel>
            .Filter
            .In(v => v.Id, ids);
        var result = await collection.DeleteManyAsync(filter);
        return result.DeletedCount;
    }
    /// <summary>
    /// Delete all Post Tags for the Blog Posts provided.
    /// </summary>
    public Task<List<BlogPostTagModel>> DeleteForPost(params BlogPostModel[] posts)
        => DeleteForPost(posts.Select(v => v.Id).ToArray());
    /// <summary>
    /// Delete all Post Tags for the Blog Post Ids provided.
    /// </summary>
    public async Task<List<BlogPostTagModel>> DeleteForPost(params string[] blogPostIds)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<BlogPostTagModel>
            .Filter
            .In(v => v.BlogPostId, blogPostIds);
        var resultData = await BaseFind(filter);
        await collection.DeleteManyAsync(filter);
        return resultData?.ToList() ?? [];
    }

    public async Task InsertOrUpdate(BlogPostTagModel model)
    {
        var collection = GetCollection();
        if (collection == null)
        {
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        }
        if (await _blogTagRepo.Exists(model.TagId) == false)
        {
            throw new ArgumentException($"Could not find Blog Tag with Id \"{model.TagId}\"", nameof(model));
        }
        if (await _blogPostRepo.Exists(model.BlogPostId) == false)
        {
            throw new ArgumentException($"Could not find Blog Post with Id \"{model.BlogPostId}\"", nameof(model));
        }
        var filter = Builders<BlogPostTagModel>
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
    }
}