using System.Text.Json.Serialization;
using System.Xml.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Adastral.Cockatoo.DataAccess.Models;

/// <summary>
/// A Version Revision for <see cref="BullseyeAppModel"/>
/// </summary>
public class BullseyeAppRevisionModel
    : BaseGuidModel
    , IPublishDelay
{
    [JsonPropertyName("_type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    [BsonIgnore]
    [XmlIgnore]
    [SoapIgnore]
    public string Type => GetType().Name;
    public const string CollectionName = "bullseye_app_revision";

    public BullseyeAppRevisionModel()
        : base()
    {
        BullseyeAppId = "";
        ArchiveStorageFileId = "";
        CreatedAt = new BsonTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        IsLive = false;
        PublishAt = null;
        Version = "0";
    }

    /// <summary>
    /// Id for the <see cref="BullseyeApp"/> that this Revision is for.
    /// </summary>
    /// <remarks>
    /// Foreign Key to <see cref="BullseyeAppModel.Id"/>
    /// </remarks>
    public string BullseyeAppId { get; set; }

    /// <summary>
    /// Previous revision that was released before this one.
    /// </summary>
    /// <remarks>
    /// Foreign Key to <see cref="BullseyeAppRevisionModel"/>
    /// </remarks>
    public string? PreviousRevisionId { get; set; }
    /// <summary>
    /// Tag for this revision. Used for displaying in-app, and is a clustered unique primary key with the <see cref="Id"/>.
    /// </summary>
    [BsonIgnoreIfNull]
    public string? Tag { get; set; }

    /// <summary>
    /// <para><see cref="uint"/> stored as a nullable string.</para>
    /// 
    /// Version number
    /// </summary>
    public string Version { get; set; }
    /// <summary>
    /// Parse <see cref="Version"/> as a <see cref="uint"/>
    /// </summary>
    public uint GetVersion()
    {
        return uint.Parse(Version);
    }
    /// <summary>
    /// Set the value for <see cref="Version"/>
    /// </summary>
    public void SetVersion(uint value)
    {
        Version = value.ToString();
    }

    /// <summary>
    /// Id for the Storage File that contains the full Archive of this revision.
    /// </summary>
    /// <remarks>
    /// Foreign Key Constraint to <see cref="StorageFileModel.Id"/>
    /// </remarks>
    public string ArchiveStorageFileId { get; set; }

    /// <summary>
    /// <para><see cref="long"/> stored as a nullable string.</para>
    /// Extracted size of the Archive in bytes.
    /// </summary>
    public string? ExtractedArchiveSize { get; set; }
    /// <summary>
    /// Try and get the value of <see cref="ExtractedArchiveSize"/>
    /// </summary>
    public long? GetExtractedArchiveSize()
    {
        if (!string.IsNullOrEmpty(ExtractedArchiveSize) && long.TryParse(ExtractedArchiveSize, out var s))
            return s;
        return null;
    }
    /// <summary>
    /// Set the value for <see cref="ExtractedArchiveSize"/>
    /// </summary>
    public void SetExtractedArchiveSize(long value)
    {
        ExtractedArchiveSize = value.ToString();
    }

    /// <summary>
    /// Id for the Storage File that contains the <c>.torrent</c> file that contains the Archive for this revision.
    /// </summary>
    /// <remarks>
    /// Foreign Key Constraint to <see cref="StorageFileModel.Id"/>
    /// </remarks>
    public string? PeerToPeerStorageFileId { get; set; }

    /// <summary>
    /// Id for the Storage File that contains the signature file.
    /// </summary>
    /// <remarks>
    /// Foreign Key Constraint to <see cref="StorageFileModel.Id"/>
    /// </remarks>
    public string? SignatureStorageFileId { get; set; }

    /// <summary>
    /// Is this current revision live? When it is, it should be available in <see cref="BullseyeV1"/>/<see cref="BullseyeV2"/>.
    /// </summary>
    public bool IsLive { get; set; }
    
    /// <summary>
    /// When set, this revision should be scheduled at the Timestamp provided (Unix Epoch, UTC, Seconds) to set the <see cref="IsLive"/> property to <see langword="true"/>
    /// </summary>
    [JsonIgnore]
    public BsonTimestamp? PublishAt { get; set; }

    [BsonIgnore]
    [JsonPropertyName(nameof(PublishAt))]
    public long? PublishAtJson => PublishAt?.Value;

    /// <summary>
    /// Timestamp when this revision was created at (Unix Epoch, UTC, Seconds)
    /// </summary>
    [JsonIgnore]
    public BsonTimestamp CreatedAt { get; set; }
    [BsonIgnore]
    [JsonPropertyName(nameof(CreatedAt))]
    public long? CreatedAtJson => CreatedAt?.Value;
}