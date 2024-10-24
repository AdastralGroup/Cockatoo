using Adastral.Cockatoo.DataAccess.Models;
using MongoDB.Driver;

namespace Adastral.Cockatoo.DataAccess.Helpers;

public static class MongoHelpers
{
    public static FilterDefinition<T> PublishDelayFilter<T>(bool value)
        where T : IPublishDelay
    {
        var current = new MongoDB.Bson.BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        if (value)
        {
            var innerFilter = Builders<T>
                .Filter
                .Where(v => v.IsLive);
            innerFilter |= Builders<T>
                .Filter
                .Where(v => v.PublishAt != null && v.PublishAt < current);
            return innerFilter;
        }
        else
        {
            var innerFilter = Builders<T>
                .Filter
                .Where(v => v.IsLive == false);
            innerFilter |= Builders<T>
                .Filter
                .Where(v => v.PublishAt != null && v.PublishAt > current);
            return innerFilter;
        }
    }
}