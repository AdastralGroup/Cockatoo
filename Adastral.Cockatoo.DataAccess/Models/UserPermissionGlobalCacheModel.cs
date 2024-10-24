using System.ComponentModel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.DataAccess.Models;

public class UserPermissionGlobalCacheModel : BaseGuidModel
{
    public const string CollectionName = "user_permissionCache_global";
    public UserPermissionGlobalCacheModel()
        : base()
    {
        UserId = Guid.Empty.ToString();
        Permissions = [];
        CreatedAt = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        UpdatedAt = CreatedAt;
    }
    /// <summary>
    /// Foreign Key to <see cref="UserModel"/>
    /// </summary>
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public string UserId { get; set; }
    /// <summary>
    /// List of calculated permissions
    /// </summary>
    [BsonSerializer(typeof(EnumAsStringListSerializer<PermissionKind>))]
    public List<PermissionKind> Permissions { get; set; }
    /// <summary>
    /// Unix Timestamp when this was created (UTC, Seconds)
    /// </summary>
    public BsonTimestamp CreatedAt { get; set; }
    /// <summary>
    /// Unix Timestamp when this was updated (UTC, Seconds)
    /// </summary>
    public BsonTimestamp UpdatedAt { get; set; }
}