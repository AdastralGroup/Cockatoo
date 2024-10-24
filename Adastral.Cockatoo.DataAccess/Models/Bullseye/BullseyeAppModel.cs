using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.DataAccess.Models;

/// <summary>
/// Model representing an application that supports the Bullseye versioning system.
/// </summary>
public class BullseyeAppModel
{
    public const string CollectionName = "bullseye_app";

    public BullseyeAppModel()
    {
        ApplicationDetailModelId = "";
        CreatedAt = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }
    
    /// <summary>
    /// Id for Application Detail Model that this Bullseye App is for.
    /// </summary>
    /// <remarks>
    /// Unique Foreign/Primary Key to <see cref="ApplicationDetailModel.Id"/>
    /// </remarks>
    [BsonElement("_id")]
    public string ApplicationDetailModelId { get; set; }

    /// <summary>
    /// Latest <see cref="BulllseyeAppRevisionModel"/> for this App.
    /// </summary>
    /// <remarks>
    /// Foreign Key to <see cref="BullseyeAppRevisionModel.Id"/>
    /// </remarks>
    public string? LatestRevisionId { get; set; }

    [BsonIgnore]
    public string Id
    {
        get => ApplicationDetailModelId;
        set => ApplicationDetailModelId = value;
    }

    /// <summary>
    /// Timestamp when this Bullseye App was created at (Unix Epoch, UTC, Seconds)
    /// </summary>
    public BsonTimestamp CreatedAt { get; set; }
}