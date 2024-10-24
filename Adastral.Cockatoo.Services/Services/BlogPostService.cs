using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using Adastral.Cockatoo.Common;
using Adastral.Cockatoo.DataAccess;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.DataAccess.Repositories;
using Adastral.Cockatoo.Services.WebApi.Models.Request;
using Adastral.Cockatoo.Services.WebApi.Models.Response;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace Adastral.Cockatoo.Services;

[CockatooDependency]
public class BlogPostService : BaseService
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly BlogPostRepository _blogPostRepo;
    private readonly BlogPostTagRepository _blogPostTagRepo;
    private readonly BlogTagRepository _blogTagRepo;
    private readonly BlogPostAttachmentRepository _blogPostAttachmentRepo;

    private readonly StorageFileRepository _storageFileRepo;
    private readonly StorageService _storageService;
    public BlogPostService(IServiceProvider services)
        : base(services)
    {
        _blogPostRepo = services.GetRequiredService<BlogPostRepository>();
        _blogPostTagRepo = services.GetRequiredService<BlogPostTagRepository>();
        _blogTagRepo = services.GetRequiredService<BlogTagRepository>();
        _blogPostAttachmentRepo = services.GetRequiredService<BlogPostAttachmentRepository>();

        _storageFileRepo = services.GetRequiredService<StorageFileRepository>();
        _storageService = services.GetRequiredService<StorageService>();
    }

    /// <summary>
    /// Delete a blog post with the request provided.
    /// </summary>
    public async Task<BlogPostV1DeleteResponse> Delete(BlogPostV1DeleteRequest req)
    {
        if (string.IsNullOrEmpty(req.Id))
        {
            throw new ArgumentException($"{nameof(BlogPostV1DeleteRequest)}.{nameof(req.Id)} is required", nameof(req));
        }

        var model = await _blogPostRepo.GetById(req.Id);
        if (model == null)
        {
            throw new NoNullAllowedException($"{nameof(BlogPostModel)} with Id {req.Id} could not be found in {nameof(BlogPostRepository)}");
        }
        var result = new BlogPostV1DeleteResponse
        {
            Request = req,
            Model = model
        };

        try
        { await _blogPostRepo.Delete(model); }
        catch (Exception ex)
        { result.ModelDeleteException = new(ex); }

        try
        { result.TagAssociations = await _blogPostTagRepo.DeleteForPost(model); }
        catch (Exception ex)
        { result.TagAssociationsDeleteException = new(ex); }

        try
        { result.Attachments = await _blogPostAttachmentRepo.DeleteForPost(model); }
        catch (Exception ex)
        { result.AttachmentsDeleteException = new(ex); }

        foreach (var attachment in result.Attachments)
        {
            try
            {
                var fileModel = await _storageFileRepo.GetById(attachment.StorageFileId);
                if (fileModel == null)
                {
                    _log.Warn($"{attachment.StorageFileId} could not be found in {nameof(StorageFileRepository)}");
                    continue;
                }
                var dependencies = await _storageService.GetFileReferences(fileModel);
                if (dependencies.Count < 1)
                {
                    result.DeletedFiles[fileModel.Id] = fileModel;
                    await _storageService.Delete(fileModel);
                }
            }
            catch (Exception ex)
            {
                result.DeletedFilesExceptions[attachment.Id] = new(ex);
            }
        }

        return result;
    }
}