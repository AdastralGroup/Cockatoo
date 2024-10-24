namespace Adastral.Cockatoo.DataAccess.Models;

public class BlogPostAttachmentModel : BaseGuidModel
{
    public const string CollectionName = "blog_post_attachment";
    /// <summary>
    /// Foreign Key to <see cref="BlogPostModel.Id"/>
    /// </summary>
    public string BlogPostId { get; set; }
    /// <summary>
    /// Foreign Key to <see cref="StorageFileModel.Id"/>
    /// </summary>
    public string StorageFileId { get; set; }
}