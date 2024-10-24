using Adastral.Cockatoo.DataAccess;
using Adastral.Cockatoo.DataAccess.Models;
using Adastral.Cockatoo.Services.WebApi.Models.Request;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

public class BlogPostV1DeleteResponse
{
    /// <summary>
    /// Request that was used to delete the blog post.
    /// </summary>
    public BlogPostV1DeleteRequest Request { get; set; } = new();

    /// <summary>
    /// Model of the Blog Post that was deleted.
    /// </summary>
    public BlogPostModel Model { get; set; } = new();
    /// <summary>
    /// Exception that was thrown while deleting <see cref="Model"/> (if there is any)
    public ExceptionWebResponse? ModelDeleteException { get; set; }
    /// <summary>
    /// List of Blog Post Tag Associations that were deleted.
    public List<BlogPostTagModel> TagAssociations { get; set; } = [];
    /// <summary>
    /// Exception that was thrown while trying to delete the Blog Post Tag Associations (if there is any)
    /// </summary>
    public ExceptionWebResponse? TagAssociationsDeleteException { get; set; }

    /// <summary>
    /// List of Blog Post Attachments that were removed.
    /// </summary>
    public List<BlogPostAttachmentModel> Attachments { get; set; } = [];
    /// <summary>
    /// Exception that was thrown while trying to delete the Blog Post Attachment Models (if there is any)
    /// </summary>
    public ExceptionWebResponse? AttachmentsDeleteException { get; set; }
    /// <summary>
    /// Dictionary of deleted files. If something is missing in here, then something else depends on that file.
    /// </summary>
    public Dictionary<string, StorageFileModel> DeletedFiles { get; set; } = new();
    /// <summary>
    /// Dictionary of Exceptions that were caught while deleting files used by the blog post provided.
    /// </summary>
    /// <remarks>
    /// <b>Key:</b> <see cref="BlogPostAttachmentModel.Id"/>
    /// </remarks>
    public Dictionary<string, ExceptionWebResponse> DeletedFilesExceptions { get; set; } = new();
}