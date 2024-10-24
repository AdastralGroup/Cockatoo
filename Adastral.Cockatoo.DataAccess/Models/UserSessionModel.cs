using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Adastral.Cockatoo.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.DataAccess.Models;

public class UserSessionModel : BaseGuidModel
{
    public UserSessionModel()
        : base()
    {
        CreatedAt = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        IsDeleted = false;
    }

    public const string CollectionName = "user_session";

    private string _userId = "";
    /// <summary>
    /// Foreign Key to <see cref="UserModel.Id"/>
    /// </summary>
    [Required]
    [BsonRequired]
    public string UserId
    {
        get => _userId;
        set
        {
            _userId = value.ToLower().Trim();
        }
    }

    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? IpAddress { get; set; }
    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UserAgent { get; set; }
    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Token { get; set; }
    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AspNetSessionId { get; set; }

    [DefaultValue(false)]
    public bool IsDeleted { get; set; }

    [Required]
    [BsonRequired]
    [JsonConverter(typeof(JsonLongBsonTimestampConverter))]
    public BsonTimestamp CreatedAt { get; set; }
    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(JsonLongBsonTimestampConverter))]
    public BsonTimestamp? ExpiresAt { get; set; }
}