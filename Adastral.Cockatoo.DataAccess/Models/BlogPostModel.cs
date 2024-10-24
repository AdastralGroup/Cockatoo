using System.ComponentModel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.DataAccess.Models;

public class BlogPostModel : BaseGuidModel
{
    public const string CollectionName = "blog_post";

    public BlogPostModel()
        : base()
    {
        CreatedAtTimestamp = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    /// <summary>
    /// Blog Post Title. Markdown only.
    /// </summary>
    public string Title { get; set; } = "";
    /// <summary>
    /// Content is mixed Markdown and HTML.
    /// </summary>
    public string Content { get; set; } = "";
    /// <summary>
    /// Is this blog post live? When not, it is only visible for users with <see cref="PermissionKind.ApplicationViewAllBlogPosts"/>
    /// </summary>
    [DefaultValue(false)]
    public bool IsLive { get; set; } = false;
    /// <summary>
    /// When not null, this can be used as a slug for this blog post. Slugs are unique, and insert/update should be prevented if the slug exists already (when not null)
    /// </summary>
    [BsonIgnoreIfDefault]
    [BsonIgnoreIfNull]
    public string? Slug { get; set; }

    /// <summary>
    /// Timestamp when this Blog Post was created at (Unix Epoch, UTC, Seconds)
    /// </summary>
    [Description("Timestamp when this blog post was created.")]
    public BsonTimestamp CreatedAtTimestamp { get; set; }

    /// <summary>
    /// List of <see cref="UserModel.Id"/> for who created this blog post.
    /// </summary>
    [Description("List of Users that will be displayed as authors.")]
    [BsonIgnoreIfDefault]
    [BsonIgnoreIfNull]
    public List<string> AuthorIds { get; set; } = [];

    /// <summary>
    /// <see cref="ApplicationDetailModel.Id"/> that is associated with this blog post.
    /// </summary>
    [Description("Application Id that is associated with this blog post (if there is any)")]
    [BsonIgnoreIfDefault]
    [BsonIgnoreIfNull]
    [DefaultValue(null)]
    public string? ApplicationId { get; set; }
    /// <summary>
    /// <see cref="BullseyeAppRevisionModel.Id"/> that is associated with this blog post (if there is any).
    /// </summary>
    [Description("Bullseye Revision that is associated with this blog post (if there is any)")]
    [BsonIgnoreIfDefault]
    [BsonIgnoreIfNull]
    [DefaultValue(null)]
    public string? BullseyeRevisionId { get; set; }
}