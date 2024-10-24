using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.DataAccess.Models;

public class CockatooDatabaseVersionModel
    : ICreatedAt
{
    public const string CollectionName = "_cockatoo_db_version";
    public const int LatestVersion = 1;
    [BsonElement("_id")]
    public int Version { get; set; }
    /// <summary>
    /// Previous version that this was upgraded from.
    /// </summary>
    public int? PreviousVersion { get; set; }
    /// <summary>
    /// Unix Timestamp when the database was upgraded. (UTC, Seconds)
    /// </summary>
    [BsonRepresentation(BsonType.Int64)]
    public BsonTimestamp CreatedAt { get; set; }

    public CockatooDatabaseVersionModel()
        : base()
    {
        CreatedAt = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }
}