using System.ComponentModel;
using System.Text.Json.Serialization;
using Adastral.Cockatoo.DataAccess.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.Services.WebApi.Models.Response;

public class AdminUserV1DetailResponse : IUserModel
{
    [JsonPropertyName("_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string Type => GetType().Name;

    #region IUserModel
    [JsonRequired]
    [BsonId]
    [BsonElement("_id")]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = "";
    /// <inheritdoc/>
    [DefaultValue("User")]
    public string DisplayName { get; set; } = "User";
    /// <inheritdoc/>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [BsonIgnoreIfNull]
    public string? Email { get; set; }
    /// <inheritdoc/>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [BsonIgnoreIfNull]
    public string? SteamUserId { get; set; }
    /// <inheritdoc/>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [BsonIgnoreIfNull]
    public string? OAuthUserId { get; set; }
    /// <inheritdoc/>
    [DefaultValue(false)]
    public bool IsServiceAccount { get; set; }
    #endregion
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [BsonIgnoreIfNull]
    public string? AvatarFileId { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [BsonIgnoreIfNull]
    public string? OwnerUserId { get; set; }

    public void FromModel(UserModel user)
    {
        Id = user.Id;
        DisplayName = user.DisplayName;
        Email = user.Email;
        SteamUserId = user.SteamUserId;
        OAuthUserId = user.OAuthUserId;
        IsServiceAccount = user.IsServiceAccount;
    }

    public void FromModel(UserPreferencesModel? preferences)
    {
        AvatarFileId = preferences?.AvatarStorageFileId?.ToLower();
    }

    public void FromModel(ServiceAccountModel? serviceAccount)
    {
        OwnerUserId = serviceAccount?.OwnerUserId?.ToLower();
    }
}