using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Adastral.Cockatoo.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Models;

/// <summary>
/// Model used to store what user is in what group.
/// </summary>
public class GroupUserAssociationModel : BaseGuidModel
{
    public const string CollectionName = "group_user_association";

    public GroupUserAssociationModel()
        : base()
    {
        UserId = Guid.Empty.ToString();
        GroupId = Guid.Empty.ToString();
        IsDeleted = false;
        CreatedAt = new(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    /// <summary>
    /// Foreign Key to <see cref="UserModel"/>
    /// </summary>
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public string UserId { get; set; } = Guid.Empty.ToString();

    /// <summary>
    /// Foreign Key to <see cref="GroupModel"/>
    /// </summary>
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public string GroupId { get; set; } = "";

    /// <summary>
    /// Association should be ignored when this is set to <see langword="false"/>
    /// </summary>
    [DefaultValue(false)]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Unix Timestamp when this association was created (UTC, Seconds)
    /// </summary>
    [Required]
    [BsonRequired]
    [JsonConverter(typeof(JsonLongBsonTimestampConverter))]
    public BsonTimestamp CreatedAt { get; set; }

    public UpdateDefinition<GroupUserAssociationModel> CreateUpdateDefinition()
    {
        var result = Builders<GroupUserAssociationModel>
            .Update
            .Set(v => v.UserId, UserId)
            .Set(v => v.GroupId, GroupId)
            .Set(v => v.IsDeleted, IsDeleted)
            .Set(v => v.CreatedAt, CreatedAt);
        return result;
    }
}