using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.DataAccess.Models;

public class ApplicationColorModel : BaseGuidModel
{
    public static string CollectionName => "application_color";

    /// <summary>
    /// <see cref="ApplicationDetailModel.Id"/> 
    /// </summary>
    public string ApplicationId { get; set; } = "";
    [BsonRepresentation(BsonType.String)]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ApplicationColorKind Kind { get; set; }
    /// <summary>
    /// Color that is formatted in hex, like <c>#09fe09</c>
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Value { get; set; }

    public string UpdatedAtTimestamp { get; set; } = "0";
    public long GetUpdatedAtTimestamp() => long.Parse(UpdatedAtTimestamp);
    public void SetUpdatedAtTimestamp(long value)
    {
        UpdatedAtTimestamp = value.ToString();
    }
    public void SetUpdatedAtTimestamp()
    {
        SetUpdatedAtTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }
}
public enum ApplicationColorKind
{
    [BsonRepresentation(BsonType.String)]
    Dark = 0,
    [BsonRepresentation(BsonType.String)]
    Light = 1,
    [BsonRepresentation(BsonType.String)]
    Main = 2,
    [BsonRepresentation(BsonType.String)]
    Accent = 3,
    [BsonRepresentation(BsonType.String)]
    Secondary = 4,
    [BsonRepresentation(BsonType.String)]
    LightForeground = 5,
    [BsonRepresentation(BsonType.String)]
    Click = 6,
    [BsonRepresentation(BsonType.String)]
    ClickT = 7
}