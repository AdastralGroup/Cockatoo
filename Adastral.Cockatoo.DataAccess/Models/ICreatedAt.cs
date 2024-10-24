using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.DataAccess.Models;

public interface ICreatedAt
{
    /// <summary>
    /// Unix Timestamp when this document was created (UTC, Seconds)
    /// </summary>
    [BsonRepresentation(BsonType.Int64)]
    public BsonTimestamp CreatedAt { get; set; }
}