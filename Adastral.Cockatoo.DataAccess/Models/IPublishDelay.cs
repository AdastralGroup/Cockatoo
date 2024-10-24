using MongoDB.Bson;

namespace Adastral.Cockatoo.DataAccess.Models;

public interface IPublishDelay
{
    public bool IsLive { get; set; }
    public BsonTimestamp? PublishAt { get; set; }
}