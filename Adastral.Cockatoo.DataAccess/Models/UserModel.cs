using System.ComponentModel;
using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.DataAccess.Models;

public class UserModel
    : BaseGuidModel
    , IUserModel
{
    public static string CollectionName => "user";
    /// <inheritdoc/>
    public string DisplayName { get; set; } = "User";
    /// <inheritdoc/>
    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Email { get; set; }
    /// <inheritdoc/>
    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SteamUserId { get; set; }
    /// <inheritdoc/>
    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OAuthUserId { get; set; }
    /// <summary>
    /// <para><see cref="long"/> formatted as a string.</para>
    ///
    /// <para>Since Unix Epoch (Seconds, UTC)</para>
    /// </summary>
    [BsonIgnoreIfNull]
    [BsonIgnoreIfDefault]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string CreatedAtTimestamp { get; set; } = "";
    public long GetCreatedAtTimestamp()
    {
        if (string.IsNullOrEmpty(CreatedAtTimestamp))
            return 0;
        if (long.TryParse(CreatedAtTimestamp, out var s))
        {
            return s;
        }
        return 0;
    }
    public void SetCreatedAtTimestamp()
    {
        SetCreatedAtTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }
    public void SetCreatedAtTimestamp(long value)
    {
        CreatedAtTimestamp = value.ToString();
    }

    /// <inheritdoc/>
    [DefaultValue(false)]
    public bool IsServiceAccount { get; set; } = false;

    private IDictionary<string, object> _extraElements = new Dictionary<string, object>();
    [BsonExtraElements()]
    [JsonIgnore]
    public IDictionary<string, object> ExtraElements
    {
        get => _extraElements;
        set
        {
            _extraElements = value;
            if (_extraElements.ContainsKey("LdapUsername"))
            {
                _extraElements.Remove("LdapUsername");
            }
        }
    }

    public string FormatName()
    {
        if (string.IsNullOrEmpty(DisplayName))
        {
            if (string.IsNullOrEmpty(Email))
            {
                return Id;
            }
            return Email;
        }
        else
        {
            if (string.IsNullOrEmpty(Email))
            {
                return DisplayName.Trim();
            }
            return $"{DisplayName} ({Email})";
        }
    }
}

public interface IUserModel
    : IBaseGuidModel
{
    /// <summary>
    /// Display Name for this user.
    /// </summary>
    [DefaultValue("User")]
    public string DisplayName { get; set; }
    /// <summary>
    /// Email associated with this user.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [BsonIgnoreIfNull]
    public string? Email { get; set; }
    /// <summary>
    /// Steam User Ids. Can be a Steam3 ID or a Steam64 ID.
    /// </summary>
    /// <remarks>
    /// <see href="https://developer.valvesoftware.com/wiki/SteamID">SteamID (Valve Developer Wiki)</see>
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [BsonIgnoreIfNull]
    public string? SteamUserId { get; set; }
    /// <summary>
    /// Username associated with the OAuth/OIDC Endpoint.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [BsonIgnoreIfNull]
    public string? OAuthUserId { get; set; }
    /// <summary>
    /// Is this a service account? If so, there will be a record in <see cref="ServiceAccountRepository"/>
    /// where <see cref="ServiceAccountModel.UserId"/> will equal <see cref="UserModel.Id"/>.
    /// </summary>
    [DefaultValue(false)]
    public bool IsServiceAccount { get; set; }
}

public enum CanUserCreateTokenKind
{
    /// <summary>
    /// The provided requesting user can create a token for the target user.
    /// </summary>
    Yes,
    /// <summary>
    /// Returned when the Target User provided is not a service account.
    /// </summary>
    TargetUserIsNotServiceAccount,
    /// <summary>
    /// Requesting user is not the owner of the target service account, and it doesn't have <see cref="PermissionKind.ServiceAccountAdmin"/>
    /// </summary>
    RequestingUserIsNotOwner,
    /// <summary>
    /// The requesting user is a Service Account, and thus cannot create tokens for any other service account.
    /// </summary>
    RequestingUserIsServiceAccount,
}