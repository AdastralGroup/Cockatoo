namespace Adastral.Cockatoo.DataAccess.Models;

public class BlogTagModel : BaseGuidModel
{
    public const string CollectionName = "blog_tag";

    /// <summary>
    /// Name of the Tag
    /// </summary>
    public string Name { get; set; }
}