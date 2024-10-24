using System.ComponentModel;
using Adastral.Cockatoo.DataAccess.Models;

namespace Adastral.Cockatoo.Services.WebApi.Models.Request;

/// <summary>
/// Request Data to Delete a Blog Post.
/// </summary>
public class BlogPostV1DeleteRequest
{
    /// <summary>
    /// <see cref="BlogPostModel.Id"/>
    /// </summary>
    public string Id { get; set; } = "";
    /// <summary>
    /// Delete any Storage resources that are used by the Blog Post Id provided.
    /// </summary>
    [DefaultValue(false)]
    public bool DeleteStorageResources { get; set; } = false;
}