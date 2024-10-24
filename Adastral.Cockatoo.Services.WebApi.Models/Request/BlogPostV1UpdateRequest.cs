using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Adastral.Cockatoo.Services.WebApi.Models.Request;

public class BlogPostV1UpdateRequest
{
    /// <summary>
    /// New value for <see cref="BlogPostModel.Title"/>
    /// </summary>
    /// <remarks>
    /// Ignored when <see langword="null"/>
    /// </remarks>
    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [DefaultValue(null)]
    public string? Title { get; set; }

    /// <summary>
    /// New value for <see cref="BlogPostModel.Content"/>
    /// </summary>
    /// <remarks>
    /// Ignored when <see langword="null"/>
    /// </remarks>
    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [DefaultValue(null)]
    public string? Content { get; set; }

    /// <summary>
    /// New value for <see cref="BlogPostModel.IsLive"/>
    /// </summary>
    /// <remarks>
    /// Ignored when <see langword="null"/>
    /// </remarks>
    [JsonPropertyName("isLive")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [DefaultValue(null)]
    public bool? IsLive { get; set; }

    /// <summary>
    /// What action should be done with <see cref="Authors"/>?
    /// </summary>
    [JsonPropertyName("authorsAction")]
    [DefaultValue(BlogPostV1UpdateAuthorsKind.Add)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BlogPostV1UpdateAuthorsKind AuthorsAction { get; set; } = BlogPostV1UpdateAuthorsKind.Add;
    /// <summary>
    /// List of <see cref="UserModel.Id"/>
    /// </summary>
    /// <remarks>
    /// Ignored when <see langword="null"/>
    /// </remarks>
    [JsonPropertyName("authors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [DefaultValue(null)]
    public List<string>? Authors { get; set; }
    
    /// <summary>
    /// What action should be done with <see cref="Tags"/>?
    /// </summary>
    [JsonPropertyName("tagsAction")]
    [DefaultValue(BlogPostV1UpdateTagsKind.Add)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BlogPostV1UpdateTagsKind TagsAction { get; set; } = BlogPostV1UpdateTagsKind.Add;
    /// <summary>
    /// List of <see cref="BlogTagModel.Id"/>
    /// </summary>
    /// <remarks>
    /// Ignored when <see langword="null"/>
    /// </remarks>
    [JsonPropertyName("tags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [DefaultValue(null)]
    public List<string>? Tags { get; set; }

    /// <summary>
    /// New value for <see cref="BlogPostModel.ApplicationId"/>
    /// </summary>
    /// <remarks>
    /// Ignored when <see langword="null"/>
    /// </remarks>
    [JsonPropertyName("applicationId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [DefaultValue(null)]
    public string? ApplicationId { get; set; }

    /// <summary>
    /// New value for <see cref="BlogPostModel.BullseyeRevisionId"/>
    /// </summary>
    /// <remarks>
    /// Ignored when <see langword="null"/>
    /// </remarks>
    [JsonPropertyName("bullseyeRevisionId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [DefaultValue(null)]
    public string? BullseyeRevisionId { get; set; }
}
public enum BlogPostV1UpdateAuthorsKind
{
    /// <summary>
    /// Add the specified Author Ids, if they exist.
    /// </summary>
    Add,
    /// <summary>
    /// Remove the Author Ids from the provided Blog Post, if those Authors exist.
    /// </summary>
    Remove,
    Set
}
public enum BlogPostV1UpdateTagsKind
{
    /// <summary>
    /// Add the specified Tag Ids, if they exist.
    /// </summary>
    Add,
    /// <summary>
    /// Remove the Tag Ids from the provided Blog Post, if those tags exist.
    /// </summary>
    Remove,
    Set
}