using System.Data;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Repositories;

[CockatooDependency]
public class BlogPostAttachmentRepository : BaseRepository<BlogPostAttachmentModel>
{
    private readonly StorageFileRepository _storageFileRepo;
    private readonly BlogPostRepository _blogPostRepo;
    public BlogPostAttachmentRepository(IServiceProvider services)
        : base(BlogPostAttachmentModel.CollectionName, services)
    {
        _storageFileRepo = services.GetRequiredService<StorageFileRepository>();
        _blogPostRepo = services.GetRequiredService<BlogPostRepository>();
    }

    public async Task<BlogPostAttachmentModel?> GetById(string id)
    {
        var filter = Builders<BlogPostAttachmentModel>
            .Filter
            .Where(v => v.Id == id);
        var result = await BaseFind(filter);
        return result?.FirstOrDefault();
    }

    public Task<List<BlogPostAttachmentModel>> GetAllUsingFile(StorageFileModel file)
        => GetAllUsingFile(file.Id);
    public async Task<List<BlogPostAttachmentModel>> GetAllUsingFile(string storageFileId)
    {
        var filter = Builders<BlogPostAttachmentModel>
            .Filter
            .Where(v => v.StorageFileId == storageFileId);
        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }
    public async Task<List<BlogPostAttachmentModel>> GetAllForBlogPost(string blogPostId)
    {
        var filter = Builders<BlogPostAttachmentModel>
            .Filter
            .Where(v => v.BlogPostId == blogPostId);
        var result = await BaseFind(filter);
        return result?.ToList() ?? [];
    }

    public Task<List<BlogPostAttachmentModel>> DeleteForPost(params BlogPostModel[] posts)
        => DeleteForPost(posts.Select(v => v.Id).ToArray());
    public async Task<List<BlogPostAttachmentModel>> DeleteForPost(params string[] blogPostIds)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<BlogPostAttachmentModel>
            .Filter
            .In(v => v.BlogPostId, blogPostIds);
        var result = await BaseFind(filter);
        await collection.DeleteManyAsync(filter);
        return result?.ToList() ?? [];
    }

    public async Task<long> Delete(params string[] ids)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        var filter = Builders<BlogPostAttachmentModel>
            .Filter
            .In(v => v.Id, ids);
        var result = await collection.DeleteManyAsync(filter);
        return result.DeletedCount;
    }

    public async Task InsertOrUpdate(BlogPostAttachmentModel model)
    {
        var collection = GetCollection();
        if (collection == null)
            throw new NoNullAllowedException($"{nameof(GetCollection)} returned null");
        if (await _blogPostRepo.Exists(model.BlogPostId) == false)
        {
            throw new NoNullAllowedException($"{nameof(BlogPostModel)} with Id {model.BlogPostId} does not exist in {nameof(BlogPostRepository)}");
        }
        if (await _storageFileRepo.Exists(model.StorageFileId) == false)
        {
            throw new NoNullAllowedException($"{nameof(StorageFileModel)} with Id {model.StorageFileId} does not exist in {nameof(StorageFileRepository)}");
        }

        var filter = Builders<BlogPostAttachmentModel>
            .Filter
            .Where(v => v.Id == model.Id);
        var result = await BaseFind(filter);
        if (result?.Any() ?? false)
        {
            await collection.ReplaceOneAsync(filter, model);
        }
        else
        {
            await collection.InsertOneAsync(model);
        }
    }
}