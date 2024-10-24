namespace Adastral.Cockatoo.DataAccess.Models;

public class BlogPostTagModel : BaseGuidModel
{
    public const string CollectionName = "blog_post_tag";
    /// <summary>
    /// <see cref="BlogPostModel.Id"/>
    /// </summary>
    public string BlogPostId { get; set; }
    /// <summary>
    /// <see cref="BlogTagModel.Id"/>
    /// </summary>
    public string TagId { get; set; }
}