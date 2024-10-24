using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Adastral.Cockatoo.Services.WebApi.Models.Request;

public class ManageBullseyeV1RegisterRevisionRequest
{
    /// <summary>
    /// Value for <see cref="BullseyeAppRevisionModel.Version"/>
    /// </summary>
    [JsonPropertyName("version")]
    public uint Version { get; set; }
    /// <summary>
    /// Value for <see cref="BullseyeAppRevisionModel.Tag"/>
    [JsonPropertyName("tag")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tag { get; set; }
    /// <summary>
    /// Archive File for this Revision (<see cref="StorageFileModel.Id"/>)
    /// </summary>
    [JsonPropertyName("archive")]
    public string ArchiveFileId { get; set; }
    /// <summary>
    /// Extracted size in bytes of the Archive provided. (optional, but recomended)
    /// </summary>
    [JsonPropertyName("archiveExtractedSize")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? ExtractedArchiveSize { get; set; }
    /// <summary>
    /// File for the <c>.torrent</c> file (<see cref="StorageFileModel.Id"/>, optional)
    /// </summary>
    [JsonPropertyName("p2p")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PeerToPeerFileId { get; set; }
    /// <summary>
    /// File for the signature file (<see cref="StorageFileModel.Id"/>, optional)
    /// </summary>
    [JsonPropertyName("signature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SignatureFileId { get; set; }
    /// <summary>
    /// Previons Bullseye App Revision. (<see cref="BullseyeAppRevisionModel.Id"/>, optional)
    /// </summary>
    [JsonPropertyName("previousRevision")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PreviousRevisionId { get; set; }
    /// <summary>
    /// Should this revision be marked as publicly available? (<see cref="BullseyeAppRevisionModel.IsLive"/>)
    /// </summary>
    [JsonPropertyName("live")]
    [DefaultValue(false)]
    public bool IsLive { get; set; }
}