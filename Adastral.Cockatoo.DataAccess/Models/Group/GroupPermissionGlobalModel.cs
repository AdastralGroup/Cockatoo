using System.ComponentModel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.DataAccess.Models;

public class GroupPermissionGlobalModel : BaseGuidModel
{
    public const string CollectionName = "group_permission_global";

    public GroupPermissionGlobalModel()
        : base()
    {
        GroupId = Guid.Empty.ToString();
        Kind = PermissionKind.Login;
        Allow = true;
    }

    /// <summary>
    /// Foreign Key to <see cref="GroupModel"/>
    /// </summary>
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public string GroupId { get; set; }
    [BsonRepresentation(BsonType.String)]
    public PermissionKind Kind { get; set; } = PermissionKind.Login;
    /// <summary>
    /// Should the permission kind be allowed? When <see langword="false"/> it will not allow it.
    /// </summary>
    [DefaultValue(true)]
    public bool Allow { get; set; }
}