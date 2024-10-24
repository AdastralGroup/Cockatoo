using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.DataAccess.Models;

public class UserPreferencesModel
{
    public const string CollectionName = "user_preferences";

    [BsonId]
    [BsonElement("_id")]
    [BsonRepresentation(BsonType.String)]
    [Required]
    [JsonRequired]
    public string UserId { get; set; } = "";

    [BsonIgnore]
    public string Id => UserId;

    /// <summary>
    /// Foreign Key to <see cref="StorageFileModel.Id"/>
    /// </summary>
    public string? AvatarStorageFileId { get; set; }
    
    [BsonIgnoreIfNull]
    [BsonIgnoreIfDefault]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Theme { get; set; }
}