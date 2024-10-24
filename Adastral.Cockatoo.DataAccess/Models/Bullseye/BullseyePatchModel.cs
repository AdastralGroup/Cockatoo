using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.DataAccess.Models;

/// <summary>
/// Model representing a patch that can be used to upgrade from one <see cref="BullseyeAppRevisionModel"/> to another.
/// </summary>
public class BullseyePatchModel : BaseGuidModel
{
    public const string CollectionName = "bullseye_patch";

    public BullseyePatchModel()
        : base()
    {
        FromRevisionId = "";
        ToRevisionId = "";
        StorageFileId = "";
        CreatedAt = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    /// <summary>
    /// Revision that this patch will be upgrading from
    /// </summary>
    /// <remarks>
    /// Foreign Key to <see cref="BullseyeAppRevisionModel.Id"/>
    /// </remarks>
    public string FromRevisionId { get; set; }

    /// <summary>
    /// Revision that this patch will be upgrading to
    /// </summary>
    /// <remarks>
    /// Foreign Key to <see cref="BullseyeAppRevisionModel.Id"/>
    /// </remarks>
    public string ToRevisionId { get; set; }

    /// <summary>
    /// Id for <see cref="StorageFileModel"/> that contains the Butler Patch file.
    /// </summary>
    /// <remarks>
    /// Foreign Key to <see cref="StorageFileModel.Id"/>
    /// </remarks>
    public string StorageFileId { get; set; }

    /// <summary>
    /// Id for <see cref="StorageFileModel"/> that contains the <c>.torrent</c> file that contains the Butler Patch file.
    /// </summary>
    /// <remarks>
    /// Foreign Key to <see cref="StorageFileModel.Id"/>
    /// </remarks>
    public string? PeerToPeerStorageFileId { get; set; }

    /// <summary>
    /// Timestamp when this Patch was created (Unix Epoch, UTC, Seconds)
    /// </summary>
    [JsonIgnore]
    public BsonTimestamp CreatedAt { get; set; }

    [BsonIgnore]
    [JsonPropertyName(nameof(CreatedAt))]
    public long? CreatedAtJson => CreatedAt?.Value;
}