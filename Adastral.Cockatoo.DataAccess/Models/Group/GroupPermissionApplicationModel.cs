using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Adastral.Cockatoo.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.DataAccess.Models;

public class GroupPermissionApplicationModel
    : BaseGuidModel
{
    public const string CollectionName = "group_permission_application";
    public GroupPermissionApplicationModel()
        : base()
    {
        GroupId = Guid.Empty.ToString();
        Kind = ScopedApplicationPermissionKind.Admin;
        Allow = true;
        ApplicationId = null;
    }
    /// <summary>
    /// Foreign Key to <see cref="GroupModel"/>
    /// </summary>
    [DefaultValue("00000000-0000-0000-0000-000000000000")]
    public string GroupId { get; set; }
    /// <summary>
    /// When <see langword="null"/>, this applies to app Applications that are owned by the <see cref="GroupModel"/>
    /// associated with this document.
    /// </summary>
    public string? ApplicationId { get; set; }
    /// <summary>
    /// Permission
    /// </summary>
    [BsonRepresentation(BsonType.String)]
    public ScopedApplicationPermissionKind Kind { get; set; }
    /// <summary>
    /// Should the permission kind be allowed? When <see langword="false"/> it will not allow it.
    /// </summary>
    public bool Allow { get; set; }

    private IDictionary<string, object> _extraElements = new Dictionary<string, object>();
    [BsonExtraElements()]
    [JsonIgnore]
    public IDictionary<string, object> ExtraElements
    {
        get => _extraElements;
        set
        {
            bool p = false;
            if (value.TryGetValue("Permission", out var o ))
            {
                if (Enum.TryParse<ScopedApplicationPermissionKind>(o?.ToString() ?? "", out var x))
                {
                    Kind = x;
                    p = true;
                }
            }
            _extraElements = value;
            if (p)
            {
                _extraElements.Remove("Permission");
            }
        }
    }

    public GroupPermissionApplicationModel Clone()
    {
        return (this.MemberwiseClone() as GroupPermissionApplicationModel)!;
    }
}

public enum ScopedApplicationPermissionKind
{
    [EnumDisplayIgnore]
    Unknown = 0x00,
    /// <summary>
    /// User can do anything related to the application
    /// </summary>
    Admin,
    EditDetails,
    EditAppearance,
    SubmitRevisions,
    ManageRevisions,
    UpdateCache,
    ReadOnly
}