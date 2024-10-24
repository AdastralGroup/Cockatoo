using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.DataAccess.Models;

/// <summary>
/// Model representing a pre-generated cached version of a Users permissions.
/// </summary>
public class PermissionUserCacheModel : BaseGuidModel
{
    public const string CollectionName = "auth_permission_usercache";
    /// <summary>
    /// Foreign Key to <see cref="UserModel.Id"/>
    /// </summary>
    public string UserId { get; set; } = "";
    /// <summary>
    /// <para><see cref="long"/> formatted as a string.</para>
    /// <para>Timestamp since this was created (UTC, Seconds)</para>
    /// </summary>
    public string Timestamp { get; set; } = "0";

    /// <summary>
    /// Get value for <see cref="Timestamp"/>
    /// </summary>
    public long GetTimestamp()
    {
        return long.Parse(Timestamp);
    }
    
    /// <summary>
    /// Set value for <see cref="Timestamp"/>
    /// </summary>
    public void SetTimestamp(long value)
    {
        Timestamp = value.ToString();
    }

    /// <summary>
    /// Array of allowed permissions. If a permission isn't in this, then deny the user.
    /// </summary>
    [BsonSerializer(typeof(PermissionListSerializer))]
    public List<PermissionKind> Permissions { get; set; } = [];
}