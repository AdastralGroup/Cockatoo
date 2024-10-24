using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.DataAccess.Models;

public interface IUpdatedAt
{
    /// <summary>
    /// Unix Timestamp when this document was last updated. (UTC, Seconds)
    /// </summary>
    [BsonRepresentation(BsonType.Int64)]
    public BsonTimestamp UpdatedAt { get; set; }
}