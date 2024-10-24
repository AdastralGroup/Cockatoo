using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Adastral.Cockatoo.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.DataAccess.Models;

public class UserSessionRequestModel : BaseGuidModel
{
    public const string CollectionName = "user_session_request";
    public UserSessionRequestModel()
        : base()
    {
        CreatedAt = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    private string _userSessionId = "";
    public string UserSessionId
    {
        get => _userSessionId;
        set => _userSessionId = value.Trim().ToLower();
    }
    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Method { get; set; }
    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; set; }
    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UserAgent { get; set; }
    [BsonIgnoreIfNull]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? IpAddress { get; set; }

    [Required]
    [BsonRequired]
    [JsonConverter(typeof(JsonLongBsonTimestampConverter))]
    public BsonTimestamp CreatedAt { get; set; }
}